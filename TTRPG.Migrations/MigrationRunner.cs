using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

public class MigrationRunner
{
    private readonly string _connectionString;

    public MigrationRunner(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Run(string migrationsPath)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        EnsureMigrationTable(connection);
        var applied = GetAppliedMigrations(connection);

        foreach (var file in Directory.GetFiles(migrationsPath, "*.sql").OrderBy(f => f))
        {
            var name = Path.GetFileName(file);
            var sqlText = File.ReadAllText(file);
            var hash = ComputeHash(sqlText);

            if (applied.TryGetValue(name, out var oldHash) && oldHash == hash)
            {
                Console.WriteLine($"⏭ Skipped {name}");
                continue;
            }

            Console.WriteLine($"▶ Applying {name}");

            
            var batches = sqlText.Split(
                new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO\n", "\nGO\r\n" },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                using var cmd = new SqlCommand(batch, connection);
                cmd.ExecuteNonQuery();
            }

            SaveMigration(connection, name, hash);
        }
    }

    private void EnsureMigrationTable(SqlConnection conn)
    {
        var sql = @"
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MigrationHistory')
        BEGIN
            CREATE TABLE MigrationHistory (
                Id INT IDENTITY PRIMARY KEY,
                MigrationName NVARCHAR(255),
                Hash CHAR(64),
                AppliedAt DATETIME2 DEFAULT SYSUTCDATETIME()
            )
        END";
        using var cmd = new SqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private Dictionary<string, string> GetAppliedMigrations(SqlConnection conn)
    {
        var dict = new Dictionary<string, string>();
        var cmd = new SqlCommand("SELECT MigrationName, Hash FROM MigrationHistory", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            dict[reader.GetString(0)] = reader.GetString(1);
        return dict;
    }

    private void SaveMigration(SqlConnection conn, string name, string hash)
    {
        var cmd = new SqlCommand(
            "INSERT INTO MigrationHistory (MigrationName, Hash) VALUES (@n, @h)", conn);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@h", hash);
        cmd.ExecuteNonQuery();
    }

    private string ComputeHash(string sql)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sql)));
    }
}
