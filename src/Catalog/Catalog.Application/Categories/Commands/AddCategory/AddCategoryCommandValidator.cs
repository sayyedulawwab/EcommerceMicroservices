using FluentValidation;

namespace Catalog.Application.Categories.Commands.AddCategory;
public class AddCategoryCommandValidator : AbstractValidator<AddCategoryCommand>
{
    public AddCategoryCommandValidator()
    {
        RuleFor(c => c.name).NotEmpty();
        RuleFor(c => c.code).NotEmpty();
    }
}
