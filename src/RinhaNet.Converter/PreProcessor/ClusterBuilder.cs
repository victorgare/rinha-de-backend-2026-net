using RinhaNet.Api.VectorSearch;
using RinhaNet.Api.VectorSearch.Algo;
using Spectre.Console;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace RinhaNet.Converter.PreProcessor
{
    public class ClusterBuilder : BaseBuilder
    {
        private static readonly SquaredEuclideanDistance _distance = new();

        public static async Task BuildClusters()
        {
            const int sampleSize = 100_000;
            const int clusterCount = 1024;
            const int iterations = 10;

            Directory.CreateDirectory(OutputDir);

            Console.WriteLine("Loading sample...");
            float[][] sample = await LoadSampleAsync(sampleSize);

            Console.WriteLine("Training KMeans...");
            float[][] centroids = TrainKMeans(sample, clusterCount, iterations);

            Console.WriteLine("Saving centroids...");
            SaveCentroids(OutputDir, centroids);

            Console.WriteLine("Writing clustered database...");
            await WriteClusteredDatabaseAsync(centroids);

            Console.WriteLine("Done.");
        }

        private static float[][] TrainKMeans(float[][] sample, int clusterCount, int iterations)
        {
            var random = new Random(42);

            // initial centroids: get random sample
            var centroids = new float[clusterCount][];

            for (int i = 0; i < clusterCount; i++)
            {
                int index = random.Next(sample.Length);
                centroids[i] = (float[])sample[index].Clone();
            }

            AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn
                    {
                        CompletedStyle = new Style(Color.Green),
                        RemainingStyle = new Style(Color.Grey)
                    },
                    new PercentageColumn())
                .Start(ctx =>
            {

                var task = ctx.AddTask("Training KMeans", maxValue: iterations);

                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    var sums = new float[clusterCount][];
                    var counts = new int[clusterCount];

                    for (int c = 0; c < clusterCount; c++)
                        sums[c] = new float[Dimensions];

                    foreach (var vector in sample)
                    {
                        int cluster = FindNearestCentroid(vector, centroids);

                        counts[cluster]++;

                        for (int d = 0; d < Dimensions; d++)
                            sums[cluster][d] += vector[d];
                    }

                    for (int c = 0; c < clusterCount; c++)
                    {
                        if (counts[c] == 0)
                        {
                            // empty cluster: restart with random point
                            int index = random.Next(sample.Length);
                            centroids[c] = (float[])sample[index].Clone();
                            continue;
                        }

                        for (int d = 0; d < Dimensions; d++)
                            centroids[c][d] = sums[c][d] / counts[c];
                    }

                    task.Description = $"Training KMeans {iteration + 1} of {iterations}";
                    task.Increment(1);
                }
            });

            return centroids;
        }

        private static void SaveCentroids(string outputDir, float[][] centroids)
        {
            using var file = File.Create(Path.Combine(outputDir, "centroids.bin"));

            foreach (var centroid in centroids)
            {
                foreach (float value in centroid)
                {
                    Span<byte> bytes = stackalloc byte[sizeof(float)];
                    BitConverter.TryWriteBytes(bytes, value);
                    file.Write(bytes);
                }
            }
        }

        private static async Task<float[][]> LoadSampleAsync(int sampleSize)
        {
            var sample = new List<float[]>(sampleSize);


            await foreach (var item in ReadJsonGzAsync()
                .Where(item => item.Vector.Length == Dimensions)
                .TakeWhile(_ => sample.Count < sampleSize))
            {
                sample.Add((float[])item.Vector.Clone());
            }

            return [.. sample];
        }

        private static int FindNearestCentroid(ReadOnlySpan<float> vector, float[][] centroids)
        {
            int best = 0;
            float bestScore = float.MaxValue;

            for (int i = 0; i < centroids.Length; i++)
            {
                float score = _distance.Score(vector, centroids[i]);

                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            return best;
        }

        private static async IAsyncEnumerable<VectorRecord> ReadJsonGzAsync()
        {
            await using var jsonStream = File.OpenRead(DataPath);
            await using var gzip = new GZipStream(jsonStream, CompressionMode.Decompress);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<VectorRecord>(gzip, options))
            {
                if (item is not null)
                    yield return item;
            }
        }

        private static async Task WriteClusteredDatabaseAsync(float[][] centroids)
        {
            Directory.CreateDirectory(OutputDir);

            string clustersDir = Path.Combine(OutputDir, "Clusters");

            if (Directory.Exists(clustersDir))
                Directory.Delete(clustersDir, recursive: true);

            Directory.CreateDirectory(clustersDir);

            await using var labelsDat = File.Create(Path.Combine(OutputDir, "labels.dat"));
            await using var labelsIdx = File.Create(Path.Combine(OutputDir, "labels.idx"));

            var clusterStreams = new FileStream[centroids.Length];

            try
            {
                for (int i = 0; i < centroids.Length; i++)
                {
                    clusterStreams[i] = File.Create(
                        Path.Combine(clustersDir, $"{i:D4}.bin"));
                }

                int id = 0;

                await AnsiConsole.Progress()
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn
                        {
                            CompletedStyle = new Style(Color.Green),
                            RemainingStyle = new Style(Color.Grey)
                        },
                        new PercentageColumn())
                    .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Processing", maxValue: 3000000);

                    await foreach (var item in ReadJsonGzAsync())
                    {
                        if (item.Vector.Length != Dimensions)
                            throw new InvalidOperationException($"Registro {id} inválido.");

                        int clusterId = FindNearestCentroid(item.Vector, centroids);

                        // record: [id:int32][14 floats]
                        Span<byte> idBytes = stackalloc byte[sizeof(int)];
                        BitConverter.TryWriteBytes(idBytes, id);
                        clusterStreams[clusterId].Write(idBytes);

                        foreach (float value in item.Vector)
                        {
                            Span<byte> valueBytes = stackalloc byte[sizeof(float)];
                            BitConverter.TryWriteBytes(valueBytes, value);
                            clusterStreams[clusterId].Write(valueBytes);
                        }

                        // labels
                        long labelOffset = labelsDat.Position;
                        byte[] labelBytes = Encoding.UTF8.GetBytes(item.Label);

                        await labelsDat.WriteAsync(labelBytes);

                        Span<byte> offsetBytes = stackalloc byte[sizeof(long)];
                        Span<byte> lengthBytes = stackalloc byte[sizeof(int)];

                        BitConverter.TryWriteBytes(offsetBytes, labelOffset);
                        BitConverter.TryWriteBytes(lengthBytes, labelBytes.Length);

                        labelsIdx.Write(offsetBytes);
                        labelsIdx.Write(lengthBytes);

                        id++;

                        task.Description = $"Processing {id + 1} of {task.MaxValue}";
                        task.Increment(1);
                    }
                });
                Console.WriteLine($"Total records: {id}");
            }
            finally
            {
                foreach (var stream in clusterStreams)
                    stream?.Dispose();
            }
        }
    }
}
