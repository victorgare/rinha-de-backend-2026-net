using RinhaNet.Api.VectorSearch;
using RinhaNet.Converter.Util;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace RinhaNet.Converter.PreProcessor
{
    public static class VectorDatabaseBuilder
    {
        private const int Dimensions = 14;
        private static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()!.Location)!;
        private static readonly string DataPath = Path.Combine(AssemblyPath, "Resources", "references.json.gz");
        private static readonly string OutputDir = Path.Combine(PathFinder.FindSolutionPath(), "src", "RinhaNet.Api", "Data");
        public static async Task BuildAsync()
        {
            Directory.CreateDirectory(OutputDir);
            await using var jsonStream = File.OpenRead(DataPath);

            await using var vectors = File.Create(Path.Combine(OutputDir, "vectors.bin"));
            await using var labelsDat = File.Create(Path.Combine(OutputDir, "labels.dat"));
            await using var labelsIdx = File.Create(Path.Combine(OutputDir, "labels.idx"));

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            int id = 0;

            await using var gzip = new GZipStream(jsonStream, CompressionMode.Decompress);

            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<VectorRecord>(gzip, options))
            {
                if (item is null)
                    continue;

                if (item.Vector.Length != Dimensions)
                    throw new InvalidOperationException($"Registro {id} tem {item.Vector.Length} dimensões.");

                // grava vetor: 14 floats = 56 bytes
                foreach (var value in item.Vector)
                {
                    var bytes = BitConverter.GetBytes(value);
                    await vectors.WriteAsync(bytes);
                }

                // grava label em labels.dat
                long labelOffset = labelsDat.Position;
                byte[] labelBytes = Encoding.UTF8.GetBytes(item.Label);
                await labelsDat.WriteAsync(labelBytes);

                // grava índice: [offset:int64][length:int32]
                await labelsIdx.WriteAsync(BitConverter.GetBytes(labelOffset));
                await labelsIdx.WriteAsync(BitConverter.GetBytes(labelBytes.Length));

                id++;
            }

            Console.WriteLine($"Importados {id} registros.");
        }
    }
}
