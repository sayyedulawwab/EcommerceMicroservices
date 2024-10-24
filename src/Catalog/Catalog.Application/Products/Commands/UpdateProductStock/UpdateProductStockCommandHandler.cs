﻿using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Abstractions;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.Domain.Shared;

namespace Catalog.Application.Products.Commands.EditProduct;
internal sealed class UpdateProductStockCommandHandler : ICommandHandler<UpdateProductStockCommand, long>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateProductStockCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<long>> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.id);

        if (product is null)
        {
            return Result.Failure<long>(ProductErrors.NotFound());
        }

        if (product.Quantity == 0)
        {
            return Result.Failure<long>(ProductErrors.SoldOut(request.id));
        }

        product.RemoveStock(request.quantity);

        _productRepository.Update(product);

        await _unitOfWork.SaveChangesAsync();

        return product.Id;

    }
}
