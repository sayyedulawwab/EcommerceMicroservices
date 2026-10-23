using SharedKernel.Messaging;

namespace Cart.Application.Carts.AddCartItem;

public record AddCartItemCommand(
    Guid UserId,
    Guid ProductId,
    string ProductName,
    decimal PriceAmount,
    string PriceCurrency,
    int Quantity) : ICommand<CartResponse>;
