using FluentValidation;

namespace Catalog.Application.Products.AddProduct;
public class AddProductCommandValidator : AbstractValidator<AddProductCommand>
{
    public AddProductCommandValidator()
    {
        RuleFor(x => x.name)
           .NotEmpty()
           .MaximumLength(200);

        RuleFor(x => x.description)
            .MaximumLength(1000);

        RuleFor(x => x.priceCurrency)
            .NotEmpty()
            .Matches(@"^[A-Z]{3}$");

        RuleFor(x => x.priceAmount)
            .GreaterThan(0);

        RuleFor(x => x.quantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.categoryId)
            .GreaterThan(0);

    }
}