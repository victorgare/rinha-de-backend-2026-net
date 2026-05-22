namespace RinhaNet.Api.VectorSearch.Algo
{
    public interface ICalculate
    {
        float Score(float[] query, ReadOnlySpan<float> reference);
    }
}
