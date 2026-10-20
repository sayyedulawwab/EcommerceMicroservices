using SharedKernel.Messaging;

namespace Cart.Application.Carts.AddCartItem;

public record AddCartItemCommand(
    long UserId,
    long ProductId,
    string ProductName,
    decimal PriceAmount,
    string PriceCurrency,
    int Quantity) : ICommand<CartResponse>;
