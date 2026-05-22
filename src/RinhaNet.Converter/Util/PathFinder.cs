namespace RinhaNet.Converter.Util
{
    public static class PathFinder
    {
        public static string FindSolutionPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir is not null)
            {
                if (dir.GetFiles("*.slnx").Length > 0)
                    return dir.FullName;

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not find solution root.");
        }
    }
}
