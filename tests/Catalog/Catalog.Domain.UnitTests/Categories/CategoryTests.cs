using FluentAssertions;

namespace Catalog.Domain.Categories.Tests
{
    public class CategoryTests
    {
        [Fact]
        public void Create_ShouldReturnCategory_WhenValidInputsAreProvided()
        {
            // Arrange
            var name = new CategoryName("Electronics");
            var code = new CategoryCode("ELEC");
            var createdOn = DateTime.UtcNow;

            // Act
            var category = Category.Create(name, code, createdOn);

            // Assert
            category.Should().NotBeNull();
            category.Name.Should().Be(name);
            category.Code.Should().Be(code);
            category.CreatedOn.Should().BeCloseTo(createdOn, TimeSpan.FromSeconds(1));
            category.UpdatedOn.Should().BeNull();
        }

        [Fact]
        public void Update_ShouldUpdateCategoryProperties_WhenValidInputsAreProvided()
        {
            // Arrange
            var name = new CategoryName("Electronics");
            var code = new CategoryCode("ELEC");
            var createdOn = DateTime.UtcNow;
            var category = Category.Create(name, code, createdOn);

            var updatedName = new CategoryName("Home Appliances");
            var updatedCode = new CategoryCode("HOME");
            var updatedOn = DateTime.UtcNow;

            // Act
            var updatedCategory = Category.Update(category, updatedName, updatedCode, updatedOn);

            // Assert
            updatedCategory.Should().NotBeNull();
            updatedCategory.Name.Should().Be(updatedName);
            updatedCategory.Code.Should().Be(updatedCode);
            updatedCategory.UpdatedOn.Should().BeCloseTo(updatedOn, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Update_ShouldPreserveCreatedOn_WhenUpdatingCategory()
        {
            // Arrange
            var name = new CategoryName("Electronics");
            var code = new CategoryCode("ELEC");
            var createdOn = DateTime.UtcNow;
            var category = Category.Create(name, code, createdOn);

            var updatedName = new CategoryName("Home Appliances");
            var updatedCode = new CategoryCode("HOME");
            var updatedOn = DateTime.UtcNow;

            // Act
            var updatedCategory = Category.Update(category, updatedName, updatedCode, updatedOn);

            // Assert
            updatedCategory.CreatedOn.Should().Be(category.CreatedOn);
        }
    }
}
