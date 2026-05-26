namespace RinhaNet.Api.VectorSearch.Strategy
{
    public interface IVectorSearchEngine : IDisposable
    {
        Task<List<SearchResult>> SearchAsync(float[] query, int topK = 5);
    }
}
