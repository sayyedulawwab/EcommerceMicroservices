using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Abstractions;
using Catalog.Domain.Categories;

namespace Catalog.Application.Categories.Commands.EditCategory;
internal sealed class EditCategoryCommandHandler : ICommandHandler<EditCategoryCommand, long>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EditCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<long>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        var productCategory = await _categoryRepository.GetByIdAsync(request.id);

        if (productCategory is null)
        {
            return Result.Failure<long>(CategoryErrors.NotFound(request.id));
        }

        productCategory = Category.Update(productCategory, new CategoryName(request.name), new CategoryCode(request.code), _dateTimeProvider.UtcNow);

        _categoryRepository.Update(productCategory);

        await _unitOfWork.SaveChangesAsync();

        return productCategory.Id;

    }
}
