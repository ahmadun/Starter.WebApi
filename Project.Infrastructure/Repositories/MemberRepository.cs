using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class MemberRepository : BaseRepository<Member>, IMemberRepository
{
    public MemberRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<(IEnumerable<Member> Items, int TotalCount)> GetAllAsync(long tenantId, MemberFilterParams filters)
    {
        var where = new List<string> { "tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, Offset = filters.Offset, PageSize = filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(member_no LIKE @Search OR employee_code LIKE @Search OR full_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(filters.MemberStatus))
        {
            where.Add("member_status = @MemberStatus");
            parameters.Add("MemberStatus", filters.MemberStatus);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM coop.members WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<Member>($"""
            SELECT member_id, tenant_id, member_no, employee_code, full_name, identity_no, phone_number, email, address_line, join_date, member_status, notes, created_at, updated_at
            FROM coop.members
            WHERE {whereClause}
            ORDER BY member_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public Task<Member?> GetByIdAsync(long tenantId, long memberId) => QuerySingleOrDefaultAsync(
        """
        SELECT member_id, tenant_id, member_no, employee_code, full_name, identity_no, phone_number, email, address_line, join_date, member_status, notes, created_at, updated_at
        FROM coop.members
        WHERE tenant_id = @TenantId AND member_id = @MemberId
        """, new { TenantId = tenantId, MemberId = memberId });

    public async Task<IEnumerable<Member>> LookupAsync(long tenantId, string query, int limit)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 25);
        var exact = query.Trim();
        using var connection = CreateConnection();
        return await connection.QueryAsync<Member>(
            """
            SELECT TOP (@Limit)
                   member_id, tenant_id, member_no, employee_code, full_name, identity_no, phone_number, email, address_line, join_date, member_status, notes, created_at, updated_at
            FROM coop.members
            WHERE tenant_id = @TenantId
              AND member_status = 'active'
              AND (member_no LIKE @Search OR employee_code LIKE @Search OR full_name LIKE @Search OR phone_number LIKE @Search)
            ORDER BY
              CASE
                WHEN member_no = @Exact THEN 0
                WHEN employee_code = @Exact THEN 1
                WHEN phone_number = @Exact THEN 2
                WHEN full_name = @Exact THEN 3
                WHEN member_no LIKE @StartsWith THEN 4
                WHEN employee_code LIKE @StartsWith THEN 5
                WHEN full_name LIKE @StartsWith THEN 6
                ELSE 7
              END,
              full_name,
              member_id DESC
            """,
            new
            {
                TenantId = tenantId,
                Limit = normalizedLimit,
                Exact = exact,
                Search = $"%{exact}%",
                StartsWith = $"{exact}%"
            });
    }

    public async Task<bool> MemberNoExistsAsync(long tenantId, string memberNo, long? excludeMemberId = null)
        => await ExistsAsync("member_no", tenantId, memberNo, excludeMemberId);

    public async Task<bool> EmployeeCodeExistsAsync(long tenantId, string? employeeCode, long? excludeMemberId = null)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
            return false;
        return await ExistsAsync("employee_code", tenantId, employeeCode, excludeMemberId);
    }

    public Task<long> CreateAsync(Member member) => ExecuteScalarAsync<long>(
        """
        INSERT INTO coop.members (tenant_id, member_no, employee_code, full_name, identity_no, phone_number, email, address_line, join_date, member_status, notes, created_at, updated_at)
        VALUES (@TenantId, @MemberNo, @EmployeeCode, @FullName, @IdentityNo, @PhoneNumber, @Email, @AddressLine, @JoinDate, @MemberStatus, @Notes, @CreatedAt, @UpdatedAt);
        SELECT CAST(SCOPE_IDENTITY() AS bigint);
        """, member)!;

    public async Task<bool> UpdateAsync(Member member)
        => await ExecuteAsync(
            """
            UPDATE coop.members
            SET employee_code = @EmployeeCode, full_name = @FullName, identity_no = @IdentityNo, phone_number = @PhoneNumber,
                email = @Email, address_line = @AddressLine, join_date = @JoinDate, member_status = @MemberStatus,
                notes = @Notes, updated_at = @UpdatedAt
            WHERE tenant_id = @TenantId AND member_id = @MemberId
            """, member) > 0;

    private async Task<bool> ExistsAsync(string column, long tenantId, string value, long? excludeId)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(1)
            FROM coop.members
            WHERE tenant_id = @TenantId AND {column} = @Value
              AND (@ExcludeId IS NULL OR member_id <> @ExcludeId)
            """, new { TenantId = tenantId, Value = value, ExcludeId = excludeId }) > 0;
    }
}
