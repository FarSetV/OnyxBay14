namespace ChangeMaster;

public static partial class Program
{
    public static int Main(string[] args)
    {
        if (args.Length >= 1)
        {
            return args[0] switch
            {
                "fetch" => Fetch().GetAwaiter().GetResult(),
                "check" => Check().GetAwaiter().GetResult(),
                _ => 1
            };
        }

        Console.WriteLine("Commands: fetch, check");
        return 1;
    }
}
