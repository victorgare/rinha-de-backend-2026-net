namespace RinhaNet.Api.VectorSearch.Algo
{
    public class EuclideanDistance : ICalculate
    {
        public float Score(ReadOnlySpan<float> query, ReadOnlySpan<float> reference)
        {
            float sum = 0f;
            for (int i = 0; i < query.Length; i++)
            {
                var diff = query[i] - reference[i];
                var diffPow = Math.Pow(diff, 2);

                sum += (float)diffPow;
            }

            return (float)Math.Sqrt(sum);
        }
    }
}
