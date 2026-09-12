internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            string mapPath = GetStringArg(args, "--map", "Data/Maps/MAP_GRAY_RANGE.map.json");
            AnnihilationPrototypeConfig config = AnnihilationScenarioLoader.Load(mapPath, out string mapId);
            AnnihilationGateChecks.Run(config, mapId);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ANNIHILATION GATE FAILED");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string GetStringArg(string[] args, string name, string fallback)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], name, StringComparison.Ordinal))
                return args[i + 1];
        return fallback;
    }
}
