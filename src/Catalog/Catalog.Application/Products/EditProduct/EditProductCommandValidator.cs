using FluentValidation;

namespace Catalog.Application.Products.EditProduct;
public class EditProductCommandValidator : AbstractValidator<EditProductCommand>
{
    public EditProductCommandValidator()
    {
        RuleFor(c => c.id).NotEmpty();
        RuleFor(c => c.name).NotEmpty();
        RuleFor(c => c.description).NotEmpty();
        RuleFor(c => c.priceCurrency).NotEmpty();
        RuleFor(c => c.priceAmount).NotEmpty();
        RuleFor(c => c.quantity).NotEmpty();
        RuleFor(c => c.categoryId).NotEmpty();

    }
}
