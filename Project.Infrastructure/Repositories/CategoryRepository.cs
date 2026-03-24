using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class CategoryRepository : BaseRepository<Category>, ICategoryRepository
{
    public CategoryRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    private static (string WhereClause, DynamicParameters Parameters) BuildFilters(
        int currentUserId,
        int? departmentId,
        bool includePrivate,
        bool includeInactive,
        bool includeAllDepartments,
        string? name = null,
        int? offset = null,
        int? pageSize = null)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!includeInactive)
            where.Add("c.is_active = 1");

        var departmentVisibilityCondition = includeAllDepartments
            ? "c.visibility = @DepartmentVisibility"
            : departmentId.HasValue
                ? "(c.visibility = @DepartmentVisibility AND c.department_id = @DepartmentId)"
                : "1 = 0";

        var visibilityConditions = new List<string>
        {
            "c.visibility = @GlobalVisibility",
            departmentVisibilityCondition
        };

        if (includePrivate)
            visibilityConditions.Add("(c.visibility = @PrivateVisibility AND c.owner_user_id = @CurrentUserId)");

        where.Add($"({string.Join(" OR ", visibilityConditions)})");

        if (!string.IsNullOrWhiteSpace(name))
            where.Add("c.category_name LIKE '%' + @Name + '%'");

        parameters.Add("CurrentUserId", currentUserId);
        parameters.Add("DepartmentId", departmentId);
        parameters.Add("GlobalVisibility", CategoryVisibility.Global);
        parameters.Add("DepartmentVisibility", CategoryVisibility.Department);
        parameters.Add("PrivateVisibility", CategoryVisibility.Private);

        if (!string.IsNullOrWhiteSpace(name))
            parameters.Add("Name", name.Trim());

        if (offset.HasValue)
            parameters.Add("Offset", offset.Value);

        if (pageSize.HasValue)
            parameters.Add("PageSize", pageSize.Value);

        return (string.Join(" AND ", where), parameters);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(int currentUserId, CategoryFilterParams filters)
    {
        var (whereClause, parameters) = BuildFilters(
            currentUserId,
            filters.DepartmentId,
            filters.IncludePrivate,
            filters.IncludeInactive,
            filters.IncludeAllDepartments
        );

        var sql = $"""
            SELECT
                c.*,
                d.department_name AS DepartmentName
            FROM categories c
            LEFT JOIN departments d ON d.department_id = c.department_id
            WHERE {whereClause}
            ORDER BY
                CASE c.visibility
                    WHEN @GlobalVisibility THEN 0
                    WHEN @DepartmentVisibility THEN 1
                    ELSE 2
                END,
                ISNULL(d.department_name, ''),
                c.category_name
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Category>(sql, parameters);
    }

    public async Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(int currentUserId, CategoryPagedFilterParams filters)
    {
        var (whereClause, parameters) = BuildFilters(
            currentUserId,
            filters.DepartmentId,
            filters.IncludePrivate,
            filters.IncludeInactive,
            filters.IncludeAllDepartments,
            filters.Name,
            filters.Offset,
            filters.PageSize
        );

        var countSql = $"""
            SELECT COUNT(*)
            FROM categories c
            LEFT JOIN departments d ON d.department_id = c.department_id
            WHERE {whereClause}
            """;

        var dataSql = $"""
            SELECT
                c.*,
                d.department_name AS DepartmentName
            FROM categories c
            LEFT JOIN departments d ON d.department_id = c.department_id
            WHERE {whereClause}
            ORDER BY
                CASE c.visibility
                    WHEN @GlobalVisibility THEN 0
                    WHEN @DepartmentVisibility THEN 1
                    ELSE 2
                END,
                ISNULL(d.department_name, ''),
                c.category_name
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        using var connection = _connectionFactory.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        if (totalCount == 0)
            return ([], 0);

        var items = await connection.QueryAsync<Category>(dataSql, parameters);
        return (items, totalCount);
    }

    public async Task<Category?> GetByIdAsync(int id, int currentUserId)
    {
        const string sql = """
            SELECT
                c.*,
                d.department_name AS DepartmentName
            FROM categories c
            LEFT JOIN departments d ON d.department_id = c.department_id
            WHERE c.category_id = @Id
              AND (
                    c.visibility = @GlobalVisibility
                    OR c.visibility = @DepartmentVisibility
                    OR (c.visibility = @PrivateVisibility AND c.owner_user_id = @CurrentUserId)
              )
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Category>(sql, new
        {
            Id = id,
            CurrentUserId = currentUserId,
            GlobalVisibility = CategoryVisibility.Global,
            DepartmentVisibility = CategoryVisibility.Department,
            PrivateVisibility = CategoryVisibility.Private
        });
    }

    public async Task<Category> CreateAsync(Category category)
    {
        const string sql = """
            INSERT INTO categories (
                category_name,
                visibility,
                department_id,
                owner_user_id,
                is_active,
                created_at,
                updated_at
            )
            OUTPUT INSERTED.*
            VALUES (
                @CategoryName,
                @Visibility,
                @DepartmentId,
                @OwnerUserId,
                @IsActive,
                @CreatedAt,
                @UpdatedAt
            )
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Category>(sql, new
        {
            category.CategoryName,
            category.Visibility,
            category.DepartmentId,
            category.OwnerUserId,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt
        });
    }

    public async Task<bool> UpdateAsync(Category category)
    {
        const string sql = """
            UPDATE categories
            SET category_name = @CategoryName,
                visibility = @Visibility,
                department_id = @DepartmentId,
                owner_user_id = @OwnerUserId,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE category_id = @CategoryId
            """;
        var affected = await ExecuteAsync(sql, new
        {
            category.CategoryName,
            category.Visibility,
            category.DepartmentId,
            category.OwnerUserId,
            category.IsActive,
            category.UpdatedAt,
            category.CategoryId
        });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM categories WHERE category_id = @Id";
        var affected = await ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }
}
