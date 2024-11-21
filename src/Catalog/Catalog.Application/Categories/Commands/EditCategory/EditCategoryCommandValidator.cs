using FluentValidation;

namespace Catalog.Application.Categories.Commands.EditCategory;
public class EditCategoryCommandValidator : AbstractValidator<EditCategoryCommand>
{
    public EditCategoryCommandValidator()
    {
        RuleFor(c => c.name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(c => c.parentCategoryId)
            .NotNull()
            .GreaterThanOrEqualTo(0);

    }
}