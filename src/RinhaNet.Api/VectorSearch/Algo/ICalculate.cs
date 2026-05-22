namespace RinhaNet.Api.VectorSearch.Algo
{
    public interface ICalculate
    {
        Task<float> Score(float[] query, Span<float> reference);
    }
}
