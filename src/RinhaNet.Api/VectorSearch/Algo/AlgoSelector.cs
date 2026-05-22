using RinhaNet.Api.Tools;

namespace RinhaNet.Api.VectorSearch.Algo
{
    public class AlgoSelector(VectorSearchType type) : ICalculate
    {
        private readonly ICalculate _calculate = type switch
        {
            VectorSearchType.EuclideanDistance => new EuclideanDistance(),
            VectorSearchType.SquaredEuclideanDistance => new SquaredEuclideanDistance(),
            _ => throw new NotSupportedException($"Tipo de busca vetorial '{type}' não suportado."),
        };

        public float Score(float[] query, ReadOnlySpan<float> reference)
        {
            using var perf = new PerfStep("Score");
            return _calculate.Score(query, reference);
        }
    }
}
