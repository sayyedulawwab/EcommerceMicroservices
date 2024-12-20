﻿using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.EditProduct;
public record EditProductCommand(long id, string name, string description, string priceCurrency, decimal priceAmount, int quantity, long categoryId) : ICommand<long>;