using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Categories.AddCategory;
using Catalog.Domain.Categories;
using Moq;
using SharedKernel.Domain;

namespace Catalog.Application.UnitTests.Categories;

public sealed class AddCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly AddCategoryCommandHandler _handler;

    public AddCategoryCommandHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(TestEntities.Now);

        _handler = new AddCategoryCommandHandler(
            _categoryRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object);
    }

    [Fact]
    public async Task Handle_UnknownParent_ReturnsParentNotFoundAndAddsNothing()
    {
        _categoryRepository
            .Setup(r => r.ExistsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result<long> result = await _handler.Handle(
            new AddCategoryCommand("Laptops", "Portable computers", 99),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.ParentNotFound(99), result.Error);
        _categoryRepository.Verify(r => r.Add(It.IsAny<Category>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingParent_AddsCategoryAndSaves()
    {
        _categoryRepository
            .Setup(r => r.ExistsAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<long> result = await _handler.Handle(
            new AddCategoryCommand("Laptops", "Portable computers", 3),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _categoryRepository.Verify(
            r => r.Add(It.Is<Category>(c => c.Name == "Laptops" && c.ParentCategoryId == 3 && c.CreatedOnUtc == TestEntities.Now)),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoParent_SkipsParentCheckAndAddsRootCategory()
    {
        Result<long> result = await _handler.Handle(
            new AddCategoryCommand("Electronics", "Devices", null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _categoryRepository.Verify(r => r.ExistsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _categoryRepository.Verify(r => r.Add(It.Is<Category>(c => c.ParentCategoryId == null)), Times.Once);
    }
}
