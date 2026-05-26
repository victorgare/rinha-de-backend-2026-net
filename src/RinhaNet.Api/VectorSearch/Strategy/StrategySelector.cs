namespace RinhaNet.Api.VectorSearch.Strategy
{
    public class StrategySelector(VectorSearchStrategy strategy) : IVectorSearchEngine
    {
        private readonly IVectorSearchEngine _engine = strategy switch
        {
            VectorSearchStrategy.BruteForce => new BruteForceEngine("Data"),
            VectorSearchStrategy.Cluster => new ClusterSearchEngine("Data"),
            _ => throw new NotSupportedException($"Tipo de busca vetorial '{strategy}' não suportado."),
        };

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task<List<SearchResult>> SearchAsync(float[] query, int topK = 5)
        {
            return _engine.SearchAsync(query, topK);
        }

        protected virtual void Dispose(bool disposing)
        {
            _engine.Dispose();
        }
    }
}
