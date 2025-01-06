using FluentValidation;

namespace Catalog.Application.Categories.EditCategory;
public class EditCategoryCommandValidator : AbstractValidator<EditCategoryCommand>
{
    public EditCategoryCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.Description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(c => c.ParentCategoryId)
            .NotNull()
            .GreaterThanOrEqualTo(0);

    }
}