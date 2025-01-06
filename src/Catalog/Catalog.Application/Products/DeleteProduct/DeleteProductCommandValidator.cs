using FluentValidation;

namespace Catalog.Application.Products.DeleteProduct;
public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}