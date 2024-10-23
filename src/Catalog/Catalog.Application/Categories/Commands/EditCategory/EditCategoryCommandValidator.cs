using FluentValidation;

namespace Catalog.Application.Categories.Commands.EditCategory;
public class EditCategoryCommandValidator : AbstractValidator<EditCategoryCommand>
{
    public EditCategoryCommandValidator()
    {
        RuleFor(c => c.id).NotEmpty();
        RuleFor(c => c.name).NotEmpty();
        RuleFor(c => c.code).NotEmpty();

    }
}
