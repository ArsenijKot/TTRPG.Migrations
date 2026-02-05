using Microsoft.Extensions.Configuration;
using Infrastructure;

class Program
{
    static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        var connectionString = config.GetConnectionString("TTRPG");

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Application Menu ===");
            Console.WriteLine("1 - Run database migrations");
            Console.WriteLine("2 - Connection pooling tests");
            Console.WriteLine("3 - Async & Cancellation demo");
            Console.WriteLine("0 - Exit");
            Console.Write("Choose option: ");

            var choice = Console.ReadLine();
            if (choice == "0")
                break;

            switch (choice)
            {
                case "1":
                    new MigrationRunner(connectionString).Run("Migrations");
                    Console.WriteLine(" Migrations completed");
                    break;

                case "2":
                    await new ConnectionPoolingTester(connectionString).RunAsync();
                    break;

                case "3":
                    await RunAsyncDemo(connectionString);
                    break;
            }

            Console.WriteLine("\nPress any key...");
            Console.ReadKey();
        }
    }

    private static async Task RunAsyncDemo(string connectionString)
    {
        var demo = new AsyncOperationsDemo(connectionString);

        Console.Clear();
        Console.WriteLine("1 - Timeout cancellation");
        Console.WriteLine("2 - Manual cancellation");
        Console.WriteLine("3 - Parallel execution");
        Console.Write("Choose demo: ");

        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                await demo.RunWithTimeoutAsync();
                break;
            case "2":
                await demo.RunWithManualCancellationAsync();
                break;
            case "3":
                await demo.RunParallelAsync();
                break;
        }
    }
}
