using FluentValidation;

namespace Catalog.Application.Products.Commands.AddProduct;
public class AddProductCommandValidator : AbstractValidator<AddProductCommand>
{
    public AddProductCommandValidator()
    {
        RuleFor(c => c.name).NotEmpty();
        RuleFor(c => c.description).NotEmpty();
        RuleFor(c => c.priceCurrency).NotEmpty();
        RuleFor(c => c.priceAmount).NotEmpty();
        RuleFor(c => c.quantity).NotEmpty();
        RuleFor(c => c.categoryId).NotEmpty();

    }
}
