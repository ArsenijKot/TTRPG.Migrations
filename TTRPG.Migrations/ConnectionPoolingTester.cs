using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace Infrastructure;

public class ConnectionPoolingTester
{
    private readonly string _baseConnectionString;
    private const int Iterations = 100;

    public ConnectionPoolingTester(string baseConnectionString)
    {
        _baseConnectionString = baseConnectionString;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Connection Pooling Tests ===");
            Console.WriteLine("1 - Pooling enabled (default)");
            Console.WriteLine("2 - Pooling disabled");
            Console.WriteLine("3 - Custom pool (Min=5, Max=50)");
            Console.WriteLine("0 - Back");
            Console.Write("Choose test: ");

            var choice = Console.ReadLine();
            if (choice == "0")
                break;

            string connectionString;
            string description;

            switch (choice)
            {
                case "1":
                    connectionString = _baseConnectionString;
                    description = "Pooling ENABLED";
                    break;

                case "2":
                    connectionString = _baseConnectionString + ";Pooling=false;";
                    description = "Pooling DISABLED";
                    break;

                case "3":
                    connectionString = _baseConnectionString +
                        ";Min Pool Size=5;Max Pool Size=50;";
                    description = "Custom pool";
                    break;

                default:
                    Console.WriteLine("Invalid choice.");
                    Console.ReadKey();
                    continue;
            }

            Console.WriteLine($"\nRunning: {description}");

            var time = await RunTestAsync(connectionString);

            Console.WriteLine($" Time: {time} ms");
            Console.WriteLine("\nPress any key...");
            Console.ReadKey();
        }
    }

    private async Task<long> RunTestAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < Iterations; i++)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command =
                new SqlCommand("SELECT 1", connection);

            await command.ExecuteScalarAsync(cancellationToken);
        }

        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }
}
