using Microsoft.Data.SqlClient;

namespace Infrastructure;

public class AsyncOperationsDemo
{
    private readonly string _connectionString;

    public AsyncOperationsDemo(string connectionString)
    {
        _connectionString = connectionString;
    }


    public async Task RunWithTimeoutAsync()
    {
        Console.WriteLine(" Running long operation with 3s timeout...");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        try
        {
            await ExecuteLongQueryAsync(cts.Token);
            Console.WriteLine(" Operation completed");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(" Operation cancelled by timeout");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($" SQL error: {ex.Message}");
        }
    }


    public async Task RunWithManualCancellationAsync()
    {
        using var cts = new CancellationTokenSource();

        Console.WriteLine(" Press ANY key to cancel operation...");

        _ = Task.Run(() =>
        {
            Console.ReadKey();
            cts.Cancel();
        });

        try
        {
            await ExecuteLongQueryAsync(cts.Token);
            Console.WriteLine(" Operation completed");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(" Operation cancelled by user");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($" SQL error: {ex.Message}");
        }
    }


    public async Task RunParallelAsync()
    {
        using var cts = new CancellationTokenSource();

        var tasks = new[]
        {
            ExecuteLongQueryAsync(cts.Token),
            ExecuteLongQueryAsync(cts.Token),
            ExecuteFailingQueryAsync(cts.Token)
        };

        try
        {
            await Task.WhenAll(tasks);
            Console.WriteLine(" All tasks completed");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(" Operation cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" One of tasks failed: {ex.Message}");
        }
    }

    

    private async Task ExecuteLongQueryAsync(CancellationToken token)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(token);

        await using var command = new SqlCommand(
            "WAITFOR DELAY '00:00:05'; SELECT 1;",
            connection);

        
        command.CommandTimeout = 0;

        await using var reader = await command.ExecuteReaderAsync(token);

        while (await reader.ReadAsync(token))
        {
            // спеціально пусто
        }
    }

    private async Task ExecuteFailingQueryAsync(CancellationToken token)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(token);

        await using var command = new SqlCommand(
            "SELECT * FROM NonExistingTable;",
            connection);

        command.CommandTimeout = 0;

        await command.ExecuteNonQueryAsync(token);
    }
}
