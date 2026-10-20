using System.Net;
using Catalog.Application.Categories.DeleteCategory;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Moq;
using SharedKernel.Domain;

namespace Catalog.Application.UnitTests.Categories;

public sealed class DeleteCategoryCommandHandlerTests
{
    private const long CategoryId = 5;

    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Category _category = TestEntities.NewCategory(CategoryId);
    private readonly DeleteCategoryCommandHandler _handler;

    public DeleteCategoryCommandHandlerTests()
    {
        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_category);

        _handler = new DeleteCategoryCommandHandler(
            _categoryRepository.Object,
            _productRepository.Object,
            _unitOfWork.Object);
    }

    private Task<Result<long>> HandleAsync(long id = CategoryId) =>
        _handler.Handle(new DeleteCategoryCommand(id), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_CategoryHasChildren_ReturnsInUseConflict()
    {
        _categoryRepository
            .Setup(r => r.HasChildrenAsync(CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<long> result = await HandleAsync();

        AssertInUse(result);
    }

    [Fact]
    public async Task Handle_CategoryHasProducts_ReturnsInUseConflict()
    {
        _productRepository
            .Setup(r => r.AnyInCategoryAsync(CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<long> result = await HandleAsync();

        AssertInUse(result);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsNotFound()
    {
        Result<long> result = await HandleAsync(id: 404);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.NotFound(404), result.Error);
        Assert.Equal(HttpStatusCode.NotFound, result.Error.Code);
        _categoryRepository.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnusedCategory_RemovesAndSaves()
    {
        Result<long> result = await HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(CategoryId, result.Value);
        _categoryRepository.Verify(r => r.Remove(_category), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void AssertInUse(Result<long> result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.InUse(CategoryId), result.Error);
        Assert.Equal(HttpStatusCode.Conflict, result.Error.Code);
        _categoryRepository.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
