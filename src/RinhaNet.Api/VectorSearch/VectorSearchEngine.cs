namespace RinhaNet.Api.VectorSearch
{
    using RinhaNet.Api.Tools;
    using RinhaNet.Api.VectorSearch.Algo;
    using System.Buffers;
    using System.Runtime.InteropServices;
    using System.Text;

    public class SearchResult
    {
        public int Id { get; set; }
        public float Score { get; set; }
        public string Label { get; set; }
    }

    public sealed class VectorSearchEngine(string dataDir) : IDisposable
    {
        private const int Dimensions = 14;
        private const int VectorSizeBytes = Dimensions * sizeof(float);
        private const int LabelIndexSizeBytes = sizeof(long) + sizeof(int);

        private readonly FileStream _vectorsStream = File.OpenRead(Path.Combine(dataDir, "vectors.bin"));
        private readonly FileStream _labelsDatStream = File.OpenRead(Path.Combine(dataDir, "labels.dat"));
        private readonly FileStream _labelsIdxStream = File.OpenRead(Path.Combine(dataDir, "labels.idx"));

        private readonly byte[] _vectorBuffer = new byte[VectorSizeBytes];
        private readonly AlgoSelector _calculate = new(VectorSearchType.SquaredEuclideanDistance);

        public async Task<List<SearchResult>> SearchAsync(float[] query, int topK = 5)
        {
            using var perf = new PerfStep("Search");
            var results = new PriorityQueue<(int Id, float Score), float>();

            float[] vectorBuffer = ArrayPool<float>.Shared.Rent(Dimensions);
            try
            {
                int totalRecords = (int)(_vectorsStream.Length / VectorSizeBytes);
                _vectorsStream.Position = 0;

                for (int id = 0; id < totalRecords; id++)
                {
                    int read = await _vectorsStream.ReadAsync(_vectorBuffer.AsMemory(0, VectorSizeBytes));

                    if (read != VectorSizeBytes)
                        break;

                    var vector = MemoryMarshal.Cast<byte, float>(_vectorBuffer);
                    float score = _calculate.Score(query, vector);

                    if (results.Count < topK)
                    {
                        results.Enqueue((id, score), -score);
                    }
                    else if (score < results.Peek().Score)
                    {
                        results.Dequeue();
                        results.Enqueue((id, score), -score);
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
