using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MemberRepository : IMemberRepository
{
    private readonly string _connectionString;

    public MemberRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<Member>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string sortBy,
        string sortDirection,
        string? filterValue,
        CancellationToken cancellationToken = default)
    {
        var allowedSortColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "name",
            ["email"] = "email",
            ["join_date"] = "join_date"
        };

        if (!allowedSortColumns.TryGetValue(sortBy, out var sortColumn))
            sortColumn = "name";

        sortDirection = sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        var offset = (pageNumber - 1) * pageSize;

        var sql = new StringBuilder(@"
            SELECT member_id, name, nickname, email, join_date
            FROM Members
            WHERE 1 = 1");

        if (!string.IsNullOrWhiteSpace(filterValue))
            sql.Append(" AND (name LIKE @filter OR email LIKE @filter)");

        sql.Append($@"
            ORDER BY {sortColumn} {sortDirection}
            OFFSET @offset ROWS
            FETCH NEXT @pageSize ROWS ONLY;");

        var result = new List<Member>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql.ToString(), connection);
        command.Parameters.AddWithValue("@offset", offset);
        command.Parameters.AddWithValue("@pageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(filterValue))
            command.Parameters.AddWithValue("@filter", $"%{filterValue}%");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new Member
            {
                MemberId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Nickname = reader.IsDBNull(2) ? null : reader.GetString(2),
                Email = reader.GetString(3),
                JoinDate = reader.GetDateTime(4)
            });
        }

        return result;
    }

    public async Task<int> CreateAsync(Member member, CancellationToken cancellationToken = default)
    {
        var sql = @"
        INSERT INTO Members (name, nickname, email, join_date)
        VALUES (@name, @nickname, @email, @join_date);

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", member.Name);
        command.Parameters.AddWithValue("@nickname", (object?)member.Nickname ?? DBNull.Value);
        command.Parameters.AddWithValue("@email", member.Email);
        command.Parameters.AddWithValue("@join_date", member.JoinDate);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }


    public async Task<Member?> GetByIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var sql = "SELECT member_id, name, nickname, email, join_date FROM Members WHERE member_id = @id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", memberId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Member
            {
                MemberId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Nickname = reader.IsDBNull(2) ? null : reader.GetString(2),
                Email = reader.GetString(3),
                JoinDate = reader.GetDateTime(4)
            };
        }

        return null;
    }

    public async Task<IEnumerable<Member>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = "SELECT member_id, name, nickname, email, join_date FROM Members";

        var members = new List<Member>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            members.Add(new Member
            {
                MemberId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Nickname = reader.IsDBNull(2) ? null : reader.GetString(2),
                Email = reader.GetString(3),
                JoinDate = reader.GetDateTime(4)
            });
        }

        return members;
    }

    public async Task UpdateAsync(Member member, CancellationToken cancellationToken = default)
    {
        var sql = @"
            UPDATE Members
            SET name = @name,
                nickname = @nickname,
                email = @email,
                join_date = @join_date
            WHERE member_id = @id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", member.Name);
        command.Parameters.AddWithValue("@nickname", (object?)member.Nickname ?? DBNull.Value);
        command.Parameters.AddWithValue("@email", member.Email);
        command.Parameters.AddWithValue("@join_date", member.JoinDate);
        command.Parameters.AddWithValue("@id", member.MemberId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var sql = "DELETE FROM Members WHERE member_id = @id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", memberId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    public async Task<IEnumerable<MemberSessionDto>> GetMembersWithSessionsAsync(
    CancellationToken cancellationToken = default)
    {
        var sql = @"
        SELECT
            m.member_id,
            m.name,
            m.nickname,
            g.title,
            s.date
        FROM Members m
        JOIN Session_Participants sp ON sp.member_id = m.member_id
        JOIN Sessions s ON s.session_id = sp.session_id
        JOIN Games g ON g.game_id = s.game_id
        ORDER BY s.date DESC;";

        var result = new List<MemberSessionDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new MemberSessionDto
            {
                MemberId = reader.GetInt32(0),
                MemberName = reader.GetString(1),
                Nickname = reader.IsDBNull(2) ? null : reader.GetString(2),
                GameTitle = reader.GetString(3),
                SessionDate = reader.GetDateTime(4)
            });
        }

        return result;
    }

    public async Task RegisterMemberWithoutTransactionAsync(
        Member member,
        int sessionId,
        int roleId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            // 1️ Створення учасника
            var createMemberCmd = new SqlCommand(@"
            INSERT INTO Members (name, email, join_date)
            OUTPUT INSERTED.member_id
            VALUES (@name, @email, @date)", connection);

            createMemberCmd.Parameters.AddWithValue("@name", member.Name);
            createMemberCmd.Parameters.AddWithValue("@email", member.Email);
            createMemberCmd.Parameters.AddWithValue("@date", member.JoinDate);

            var memberId = (int)await createMemberCmd.ExecuteScalarAsync(cancellationToken);

            // 2️ Штучна помилка
            throw new Exception("❌ Помилка під час додавання ролі");

            // 3️ Додавання ролі (не виконається)
            var addRoleCmd = new SqlCommand(@"
            INSERT INTO Session_Participants (session_id, member_id, role_id)
            VALUES (@s, @m, @r)", connection);

            addRoleCmd.Parameters.AddWithValue("@s", sessionId);
            addRoleCmd.Parameters.AddWithValue("@m", memberId);
            addRoleCmd.Parameters.AddWithValue("@r", roleId);

            await addRoleCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    public async Task RegisterMemberWithTransactionAsync(
    Member member,
    int sessionId,
    int roleId,
    CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var createMemberCmd = new SqlCommand(@"
            INSERT INTO Members (name, email, join_date)
            OUTPUT INSERTED.member_id
            VALUES (@name, @email, @date)", connection, transaction);

            createMemberCmd.Parameters.AddWithValue("@name", member.Name);
            createMemberCmd.Parameters.AddWithValue("@email", member.Email);
            createMemberCmd.Parameters.AddWithValue("@date", member.JoinDate);

            var memberId = (int)await createMemberCmd.ExecuteScalarAsync(cancellationToken);

            // Штучна помилка
            throw new Exception("❌ Помилка під час додавання ролі");

            var addRoleCmd = new SqlCommand(@"
            INSERT INTO Session_Participants (session_id, member_id, role_id)
            VALUES (@s, @m, @r)", connection, transaction);

            addRoleCmd.Parameters.AddWithValue("@s", sessionId);
            addRoleCmd.Parameters.AddWithValue("@m", memberId);
            addRoleCmd.Parameters.AddWithValue("@r", roleId);

            await addRoleCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);

            await transaction.RollbackAsync(cancellationToken);
        }
    }
}
