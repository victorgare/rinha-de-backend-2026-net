using RinhaNet.Converter.PreProcessor;

namespace RinhaNet.Converter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //var input = args[0];
            //var output = args[1];

            await VectorDatabaseBuilder.BuildAsync();
        }
    }
}
