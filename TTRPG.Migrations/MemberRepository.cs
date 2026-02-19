using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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

    // ---------- COMMON MAPPER (DRY FIX) ----------
    private static Member MapMember(SqlDataReader reader) => new()
    {
        MemberId = reader.GetInt32(0),
        Name = reader.GetString(1),
        Nickname = reader.IsDBNull(2) ? null : reader.GetString(2),
        Email = reader.GetString(3),
        JoinDate = reader.GetDateTime(4)
    };

    // ---------- PAGING ----------
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
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;");

        var result = new List<Member>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql.ToString(), connection);
        command.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
        command.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });

        if (!string.IsNullOrWhiteSpace(filterValue))
            command.Parameters.Add(new SqlParameter("@filter", SqlDbType.NVarChar, 255) { Value = $"%{filterValue}%" });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            result.Add(MapMember(reader));

        return result;
    }

    // ---------- CREATE ----------
    public async Task<int> CreateAsync(Member member, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Members (name, nickname, email, join_date)
            VALUES (@name, @nickname, @email, @join_date);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = member.Name });
        command.Parameters.Add(new SqlParameter("@nickname", SqlDbType.NVarChar, 200) { Value = (object?)member.Nickname ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@email", SqlDbType.NVarChar, 200) { Value = member.Email });
        command.Parameters.Add(new SqlParameter("@join_date", SqlDbType.DateTime2) { Value = member.JoinDate });

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    // ---------- READ ----------
    public async Task<Member?> GetByIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT member_id, name, nickname, email, join_date FROM Members WHERE member_id = @id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = memberId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapMember(reader) : null;
    }

    public async Task<IEnumerable<Member>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT member_id, name, nickname, email, join_date FROM Members";

        var members = new List<Member>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            members.Add(MapMember(reader));

        return members;
    }

    // ---------- UPDATE ----------
    public async Task UpdateAsync(Member member, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Members
            SET name = @name,
                nickname = @nickname,
                email = @email,
                join_date = @join_date
            WHERE member_id = @id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = member.Name });
        command.Parameters.Add(new SqlParameter("@nickname", SqlDbType.NVarChar, 200) { Value = (object?)member.Nickname ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@email", SqlDbType.NVarChar, 200) { Value = member.Email });
        command.Parameters.Add(new SqlParameter("@join_date", SqlDbType.DateTime2) { Value = member.JoinDate });
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = member.MemberId });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // ---------- DELETE ----------
    public async Task DeleteAsync(int memberId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Members WHERE member_id = @id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = memberId });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // ---------- ATOMIC REGISTER (FIXED TRANSACTION) ----------
    public async Task RegisterMemberAsync(
        Member member,
        int sessionId,
        int roleId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var createMemberCmd = new SqlCommand(@"
                INSERT INTO Members (name, email, join_date)
                OUTPUT INSERTED.member_id
                VALUES (@name, @email, @date)", connection, (SqlTransaction)transaction);

            createMemberCmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = member.Name });
            createMemberCmd.Parameters.Add(new SqlParameter("@email", SqlDbType.NVarChar, 200) { Value = member.Email });
            createMemberCmd.Parameters.Add(new SqlParameter("@date", SqlDbType.DateTime2) { Value = member.JoinDate });

            var memberId = (int)await createMemberCmd.ExecuteScalarAsync(cancellationToken);

            var addRoleCmd = new SqlCommand(@"
                INSERT INTO Session_Participants (session_id, member_id, role_id)
                VALUES (@s, @m, @r)", connection, (SqlTransaction)transaction);

            addRoleCmd.Parameters.Add(new SqlParameter("@s", SqlDbType.Int) { Value = sessionId });
            addRoleCmd.Parameters.Add(new SqlParameter("@m", SqlDbType.Int) { Value = memberId });
            addRoleCmd.Parameters.Add(new SqlParameter("@r", SqlDbType.Int) { Value = roleId });

            await addRoleCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    public async Task<IEnumerable<MemberSessionDto>> GetMembersWithSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
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

}
