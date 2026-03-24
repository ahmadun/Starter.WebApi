namespace Project.Domain.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Visibility { get; set; } = Project.Domain.Enums.CategoryVisibility.Global;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? OwnerUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
