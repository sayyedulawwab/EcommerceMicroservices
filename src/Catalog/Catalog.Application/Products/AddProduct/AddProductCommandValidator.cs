using FluentValidation;
using SharedKernel.Domain;

namespace Catalog.Application.Products.AddProduct;

public class AddProductCommandValidator : AbstractValidator<AddProductCommand>
{
    public AddProductCommandValidator()
    {
        RuleFor(x => x.Name)
           .NotEmpty()
           .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotNull()
            .MaximumLength(1000);

        RuleFor(x => x.PriceCurrency)
            .Must(Currency.IsSupported)
            .WithMessage("The currency is not supported");

        RuleFor(x => x.PriceAmount)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.CategoryId)
            .NotEmpty();

    }
}
