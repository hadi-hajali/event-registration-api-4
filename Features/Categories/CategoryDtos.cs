namespace EventRegistration.Api.Features.Categories;

public sealed class CategoryResponse
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed record CreateCategoryRequest(string Name, string? Description, bool IsActive = true);

public sealed record UpdateCategoryRequest(string Name, string? Description, bool IsActive);
