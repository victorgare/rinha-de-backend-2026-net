namespace RinhaNet.Api.VectorSearch.Algo
{
    public class AlgoSelector(VectorSearchType type) : ICalculate
    {
        private readonly ICalculate _calculate = type switch
        {
            VectorSearchType.EuclideanDistance => new EuclideanDistance(),
            _ => throw new NotSupportedException($"Tipo de busca vetorial '{type}' não suportado."),
        };

        public float Score(float[] query, Span<float> reference)
        {
            return _calculate.Score(query, reference);
        }
    }
}
