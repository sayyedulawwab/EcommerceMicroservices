﻿using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.UpdateProductStock;
public record UpdateProductStockCommand(long id, int quantity) : ICommand<long>;