using eShop.Catalog.API.Infrastructure.Exceptions;
using eShop.Catalog.API.Model;

namespace eShop.Catalog.UnitTests;

[TestClass]
public class CatalogItemStockTests
{
    private static CatalogItem Item(int available, int max = 100, bool onReorder = false) =>
        new("Widget") { AvailableStock = available, MaxStockThreshold = max, OnReorder = onReorder };

    [TestMethod]
    [DataRow(1)]
    [DataRow(0)]
    [DataRow(-1)]
    public void RemoveStock_EmptyStock_ThrowsSoldOut(int desired)
    {
        var item = Item(available: 0);

        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(desired));

        Assert.AreEqual("Empty stock, product item Widget is sold out", ex.Message);
        Assert.AreEqual(0, item.AvailableStock);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void RemoveStock_NonPositiveQuantity_Throws(int desired)
    {
        var item = Item(available: 5);

        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(desired));

        Assert.AreEqual("Item units desired should be greater than zero", ex.Message);
        Assert.AreEqual(5, item.AvailableStock);
    }

    [TestMethod]
    [DataRow(10, 1, 1, 9)]
    [DataRow(10, 10, 10, 0)]
    [DataRow(10, 4, 4, 6)]
    public void RemoveStock_SufficientStock_RemovesDesired(int available, int desired, int expectedRemoved, int expectedAvailable)
    {
        var item = Item(available);

        var removed = item.RemoveStock(desired);

        Assert.AreEqual(expectedRemoved, removed);
        Assert.AreEqual(expectedAvailable, item.AvailableStock);
    }

    [TestMethod]
    [DataRow(3, 4)]
    [DataRow(3, int.MaxValue)]
    public void RemoveStock_InsufficientStock_RemovesAvailableAndZeroes(int available, int desired)
    {
        var item = Item(available);

        var removed = item.RemoveStock(desired);

        Assert.AreEqual(available, removed);
        Assert.AreEqual(0, item.AvailableStock);
    }

    [TestMethod]
    [DataRow(0, 100, 10, 10, 10)]
    [DataRow(5, 100, 95, 95, 100)]
    [DataRow(5, 100, 0, 0, 5)]
    public void AddStock_UnderMax_AddsQuantityAndClearsOnReorder(int available, int max, int quantity, int expectedAdded, int expectedAvailable)
    {
        var item = Item(available, max, onReorder: true);

        var added = item.AddStock(quantity);

        Assert.AreEqual(expectedAdded, added);
        Assert.AreEqual(expectedAvailable, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    [DataRow(90, 100, 20, 10)]
    [DataRow(100, 100, 1, 0)]
    [DataRow(0, 100, int.MaxValue, 100)]
    public void AddStock_ExceedsMax_ClampsToMaxThreshold(int available, int max, int quantity, int expectedAdded)
    {
        var item = Item(available, max, onReorder: true);

        var added = item.AddStock(quantity);

        Assert.AreEqual(expectedAdded, added);
        Assert.AreEqual(max, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }
}
