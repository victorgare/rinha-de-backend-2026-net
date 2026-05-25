using RinhaNet.Converter.PreProcessor;

namespace RinhaNet.Converter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //await VectorDatabaseBuilder.BuildAsync();
            await ClusterBuilder.BuildClusters();
        }
    }
}
