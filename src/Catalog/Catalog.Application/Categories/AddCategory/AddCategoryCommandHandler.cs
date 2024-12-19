using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Categories;
using SharedKernel.Domain;

namespace Catalog.Application.Categories.AddCategory;
internal sealed class AddCategoryCommandHandler : ICommandHandler<AddCategoryCommand, long>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<long>> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.name, request.description, request.parentCategoryId, _dateTimeProvider.UtcNow);

        _categoryRepository.Add(category);

        await _unitOfWork.SaveChangesAsync();

        return category.Id;

    }
}