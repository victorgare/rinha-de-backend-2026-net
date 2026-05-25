using RinhaNet.Converter.Util;
using System.Reflection;

namespace RinhaNet.Converter.PreProcessor
{
    public class BaseBuilder
    {
        internal const int Dimensions = 14;
        internal static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()!.Location)!;
        internal static readonly string DataPath = Path.Combine(AssemblyPath, "Resources", "references.json.gz");
        internal static readonly string OutputDir = Path.Combine(PathFinder.FindSolutionPath(), "src", "RinhaNet.Api", "Data");
    }
}
