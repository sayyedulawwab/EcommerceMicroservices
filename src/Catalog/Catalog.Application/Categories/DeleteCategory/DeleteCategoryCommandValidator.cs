using FluentValidation;

namespace Catalog.Application.Categories.DeleteCategory;

public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}