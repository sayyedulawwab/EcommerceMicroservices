﻿using FluentValidation;

namespace Catalog.Application.Categories.AddCategory;
public class AddCategoryCommandValidator : AbstractValidator<AddCategoryCommand>
{
    public AddCategoryCommandValidator()
    {
        RuleFor(c => c.name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(c => c.parentCategoryId)
            .NotNull()
            .GreaterThanOrEqualTo(0);
    }
}