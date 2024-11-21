using FluentValidation;

namespace Catalog.Application.Products.Commands.DeleteProduct;
public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(c => c.id).NotEmpty();
    }
}