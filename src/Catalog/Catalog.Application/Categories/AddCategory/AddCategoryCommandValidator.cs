using FluentValidation;

namespace Catalog.Application.Categories.AddCategory;
public class AddCategoryCommandValidator : AbstractValidator<AddCategoryCommand>
{
    public AddCategoryCommandValidator()
    {
        RuleFor(c => c.name).NotEmpty();
        RuleFor(c => c.code).NotEmpty();
    }
}
