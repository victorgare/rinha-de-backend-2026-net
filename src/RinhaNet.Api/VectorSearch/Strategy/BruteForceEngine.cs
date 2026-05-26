using RinhaNet.Api.Tools;
using RinhaNet.Api.VectorSearch.Algo;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace RinhaNet.Api.VectorSearch.Strategy
{
    public sealed class BruteForceEngine(string dataDir) : IVectorSearchEngine
    {
        private const int Dimensions = 14;
        private const int VectorSizeBytes = Dimensions * sizeof(float);
        private const int LabelIndexSizeBytes = sizeof(long) + sizeof(int);

        private readonly FileStream _vectorsStream = File.OpenRead(Path.Combine(dataDir, "vectors.bin"));
        private readonly FileStream _labelsDatStream = File.OpenRead(Path.Combine(dataDir, "labels.dat"));
        private readonly FileStream _labelsIdxStream = File.OpenRead(Path.Combine(dataDir, "labels.idx"));

        private readonly AlgoSelector _calculate = new(VectorSearchType.SquaredEuclideanDistance);

        private const int RecordsPerBlock = 16384;
        private const int BlockSizeBytes = RecordsPerBlock * VectorSizeBytes;
        private readonly byte[] _blockBuffer = new byte[BlockSizeBytes];

        public async Task<List<SearchResult>> SearchAsync(float[] query, int topK = 5)
        {
            using var perf = new PerfStep("Search");
            var results = new PriorityQueue<(int Id, float Score), float>();

            float[] vectorBuffer = ArrayPool<float>.Shared.Rent(Dimensions);
            try
            {
                int totalRecords = (int)(_vectorsStream.Length / VectorSizeBytes);
                _vectorsStream.Position = 0;
                int globalId = 0;

                while (globalId < totalRecords)
                {
                    int bytesRead = await _vectorsStream.ReadAsync(_blockBuffer);

                    if (bytesRead <= 0)
                        break;

                    int recordsRead = bytesRead / VectorSizeBytes;

                    ReadOnlySpan<byte> blockBytes = _blockBuffer.AsSpan(0, recordsRead * VectorSizeBytes);
                    ReadOnlySpan<float> blockFloats = MemoryMarshal.Cast<byte, float>(blockBytes);

                    for (int localRecord = 0; localRecord < recordsRead; localRecord++)
                    {
                        int floatOffset = localRecord * Dimensions;

                        ReadOnlySpan<float> vector = blockFloats.Slice(floatOffset, Dimensions);

                        float score = _calculate.Score(query, vector);

                        if (results.Count < topK)
                        {
                            results.Enqueue((globalId, score), -score);
                        }
                        else if (score < results.Peek().Score)
                        {
                            results.Dequeue();
                            results.Enqueue((globalId, score), -score);
                        }

                        globalId++;
                    }
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(vectorBuffer);
            }

            return [.. results
                .UnorderedItems
                .Select(x => new SearchResult
                {
                    Id = x.Element.Id,
                    Score = x.Element.Score,
                    Label = ReadLabel(x.Element.Id)
                })];
        }

        private string ReadLabel(int id)
        {
            long indexOffset = id * LabelIndexSizeBytes;

            Span<byte> offsetBytes = stackalloc byte[sizeof(long)];
            Span<byte> lengthBytes = stackalloc byte[sizeof(int)];

            _labelsIdxStream.Position = indexOffset;
            _labelsIdxStream.ReadExactly(offsetBytes);
            _labelsIdxStream.ReadExactly(lengthBytes);

            long labelOffset = BitConverter.ToInt64(offsetBytes);
            int length = BitConverter.ToInt32(lengthBytes);

            byte[] labelBytes = new byte[length];

            _labelsDatStream.Position = labelOffset;
            _labelsDatStream.ReadExactly(labelBytes);

            return Encoding.UTF8.GetString(labelBytes);
        }

        public void Dispose()
        {
            _vectorsStream.Dispose();
            _labelsDatStream.Dispose();
            _labelsIdxStream.Dispose();
        }
    }
}
