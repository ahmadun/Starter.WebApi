using System.Data;
using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<Tenant?> GetTenantByCodeAsync(string tenantCode)
    {
        const string sql = """
            SELECT tenant_id, tenant_code, tenant_name, legal_name, phone_number, email, address_line, is_active, created_at, updated_at
            FROM coop.tenants
            WHERE tenant_code = @TenantCode
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Tenant>(sql, new { TenantCode = tenantCode });
    }

    public async Task<User?> GetByTenantAndUsernameAsync(string tenantCode, string username)
    {
        const string sql = """
            SELECT u.user_id, u.tenant_id, u.member_id, u.username, u.email, u.password_hash, u.display_name, u.user_type,
                   u.is_active, u.last_login_at, u.failed_login_attempts, u.lockout_until, u.refresh_token, u.refresh_token_expires_at,
                   u.created_at, u.updated_at,
                   m.member_id, m.member_no, m.employee_code, m.full_name, m.identity_no, m.phone_number, m.email, m.address_line,
                   m.join_date, m.member_status, m.notes, m.created_at, m.updated_at
            FROM coop.users u
            INNER JOIN coop.tenants t ON t.tenant_id = u.tenant_id
            LEFT JOIN coop.members m ON m.member_id = u.member_id
            WHERE t.tenant_code = @TenantCode AND u.username = @Username
            """;

        using var connection = CreateConnection();
        var user = (await connection.QueryAsync<User, Member, User>(
            sql,
            (u, m) =>
            {
                u.Member = m?.MemberId > 0 ? m : null;
                return u;
            },
            new { TenantCode = tenantCode, Username = username },
            splitOn: "member_id")).SingleOrDefault();

        if (user is null)
            return null;

        user.Roles = await GetRoleCodesAsync(connection, user.UserId);
        return user;
    }

    public async Task<User?> GetByIdAsync(long userId)
    {
        const string sql = """
            SELECT u.user_id, u.tenant_id, u.member_id, u.username, u.email, u.password_hash, u.display_name, u.user_type,
                   u.is_active, u.last_login_at, u.failed_login_attempts, u.lockout_until, u.refresh_token, u.refresh_token_expires_at,
                   u.created_at, u.updated_at,
                   m.member_id, m.member_no, m.employee_code, m.full_name, m.identity_no, m.phone_number, m.email, m.address_line,
                   m.join_date, m.member_status, m.notes, m.created_at, m.updated_at
            FROM coop.users u
            LEFT JOIN coop.members m ON m.member_id = u.member_id
            WHERE u.user_id = @UserId
            """;

        using var connection = CreateConnection();
        var user = (await connection.QueryAsync<User, Member, User>(
            sql,
            (u, m) =>
            {
                u.Member = m?.MemberId > 0 ? m : null;
                return u;
            },
            new { UserId = userId },
            splitOn: "member_id")).SingleOrDefault();

        if (user is null)
            return null;

        user.Roles = await GetRoleCodesAsync(connection, user.UserId);
        return user;
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        const string sql = """
            SELECT TOP 1 u.user_id
            FROM coop.users u
            WHERE u.refresh_token = @RefreshToken
              AND u.refresh_token_expires_at > @Now
            """;

        using var connection = CreateConnection();
        var userId = await connection.QuerySingleOrDefaultAsync<long?>(sql, new { RefreshToken = refreshToken, Now = DateTime.UtcNow });
        return userId.HasValue ? await GetByIdAsync(userId.Value) : null;
    }

    public async Task<User?> GetByTenantAndUsernameOrEmailAsync(string tenantCode, string usernameOrEmail)
    {
        const string sql = """
            SELECT TOP 1 u.user_id
            FROM coop.users u
            INNER JOIN coop.tenants t ON t.tenant_id = u.tenant_id
            WHERE t.tenant_code = @TenantCode
              AND (u.username = @UsernameOrEmail OR u.email = @UsernameOrEmail)
            """;

        using var connection = CreateConnection();
        var userId = await connection.QuerySingleOrDefaultAsync<long?>(sql, new { TenantCode = tenantCode, UsernameOrEmail = usernameOrEmail });
        return userId.HasValue ? await GetByIdAsync(userId.Value) : null;
    }

    public async Task<User?> GetMemberRegistrationCandidateAsync(string tenantCode, string memberNo)
    {
        var tenant = await GetTenantByCodeAsync(tenantCode);
        if (tenant is null)
            return null;

        const string sql = """
            SELECT m.member_id, m.tenant_id, m.member_no, m.employee_code, m.full_name, m.identity_no, m.phone_number, m.email, m.address_line,
                   m.join_date, m.member_status, m.notes, m.created_at, m.updated_at
            FROM coop.members m
            WHERE m.tenant_id = @TenantId
              AND m.member_no = @MemberNo
              AND NOT EXISTS (
                  SELECT 1
                  FROM coop.users u
                  WHERE u.member_id = m.member_id
              )
            """;

        using var connection = CreateConnection();
        var member = await connection.QuerySingleOrDefaultAsync<Member>(sql, new { tenant.TenantId, MemberNo = memberNo });
        if (member is null)
            return null;

        return new User
        {
            TenantId = tenant.TenantId,
            MemberId = member.MemberId,
            Member = member,
            Roles = [RoleConstants.Member]
        };
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(long tenantId, UserFilterParams filters)
    {
        var where = new List<string> { "u.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, Offset = filters.Offset, PageSize = filters.PageSize });

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(u.username LIKE @Search OR u.email LIKE @Search OR u.display_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(filters.UserType))
        {
            where.Add("u.user_type = @UserType");
            parameters.Add("UserType", filters.UserType);
        }

        if (filters.IsActive.HasValue)
        {
            where.Add("u.is_active = @IsActive");
            parameters.Add("IsActive", filters.IsActive.Value);
        }

        var whereClause = string.Join(" AND ", where);

        var countSql = $"SELECT COUNT(*) FROM coop.users u WHERE {whereClause}";
        var sql = $"""
            SELECT u.user_id, u.tenant_id, u.member_id, u.username, u.email, u.password_hash, u.display_name, u.user_type,
                   u.is_active, u.last_login_at, u.failed_login_attempts, u.lockout_until, u.refresh_token, u.refresh_token_expires_at,
                   u.created_at, u.updated_at
            FROM coop.users u
            WHERE {whereClause}
            ORDER BY u.user_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<User>(sql, parameters)).ToList();
        await PopulateRolesAsync(connection, items);
        return (items, totalCount);
    }

    public async Task<IEnumerable<User>> GetOptionsAsync(long tenantId, UserOptionFilterParams filters)
    {
        var where = new List<string> { "u.tenant_id = @TenantId", "u.is_active = 1" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, Top = filters.Top });

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(u.username LIKE @Search OR u.email LIKE @Search OR u.display_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }

        var sql = $"""
            SELECT TOP (@Top)
                u.user_id, u.tenant_id, u.member_id, u.username, u.email, u.password_hash, u.display_name, u.user_type,
                u.is_active, u.last_login_at, u.failed_login_attempts, u.lockout_until, u.refresh_token, u.refresh_token_expires_at,
                u.created_at, u.updated_at
            FROM coop.users u
            WHERE {string.Join(" AND ", where)}
            ORDER BY u.display_name, u.username
            """;

        using var connection = CreateConnection();
        var items = (await connection.QueryAsync<User>(sql, parameters)).ToList();
        await PopulateRolesAsync(connection, items);
        return items;
    }

    public async Task<bool> UsernameExistsAsync(long tenantId, string username, long? excludeUserId = null)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM coop.users
            WHERE tenant_id = @TenantId
              AND username = @Username
              AND (@ExcludeUserId IS NULL OR user_id <> @ExcludeUserId)
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, Username = username, ExcludeUserId = excludeUserId }) > 0;
    }

    public async Task<bool> EmailExistsAsync(long tenantId, string email, long? excludeUserId = null)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM coop.users
            WHERE tenant_id = @TenantId
              AND email = @Email
              AND (@ExcludeUserId IS NULL OR user_id <> @ExcludeUserId)
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, Email = email, ExcludeUserId = excludeUserId }) > 0;
    }

    public async Task<long> CreateAsync(User user, IReadOnlyCollection<string> roles)
    {
        const string sql = """
            INSERT INTO coop.users (
                tenant_id, member_id, username, email, password_hash, display_name, user_type,
                is_active, created_at, updated_at, refresh_token, refresh_token_expires_at
            )
            VALUES (
                @TenantId, @MemberId, @Username, @Email, @PasswordHash, @DisplayName, @UserType,
                @IsActive, @CreatedAt, @UpdatedAt, @RefreshToken, @RefreshTokenExpiresAt
            );

            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        var userId = await connection.ExecuteScalarAsync<long>(sql, user, transaction);
        await ReplaceRolesAsync(connection, transaction, userId, roles);
        transaction.Commit();
        return userId;
    }

    public async Task<bool> UpdateAsync(User user, IReadOnlyCollection<string> roles)
    {
        const string sql = """
            UPDATE coop.users
            SET username = @Username,
                email = @Email,
                display_name = @DisplayName,
                user_type = @UserType,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId
              AND tenant_id = @TenantId
            """;

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        var affected = await connection.ExecuteAsync(sql, user, transaction);
        if (affected > 0)
        {
            await ReplaceRolesAsync(connection, transaction, user.UserId, roles);
        }

        transaction.Commit();
        return affected > 0;
    }

    public async Task<bool> UpdatePasswordAsync(long userId, string newPasswordHash)
    {
        const string sql = """
            UPDATE coop.users
            SET password_hash = @PasswordHash,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, new { UserId = userId, PasswordHash = newPasswordHash, UpdatedAt = DateTime.UtcNow }) > 0;
    }

    public async Task<bool> SoftDeleteAsync(long tenantId, long userId)
    {
        const string sql = """
            UPDATE coop.users
            SET is_active = 0,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId
              AND tenant_id = @TenantId
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, new { UserId = userId, TenantId = tenantId, UpdatedAt = DateTime.UtcNow }) > 0;
    }

    public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token)
    {
        const string sql = """
            SELECT password_reset_token_id, user_id, token, expires_at, created_at, consumed_at
            FROM coop.password_reset_tokens
            WHERE token = @Token
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PasswordResetToken>(sql, new { Token = token });
    }

    public async Task<long> CreatePasswordResetTokenAsync(long userId, string token, DateTime expiresAt)
    {
        const string sql = """
            INSERT INTO coop.password_reset_tokens (user_id, token, expires_at, created_at, consumed_at)
            VALUES (@UserId, @Token, @ExpiresAt, sysutcdatetime(), NULL);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, new { UserId = userId, Token = token, ExpiresAt = expiresAt });
    }

    public async Task InvalidatePasswordResetTokensAsync(long userId)
    {
        const string sql = """
            UPDATE coop.password_reset_tokens
            SET consumed_at = COALESCE(consumed_at, sysutcdatetime())
            WHERE user_id = @UserId
              AND consumed_at IS NULL
            """;

        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId });
    }

    public async Task ConsumePasswordResetTokenAsync(long passwordResetTokenId, DateTime consumedAt)
    {
        const string sql = """
            UPDATE coop.password_reset_tokens
            SET consumed_at = @ConsumedAt
            WHERE password_reset_token_id = @PasswordResetTokenId
            """;

        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { PasswordResetTokenId = passwordResetTokenId, ConsumedAt = consumedAt });
    }

    public Task UpdateLastLoginAsync(long userId) => ExecuteSimpleAsync(
        "UPDATE coop.users SET last_login_at = @Now WHERE user_id = @UserId",
        new { UserId = userId, Now = DateTime.UtcNow });

    public Task IncrementFailedLoginAttemptsAsync(long userId) => ExecuteSimpleAsync(
        "UPDATE coop.users SET failed_login_attempts = failed_login_attempts + 1 WHERE user_id = @UserId",
        new { UserId = userId });

    public Task ResetFailedLoginAttemptsAsync(long userId) => ExecuteSimpleAsync(
        "UPDATE coop.users SET failed_login_attempts = 0, lockout_until = NULL WHERE user_id = @UserId",
        new { UserId = userId });

    public Task LockAccountAsync(long userId, DateTime lockoutUntil) => ExecuteSimpleAsync(
        "UPDATE coop.users SET failed_login_attempts = 0, lockout_until = @LockoutUntil WHERE user_id = @UserId",
        new { UserId = userId, LockoutUntil = lockoutUntil });

    public Task StoreRefreshTokenAsync(long userId, string refreshToken, DateTime expiresAt) => ExecuteSimpleAsync(
        "UPDATE coop.users SET refresh_token = @RefreshToken, refresh_token_expires_at = @ExpiresAt WHERE user_id = @UserId",
        new { UserId = userId, RefreshToken = refreshToken, ExpiresAt = expiresAt });

    private async Task ExecuteSimpleAsync(string sql, object param)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, param);
    }

    private static async Task<List<string>> GetRoleCodesAsync(IDbConnection connection, long userId)
    {
        const string sql = """
            SELECT r.role_code
            FROM coop.user_roles ur
            INNER JOIN coop.roles r ON r.role_id = ur.role_id
            WHERE ur.user_id = @UserId
            ORDER BY r.role_code
            """;

        return (await connection.QueryAsync<string>(sql, new { UserId = userId })).ToList();
    }

    private static async Task PopulateRolesAsync(IDbConnection connection, IEnumerable<User> users)
    {
        var userList = users.ToList();
        if (userList.Count == 0)
            return;

        const string sql = """
            SELECT ur.user_id, r.role_code
            FROM coop.user_roles ur
            INNER JOIN coop.roles r ON r.role_id = ur.role_id
            WHERE ur.user_id IN @UserIds
            """;

        var roleRows = await connection.QueryAsync<(long UserId, string RoleCode)>(sql, new { UserIds = userList.Select(x => x.UserId).ToArray() });
        var lookup = roleRows.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.Select(v => v.RoleCode).OrderBy(v => v).ToList());

        foreach (var user in userList)
        {
            user.Roles = lookup.TryGetValue(user.UserId, out var roles) ? roles : [];
        }
    }

    private static async Task ReplaceRolesAsync(IDbConnection connection, IDbTransaction transaction, long userId, IReadOnlyCollection<string> roles)
    {
        await connection.ExecuteAsync("DELETE FROM coop.user_roles WHERE user_id = @UserId", new { UserId = userId }, transaction);

        const string sql = """
            INSERT INTO coop.user_roles (user_id, role_id, assigned_at)
            SELECT @UserId, r.role_id, sysutcdatetime()
            FROM coop.roles r
            WHERE r.role_code IN @RoleCodes
            """;

        if (roles.Count > 0)
        {
            await connection.ExecuteAsync(sql, new { UserId = userId, RoleCodes = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() }, transaction);
        }
    }
}
