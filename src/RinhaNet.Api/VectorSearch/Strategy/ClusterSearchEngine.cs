using RinhaNet.Api.VectorSearch.Algo;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace RinhaNet.Api.VectorSearch.Strategy
{
    public class ClusterSearchEngine : IVectorSearchEngine
    {

        private const int Dimensions = 14;
        private const int VectorSizeBytes = Dimensions * sizeof(float);
        private const int ClusterRecordSizeBytes = sizeof(int) + VectorSizeBytes;

        private const int LabelIndexSizeBytes = sizeof(long) + sizeof(int);

        private readonly float[][] _centroids;
        private readonly int _clusterCount = 1024;

        private readonly AlgoSelector _calculate = new(VectorSearchType.SquaredEuclideanDistance);

        private readonly string _dataDir;
        private readonly FileStream _labelsDatStream;
        private readonly FileStream _labelsIdxStream;

        public ClusterSearchEngine(string dataDir)
        {
            _dataDir = dataDir;

            _labelsDatStream = File.OpenRead(Path.Combine(_dataDir, "labels.dat"));
            _labelsIdxStream = File.OpenRead(Path.Combine(_dataDir, "labels.idx"));

            _centroids = LoadCentroids();
        }

        public async Task<List<SearchResult>> SearchAsync(float[] query, int topK = 5)
        {
            var clusters = await FindClusters(query, 3);
            var results = new PriorityQueue<(int Id, float Score), float>();

            float[] vectorBuffer = ArrayPool<float>.Shared.Rent(Dimensions);
            try
            {
                byte[] buffer = new byte[ClusterRecordSizeBytes];

                foreach (var (Id, _) in clusters)
                {
                    using var centroidStream = File.OpenRead(Path.Combine(_dataDir, "Clusters", $"{Id:D4}.bin"));
                    centroidStream.Position = 0;

                    while (await centroidStream.ReadAsync(buffer) == ClusterRecordSizeBytes)
                    {
                        int id = BitConverter.ToInt32(buffer, 0);

                        ReadOnlySpan<float> vector =
                            MemoryMarshal.Cast<byte, float>(
                                buffer.AsSpan(sizeof(int), VectorSizeBytes));

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
            }
            finally
            {
                ArrayPool<float>.Shared.Return(vectorBuffer);
            }

            return [ ..results
                .UnorderedItems
                .Select( x => new SearchResult
                {
                    Id = x.Element.Id,
                    Score = x.Element.Score,
                    Label = ReadLabel(x.Element.Id)
                })];

        }

        private async Task<(int Id, float Score)[]> FindClusters(float[] query, int topK)
        {
            var centoidsResults = new PriorityQueue<(int Id, float Score), float>();

            for (int i = 0; i < _centroids.Length; i++)
            {
                var centroid = _centroids[i];
                float score = _calculate.Score(query, centroid);

                if (centoidsResults.Count < topK)
                {
                    centoidsResults.Enqueue((i, score), -score);
                }
                else if (score < centoidsResults.Peek().Score)
                {
                    centoidsResults.Dequeue();
                    centoidsResults.Enqueue((i, score), -score);
                }
            }

            return [.. centoidsResults.UnorderedItems.Select(c => c.Element)];
        }

        private string ReadLabel(int id)
        {
            long indexOffset = id * LabelIndexSizeBytes;

            if (indexOffset < 0 || indexOffset + LabelIndexSizeBytes > _labelsIdxStream.Length)
                throw new InvalidOperationException(
                    $"Invalid label id {id}. indexOffset={indexOffset}, labelsIdxLength={_labelsIdxStream.Length}");

            Span<byte> offsetBytes = stackalloc byte[sizeof(long)];
            Span<byte> lengthBytes = stackalloc byte[sizeof(int)];

            _labelsIdxStream.Position = indexOffset;
            _labelsIdxStream.ReadExactly(offsetBytes);
            _labelsIdxStream.ReadExactly(lengthBytes);

            long labelOffset = BitConverter.ToInt64(offsetBytes);
            int length = BitConverter.ToInt32(lengthBytes);

            if (labelOffset < 0 || length < 0 || labelOffset + length > _labelsDatStream.Length)
                throw new InvalidOperationException(
                    $"Invalid label pointer for id {id}. labelOffset={labelOffset}, length={length}, labelsDatLength={_labelsDatStream.Length}");

            byte[] labelBytes = new byte[length];

            _labelsDatStream.Position = labelOffset;
            _labelsDatStream.ReadExactly(labelBytes);

            return Encoding.UTF8.GetString(labelBytes);
        }

        private float[][] LoadCentroids()
        {
            var centroids = new float[_clusterCount][];

            using var stream = File.OpenRead(Path.Combine(_dataDir, "centroids.bin"));

            byte[] buffer = new byte[VectorSizeBytes];

            for (int i = 0; i < _clusterCount; i++)
            {
                stream.ReadExactly(buffer);

                centroids[i] =
                    MemoryMarshal
                        .Cast<byte, float>(buffer)
                        .ToArray();
            }

            return centroids;
        }

        public void Dispose()
        {
            _labelsDatStream.Dispose();
            _labelsIdxStream.Dispose();
        }
    }
}
