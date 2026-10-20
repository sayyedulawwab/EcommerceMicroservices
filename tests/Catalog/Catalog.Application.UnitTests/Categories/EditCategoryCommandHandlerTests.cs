using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Categories.EditCategory;
using Catalog.Domain.Categories;
using Moq;
using SharedKernel.Domain;

namespace Catalog.Application.UnitTests.Categories;

public sealed class EditCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly EditCategoryCommandHandler _handler;

    public EditCategoryCommandHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(TestEntities.Now);

        _handler = new EditCategoryCommandHandler(
            _categoryRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object);
    }

    private void SetupCategories(params Category[] categories)
    {
        foreach (Category category in categories)
        {
            _categoryRepository
                .Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
        }
    }

    private Task<Result<long>> HandleAsync(long id, long? parentCategoryId) =>
        _handler.Handle(
            new EditCategoryCommand(id, "New name", "New description", parentCategoryId),
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_ParentIsSelf_ReturnsCircularParent()
    {
        SetupCategories(TestEntities.NewCategory(1));

        Result<long> result = await HandleAsync(1, parentCategoryId: 1);

        AssertRejected(result, CategoryErrors.CircularParent(1));
    }

    [Fact]
    public async Task Handle_ParentIsDescendant_ReturnsCircularParent()
    {
        // 1 <- 2 <- 3: making 3 the parent of 1 would create the cycle 1 -> 3 -> 2 -> 1.
        SetupCategories(
            TestEntities.NewCategory(1),
            TestEntities.NewCategory(2, parentCategoryId: 1),
            TestEntities.NewCategory(3, parentCategoryId: 2));

        Result<long> result = await HandleAsync(1, parentCategoryId: 3);

        AssertRejected(result, CategoryErrors.CircularParent(1));
    }

    [Fact]
    public async Task Handle_ParentIsDirectChild_ReturnsCircularParent()
    {
        SetupCategories(
            TestEntities.NewCategory(1),
            TestEntities.NewCategory(2, parentCategoryId: 1));

        Result<long> result = await HandleAsync(1, parentCategoryId: 2);

        AssertRejected(result, CategoryErrors.CircularParent(1));
    }

    [Fact]
    public async Task Handle_UnknownParent_ReturnsParentNotFound()
    {
        SetupCategories(TestEntities.NewCategory(1));

        Result<long> result = await HandleAsync(1, parentCategoryId: 99);

        AssertRejected(result, CategoryErrors.ParentNotFound(99));
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsNotFound()
    {
        Result<long> result = await HandleAsync(404, parentCategoryId: null);

        AssertRejected(result, CategoryErrors.NotFound(404));
    }

    [Fact]
    public async Task Handle_ValidParentInAnotherBranch_UpdatesAndSaves()
    {
        // 1 (edited), 10 <- 11: moving 1 under 11 is fine.
        Category category = TestEntities.NewCategory(1);
        SetupCategories(
            category,
            TestEntities.NewCategory(10),
            TestEntities.NewCategory(11, parentCategoryId: 10));

        Result<long> result = await HandleAsync(1, parentCategoryId: 11);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        Assert.Equal(11, category.ParentCategoryId);
        Assert.Equal("New name", category.Name);
        Assert.Equal(TestEntities.Now, category.UpdatedOnUtc);
        _categoryRepository.Verify(r => r.Update(category), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullParent_MakesCategoryRoot()
    {
        Category category = TestEntities.NewCategory(2, parentCategoryId: 1);
        SetupCategories(category);

        Result<long> result = await HandleAsync(2, parentCategoryId: null);

        Assert.True(result.IsSuccess);
        Assert.Null(category.ParentCategoryId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void AssertRejected(Result<long> result, Error expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, result.Error);
        _categoryRepository.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
