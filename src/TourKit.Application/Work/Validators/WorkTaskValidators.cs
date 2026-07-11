using FluentValidation;

namespace TourKit.Application.Work.Validators;

public sealed class CreateWorkTaskValidator : AbstractValidator<CreateWorkTaskDto>
{
    public CreateWorkTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Priority).InclusiveBetween(0, 2);
        RuleFor(x => x.Status).InclusiveBetween(0, 3);
    }
}

public sealed class UpdateWorkTaskValidator : AbstractValidator<UpdateWorkTaskDto>
{
    public UpdateWorkTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Priority).InclusiveBetween(0, 2);
        RuleFor(x => x.Status).InclusiveBetween(0, 3);
    }
}
