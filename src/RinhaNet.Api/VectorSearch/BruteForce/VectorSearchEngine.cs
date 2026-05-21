namespace RinhaNet.Api.VectorSearch.BruteForce
{
    using RinhaNet.Api.VectorSearch.Algo;
    using System.Text;

    public class SearchResult
    {
        public int Id { get; set; }
        public float Score { get; set; }
        public string Label { get; set; }
    }
    public sealed class VectorSearchEngine : IDisposable
    {
        private const int Dimensions = 14;
        private const int VectorSizeBytes = Dimensions * sizeof(float);
        private const int LabelIndexSizeBytes = sizeof(long) + sizeof(int);

        private readonly FileStream _vectorsStream;
        private readonly FileStream _labelsDatStream;
        private readonly FileStream _labelsIdxStream;

        private readonly byte[] _vectorBuffer = new byte[VectorSizeBytes];

        private readonly ICalculate _calculate;
        public VectorSearchEngine(string dataDir)
        {
            _vectorsStream = File.OpenRead(Path.Combine(dataDir, "vectors.bin"));
            _labelsDatStream = File.OpenRead(Path.Combine(dataDir, "labels.dat"));
            _labelsIdxStream = File.OpenRead(Path.Combine(dataDir, "labels.idx"));

            _calculate = new AlgoSelector(VectorSearchType.EuclideanDistance);
        }

        public List<SearchResult> Search(float[] query, int topK = 5)
        {
            var results = new PriorityQueue<(int Id, float Score), float>();

            int totalRecords = (int)(_vectorsStream.Length / VectorSizeBytes);

            _vectorsStream.Position = 0;

            Span<float> vector = stackalloc float[Dimensions];
            for (int id = 0; id < totalRecords; id++)
            {
                int read = _vectorsStream.Read(_vectorBuffer, 0, VectorSizeBytes);

                if (read != VectorSizeBytes)
                    break;


                for (int i = 0; i < Dimensions; i++)
                {
                    vector[i] = BitConverter.ToSingle(_vectorBuffer, i * sizeof(float));
                }

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

            return [.. results
                .UnorderedItems
                .Select(x => new SearchResult
                {
                    Id = x.Element.Id,
                    Score = x.Element.Score,
                    Label = ReadLabel(x.Element.Id)
                })
                .OrderByDescending(x => x.Score)];
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
