namespace Project.Application.DTOs;

public sealed record CategoryDto(
    int CategoryId,
    string CategoryName,
    string Visibility,
    int? DepartmentId,
    string? DepartmentName,
    int? OwnerUserId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record CategorySummaryDto(
    int CategoryId,
    string CategoryName,
    string Visibility,
    int? DepartmentId,
    string? DepartmentName,
    bool IsActive
);

public sealed record CreateCategoryDto(
    string CategoryName,
    string Visibility,
    int? DepartmentId,
    bool IsActive
);

public sealed record UpdateCategoryDto(
    string CategoryName,
    string Visibility,
    int? DepartmentId,
    bool IsActive
);

public sealed class CategoryFilterParams
{
    public int? DepartmentId { get; set; }
    public bool IncludePrivate { get; set; }
    public bool IncludeInactive { get; set; }
    public bool IncludeAllDepartments { get; set; }
}

public sealed class CategoryPagedFilterParams : Project.Application.Common.PaginationParams
{
    public string? Name { get; set; }
    public int? DepartmentId { get; set; }
    public bool IncludePrivate { get; set; }
    public bool IncludeInactive { get; set; }
    public bool IncludeAllDepartments { get; set; }
}
