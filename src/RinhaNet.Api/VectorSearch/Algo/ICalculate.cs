namespace RinhaNet.Api.VectorSearch.Algo
{
    public interface ICalculate
    {
        float Score(float[] query, Span<float> reference);
    }
}
