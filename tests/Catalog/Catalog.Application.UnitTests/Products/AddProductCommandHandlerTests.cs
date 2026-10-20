using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Products.AddProduct;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Moq;
using SharedKernel.Domain;

namespace Catalog.Application.UnitTests.Products;

public sealed class AddProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly AddProductCommandHandler _handler;

    public AddProductCommandHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(TestEntities.Now);

        _handler = new AddProductCommandHandler(
            _productRepository.Object,
            _categoryRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object);
    }

    [Fact]
    public async Task Handle_UnknownCategory_ReturnsCategoryNotFoundAndAddsNothing()
    {
        _categoryRepository
            .Setup(r => r.ExistsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result<long> result = await _handler.Handle(
            new AddProductCommand("Keyboard", "Mechanical", "USD", 50m, 10, 99),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.CategoryNotFound(99), result.Error);
        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingCategory_AddsProductAndSaves()
    {
        _categoryRepository
            .Setup(r => r.ExistsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<long> result = await _handler.Handle(
            new AddProductCommand("Keyboard", "Mechanical", "BDT", 50m, 10, 2),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _productRepository.Verify(
            r => r.Add(It.Is<Product>(p =>
                p.Name == "Keyboard" &&
                p.Price == new Money(50m, Currency.Bdt) &&
                p.Quantity == 10 &&
                p.CategoryId == 2 &&
                p.CreatedOnUtc == TestEntities.Now)),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
