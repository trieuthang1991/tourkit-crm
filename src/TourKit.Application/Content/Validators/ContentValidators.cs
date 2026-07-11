using FluentValidation;

namespace TourKit.Application.Content.Validators;

public sealed class CreatePostCategoryValidator : AbstractValidator<CreatePostCategoryDto>
{
    public CreatePostCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty();
    }
}

public sealed class UpdatePostCategoryValidator : AbstractValidator<UpdatePostCategoryDto>
{
    public UpdatePostCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty();
    }
}

public sealed class CreatePostValidator : AbstractValidator<CreatePostDto>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Status).InclusiveBetween(0, 1);
    }
}

public sealed class UpdatePostValidator : AbstractValidator<UpdatePostDto>
{
    public UpdatePostValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Status).InclusiveBetween(0, 1);
    }
}
