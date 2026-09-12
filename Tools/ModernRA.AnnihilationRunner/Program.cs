internal static class Program
{
    private static int Main()
    {
        try
        {
            AnnihilationGateChecks.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ANNIHILATION GATE FAILED");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
