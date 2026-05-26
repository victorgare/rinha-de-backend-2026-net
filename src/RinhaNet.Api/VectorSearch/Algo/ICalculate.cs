namespace RinhaNet.Api.VectorSearch.Algo
{
    public interface ICalculate
    {
        float Score(ReadOnlySpan<float> query, ReadOnlySpan<float> reference);
    }
}
