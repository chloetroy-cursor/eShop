namespace eShop.Catalog.UnitTests;

using eShop.Catalog.API.Infrastructure.Exceptions;
using eShop.Catalog.API.Model;

[TestClass]
public class CatalogItemStockTests
{
    [TestMethod]
    public void RemoveStock_empty_stock_throws_sold_out()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 0,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(2));

        //Assert
        Assert.AreEqual("Empty stock, product item Road Bike is sold out", ex.Message);
        Assert.AreEqual(0, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void RemoveStock_empty_stock_and_zero_quantity_throws_sold_out()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 0,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(0));

        //Assert
        Assert.AreEqual("Empty stock, product item Road Bike is sold out", ex.Message);
        Assert.AreEqual(0, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void RemoveStock_empty_stock_and_negative_quantity_throws_sold_out()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 0,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(-3));

        //Assert
        Assert.AreEqual("Empty stock, product item Road Bike is sold out", ex.Message);
        Assert.AreEqual(0, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void RemoveStock_zero_quantity_throws_non_positive()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 8,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(0));

        //Assert
        Assert.AreEqual("Item units desired should be greater than zero", ex.Message);
        Assert.AreEqual(8, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void RemoveStock_negative_quantity_throws_non_positive()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 8,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(-4));

        //Assert
        Assert.AreEqual("Item units desired should be greater than zero", ex.Message);
        Assert.AreEqual(8, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void RemoveStock_partial_removes_available_and_leaves_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 3,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var removed = item.RemoveStock(10);

        //Assert
        Assert.AreEqual(3, removed);
        Assert.AreEqual(0, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void RemoveStock_exact_removes_requested_and_leaves_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 6,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var removed = item.RemoveStock(6);

        //Assert
        Assert.AreEqual(6, removed);
        Assert.AreEqual(0, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void RemoveStock_within_stock_removes_requested_and_leaves_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 9,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var removed = item.RemoveStock(4);

        //Assert
        Assert.AreEqual(4, removed);
        Assert.AreEqual(5, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_under_threshold_adds_quantity_and_clears_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 10,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var added = item.AddStock(5);

        //Assert
        Assert.AreEqual(5, added);
        Assert.AreEqual(15, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_equal_to_threshold_adds_quantity_and_clears_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 90,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var added = item.AddStock(10);

        //Assert
        Assert.AreEqual(10, added);
        Assert.AreEqual(100, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_over_threshold_caps_at_max_and_clears_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 90,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var added = item.AddStock(50);

        //Assert
        Assert.AreEqual(10, added);
        Assert.AreEqual(100, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_already_above_threshold_returns_negative_delta_and_clears_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 150,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var added = item.AddStock(1);

        //Assert
        Assert.AreEqual(-50, added);
        Assert.AreEqual(100, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_already_at_max_returns_zero_and_clears_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 100,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var added = item.AddStock(5);

        //Assert
        Assert.AreEqual(0, added);
        Assert.AreEqual(100, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_zero_quantity_returns_zero_and_clears_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 25,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var added = item.AddStock(0);

        //Assert
        Assert.AreEqual(0, added);
        Assert.AreEqual(25, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_negative_quantity_decreases_stock_and_clears_reorder()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = 25,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var added = item.AddStock(-7);

        //Assert
        Assert.AreEqual(-7, added);
        Assert.AreEqual(18, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_unchecked_overflow_wraps_to_min_value()
    {
        //Arrange
        var item = new CatalogItem("Road Bike")
        {
            AvailableStock = int.MaxValue,
            MaxStockThreshold = 100,
            OnReorder = true,
        };

        //Act
        var added = item.AddStock(1);

        //Assert
        Assert.AreEqual(1, added);
        Assert.AreEqual(int.MinValue, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }
}
