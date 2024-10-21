﻿namespace Catalog.API.Controllers.Products;

public record EditProductRequest(string name, string description, string priceCurrency, decimal priceAmount,
    int quantity, long categoryId);