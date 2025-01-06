using FluentValidation;

namespace Catalog.Application.Products.AddProduct;
public class AddProductCommandValidator : AbstractValidator<AddProductCommand>
{
    public AddProductCommandValidator()
    {
        RuleFor(x => x.Name)
           .NotEmpty()
           .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.PriceCurrency)
            .NotEmpty()
            .Matches(@"^[A-Z]{3}$");

        RuleFor(x => x.PriceAmount)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);

    }
}