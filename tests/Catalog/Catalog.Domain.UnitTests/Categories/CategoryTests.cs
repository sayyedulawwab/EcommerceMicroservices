using Catalog.Domain.Categories;
using FluentAssertions;

namespace Catalog.Domain.UnitTests.Categories
{
    public class CategoryTests
    {
        [Fact]
        public void Create_ShouldReturnCategory_WhenValidInputsAreProvided()
        {
            // Arrange
            var name = "Electronics";
            var description = "ELEC";
            var parentCategoryId = 0;
            var createdOn = DateTime.UtcNow;

            // Act
            var category = Category.Create(name, description, parentCategoryId, createdOn);

            // Assert
            category.Should().NotBeNull();
            category.Name.Should().Be(name);
            category.Description.Should().Be(description);
            category.CreatedOn.Should().BeCloseTo(createdOn, TimeSpan.FromSeconds(1));
            category.UpdatedOn.Should().BeNull();
        }

        [Fact]
        public void Update_ShouldUpdateCategoryProperties_WhenValidInputsAreProvided()
        {
            // Arrange
            var name = "Electronics";
            var description = "ELEC";
            var parentCategoryId = 0;
            var createdOn = DateTime.UtcNow;
            var category = Category.Create(name, description, parentCategoryId, createdOn);

            var updatedName = "Home Appliances";
            var updatedDescription = "HOME";
            var updateParentCategoryId = 1;
            var updatedOn = DateTime.UtcNow;

            // Act
            var updatedCategory = Category.Update(category, updatedName, updatedDescription, updateParentCategoryId, updatedOn);

            // Assert
            updatedCategory.Should().NotBeNull();
            updatedCategory.Name.Should().Be(updatedName);
            updatedCategory.Description.Should().Be(updatedDescription);
            updatedCategory.UpdatedOn.Should().BeCloseTo(updatedOn, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Update_ShouldPreserveCreatedOn_WhenUpdatingCategory()
        {
            // Arrange
            var name = "Electronics";
            var description = "ELEC";
            var parentCategoryId = 0;
            var createdOn = DateTime.UtcNow;
            var category = Category.Create(name, description, parentCategoryId, createdOn);

            var updatedName = "Home Appliances";
            var updatedDescription = "HOME";
            var updateParentCategoryId = 1;
            var updatedOn = DateTime.UtcNow;

            // Act
            var updatedCategory = Category.Update(category, updatedName, updatedDescription, updateParentCategoryId, updatedOn);

            // Assert
            updatedCategory.CreatedOn.Should().Be(category.CreatedOn);
        }
    }
}
