namespace TourKit.Application.Content;

public sealed record PostCategoryDto(Guid Id, string Name, string Slug, int SortOrder, int Status);
public sealed record CreatePostCategoryDto(string Name, string Slug, int SortOrder);
public sealed record UpdatePostCategoryDto(string Name, string Slug, int SortOrder);

public sealed record PostDto(
    Guid Id, string Title, string Slug, string? Summary, string Body,
    Guid? CategoryId, string? CategoryName, int Status, DateTimeOffset? PublishedAt);

public sealed record CreatePostDto(
    string Title, string Slug, string? Summary, string Body, Guid? CategoryId, int Status);

public sealed record UpdatePostDto(
    string Title, string Slug, string? Summary, string Body, Guid? CategoryId, int Status);
