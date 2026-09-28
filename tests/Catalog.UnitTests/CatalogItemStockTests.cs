using eShop.Catalog.API.Infrastructure.Exceptions;
using eShop.Catalog.API.Model;

namespace eShop.Catalog.UnitTests;

[TestClass]
public class CatalogItemStockTests
{
    private static CatalogItem Item(
        int available,
        int max = 100,
        bool onReorder = false,
        string name = "Widget") =>
        new(name)
        {
            AvailableStock = available,
            MaxStockThreshold = max,
            OnReorder = onReorder
        };

    [TestMethod]
    [DataRow(1)]
    [DataRow(0)]
    [DataRow(-1)]
    public void RemoveStock_EmptyStock_ThrowsSoldOutBeforeQuantityCheck(int desired)
    {
        var item = Item(available: 0, onReorder: true);

        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(desired));

        Assert.AreEqual("Empty stock, product item Widget is sold out", ex.Message);
        Assert.AreEqual(0, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void RemoveStock_EmptyStock_MessageIncludesItemName()
    {
        var item = Item(available: 0, name: "Alpine Skis");

        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(0));

        Assert.AreEqual("Empty stock, product item Alpine Skis is sold out", ex.Message);
        Assert.AreEqual(0, item.AvailableStock);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void RemoveStock_NonPositiveQuantity_ThrowsAndLeavesStockUnchanged(int desired)
    {
        var item = Item(available: 5, onReorder: true);

        var ex = Assert.ThrowsExactly<CatalogDomainException>(() => item.RemoveStock(desired));

        Assert.AreEqual("Item units desired should be greater than zero", ex.Message);
        Assert.AreEqual(5, item.AvailableStock);
        Assert.IsTrue(item.OnReorder);
    }

    [TestMethod]
    public void RemoveStock_NegativeStock_IsNotEmpty_RemovesAvailableAndLeavesZero()
    {
        var item = Item(available: -5);

        var removed = item.RemoveStock(3);

        Assert.AreEqual(-5, removed);
        Assert.AreEqual(0, item.AvailableStock);
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
    public void RemoveStock_PartialRemove_RemovesAvailableAndZeroes(int available, int desired)
    {
        var item = Item(available);

        var removed = item.RemoveStock(desired);

        Assert.AreEqual(available, removed);
        Assert.AreEqual(0, item.AvailableStock);
    }

    [TestMethod]
    public void RemoveStock_MinValueAvailable_WrapsSubtractToZero()
    {
        var item = Item(available: int.MinValue);

        var removed = item.RemoveStock(1);

        Assert.AreEqual(int.MinValue, removed);
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
    public void AddStock_NegativeQuantity_SubtractsAndClearsOnReorder()
    {
        var item = Item(available: 10, max: 100, onReorder: true);

        var added = item.AddStock(-3);

        Assert.AreEqual(-3, added);
        Assert.AreEqual(7, item.AvailableStock);
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

    [TestMethod]
    [DataRow(150, 100, 1, -50, 100)]
    [DataRow(150, 100, -10, -50, 100)]
    [DataRow(150, 100, -60, -60, 90)]
    public void AddStock_AlreadyAboveMax_ClampsOnlyWhenWrappedSumExceedsMax(
        int available, int max, int quantity, int expectedAdded, int expectedAvailable)
    {
        var item = Item(available, max, onReorder: true);

        var added = item.AddStock(quantity);

        Assert.AreEqual(expectedAdded, added);
        Assert.AreEqual(expectedAvailable, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_MaxValuePlusOne_WrapsInsteadOfClamping()
    {
        var item = Item(available: int.MaxValue, max: 100, onReorder: true);

        var added = item.AddStock(1);

        Assert.AreEqual(1, added);
        Assert.AreEqual(int.MinValue, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_MinValueMinusOne_WrapsThenClampsToMax()
    {
        var item = Item(available: int.MinValue, max: 100, onReorder: true);

        var added = item.AddStock(-1);

        Assert.AreEqual(unchecked(100 - int.MinValue), added);
        Assert.AreEqual(100, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }

    [TestMethod]
    public void AddStock_ClampFromMaxValue_ReturnValueWraps()
    {
        var item = Item(available: int.MaxValue, max: 100, onReorder: true);

        var added = item.AddStock(0);

        Assert.AreEqual(unchecked(100 - int.MaxValue), added);
        Assert.AreEqual(100, item.AvailableStock);
        Assert.IsFalse(item.OnReorder);
    }
}
