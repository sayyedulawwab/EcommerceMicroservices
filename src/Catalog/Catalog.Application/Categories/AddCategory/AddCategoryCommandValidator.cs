using FluentValidation;

namespace Catalog.Application.Categories.AddCategory;
public class AddCategoryCommandValidator : AbstractValidator<AddCategoryCommand>
{
    public AddCategoryCommandValidator()
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