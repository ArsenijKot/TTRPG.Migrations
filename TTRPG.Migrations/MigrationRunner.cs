using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Data;

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
                StringSplitOptions.RemoveEmptyEntries);

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;

                    using var cmd = new SqlCommand(batch, connection, transaction);
                    cmd.CommandTimeout = 600;
                    cmd.ExecuteNonQuery();
                }

                SaveMigration(connection, name, hash, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    private void EnsureMigrationTable(SqlConnection conn)
    {
        const string sql = @"
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
        using var cmd = new SqlCommand("SELECT MigrationName, Hash FROM MigrationHistory", conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
            dict[reader.GetString(0)] = reader.GetString(1);

        return dict;
    }

    private void SaveMigration(SqlConnection conn, string name, string hash, SqlTransaction tx)
    {
        using var cmd = new SqlCommand(
            "INSERT INTO MigrationHistory (MigrationName, Hash) VALUES (@n, @h)",
            conn,
            tx);

        cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 255) { Value = name });
        cmd.Parameters.Add(new SqlParameter("@h", SqlDbType.Char, 64) { Value = hash });

        cmd.ExecuteNonQuery();
    }

    private static string ComputeHash(string sql)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sql)));
    }
}
