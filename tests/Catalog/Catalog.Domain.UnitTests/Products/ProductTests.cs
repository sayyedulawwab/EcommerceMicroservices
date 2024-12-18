using Catalog.Domain.Products;
using FluentAssertions;
using SharedKernel.Domain;

namespace Catalog.Domain.UnitTests.Products
{
    public class ProductTests
    {
        [Fact]
        public void Create_ShouldReturnProduct_WhenValidInputsAreProvided()
        {
            // Arrange
            var name = "Laptop";
            var description = "High-performance laptop";
            var currency = Currency.Create("USD");
            var price = new Money(1200.99m, currency);
            var quantity = 10;
            var categoryId = 1L;
            var createdOn = DateTime.UtcNow;

            // Act
            var product = Product.Create(name, description, price, quantity, categoryId, createdOn);

            // Assert
            product.Should().NotBeNull();
            product.Name.Should().Be(name);
            product.Description.Should().Be(description);
            product.Price.Should().Be(price);
            product.Quantity.Should().Be(quantity);
            product.CategoryId.Should().Be(categoryId);
            product.CreatedOn.Should().BeCloseTo(createdOn, TimeSpan.FromSeconds(1));
            product.UpdatedOn.Should().BeNull();
        }

        [Fact]
        public void Update_ShouldUpdateProductProperties_WhenValidInputsAreProvided()
        {
            // Arrange
            var name = "Laptop";
            var description = "High-performance laptop";
            var currency = Currency.Create("USD");
            var price = new Money(1200.99m, currency);
            var quantity = 10;
            var categoryId = 1L;
            var createdOn = DateTime.UtcNow;
            var product = Product.Create(name, description, price, quantity, categoryId, createdOn);

            var updatedName = "Gaming Laptop";
            var updatedDescription = "High-end gaming laptop";
            var updatedCurrency = Currency.Create("USD");
            var updatedPrice = new Money(1500.00m, updatedCurrency);
            var updatedQuantity = 5;
            var updatedCategoryId = 2L;
            var updatedOn = DateTime.UtcNow;

            // Act
            var updatedProduct = Product.Update(product, updatedName, updatedDescription, updatedPrice, updatedQuantity, updatedCategoryId, updatedOn);

            // Assert
            updatedProduct.Should().NotBeNull();
            updatedProduct.Name.Should().Be(updatedName);
            updatedProduct.Description.Should().Be(updatedDescription);
            updatedProduct.Price.Should().Be(updatedPrice);
            updatedProduct.Quantity.Should().Be(updatedQuantity);
            updatedProduct.CategoryId.Should().Be(updatedCategoryId);
            updatedProduct.UpdatedOn.Should().BeCloseTo(updatedOn, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Update_ShouldPreserveCreatedOn_WhenUpdatingProduct()
        {
            // Arrange
            var name = "Laptop";
            var description = "High-performance laptop";
            var currency = Currency.Create("USD");
            var price = new Money(1200.99m, currency);
            var quantity = 10;
            var categoryId = 1L;
            var createdOn = DateTime.UtcNow;
            var product = Product.Create(name, description, price, quantity, categoryId, createdOn);

            var updatedName = "Gaming Laptop";
            var updatedDescription = "High-end gaming laptop";
            var updatedCurrency = Currency.Create("BDT");
            var updatedPrice = new Money(1500.00m, currency);
            var updatedQuantity = 5;
            var updatedCategoryId = 2L;
            var updatedOn = DateTime.UtcNow;

            // Act
            var updatedProduct = Product.Update(product, updatedName, updatedDescription, updatedPrice, updatedQuantity, updatedCategoryId, updatedOn);

            // Assert
            updatedProduct.CreatedOn.Should().Be(product.CreatedOn);
        }

        [Fact]
        public void RemoveStock_ShouldReduceQuantity_WhenQuantityIsAvailable()
        {
            // Arrange
            var name = "Laptop";
            var description = "High-performance laptop";
            var currency = Currency.Create("USD");
            var price = new Money(1200.99m, currency);
            var quantity = 10;
            var categoryId = 1L;
            var createdOn = DateTime.UtcNow;
            var product = Product.Create(name, description, price, quantity, categoryId, createdOn);

            var quantityToRemove = 3;

            // Act
            product.RemoveStock(quantityToRemove);

            // Assert
            product.Quantity.Should().Be(7);
        }

        [Fact]
        public void RemoveStock_ShouldNotReduceQuantityBelowZero_WhenQuantityIsInsufficient()
        {
            // Arrange
            var name = "Laptop";
            var description = "High-performance laptop";
            var currency = Currency.Create("USD");
            var price = new Money(1200.99m, currency);
            var quantity = 2;
            var categoryId = 1L;
            var createdOn = DateTime.UtcNow;
            var product = Product.Create(name, description, price, quantity, categoryId, createdOn);

            var quantityToRemove = 5;

            // Act
            product.RemoveStock(quantityToRemove);

            // Assert
            product.Quantity.Should().Be(0);
        }
    }
}
