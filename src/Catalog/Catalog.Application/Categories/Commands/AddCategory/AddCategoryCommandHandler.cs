using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Abstractions;
using Catalog.Domain.Categories;

namespace Catalog.Application.Categories.Commands.AddCategory;
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
        var category = Category.Create(new CategoryName(request.name), new CategoryCode(request.code), _dateTimeProvider.UtcNow);

        _categoryRepository.Add(category);

        await _unitOfWork.SaveChangesAsync();

        return category.Id;

    }
}
