using System.IO;
using System.Runtime.InteropServices;
using eShop.Catalog.API.Model;

namespace eShop.Catalog.UnitTests;

[TestClass]
public class NativeStockTests
{
    [TestMethod]
    public void RustLibraryIsCopiedNextToTestAssemblyAndLoads()
    {
        var expected = Path.Combine(AppContext.BaseDirectory, NativeStock.LibraryFileName);

        Assert.IsTrue(File.Exists(expected), $"missing {expected}");
        Assert.IsTrue(NativeLibrary.TryLoad("catalog", typeof(CatalogItem).Assembly, null, out var handle));
        Assert.AreNotEqual(IntPtr.Zero, handle);
    }

    [TestMethod]
    public void RustExportsReturnTheStatusCodesCatalogItemMapsToExceptions()
    {
        Assert.AreEqual(NativeStock.EmptyStock, NativeStock.RemoveStock(0, 1, out _, out _));
        Assert.AreEqual(NativeStock.NonPositiveQuantity, NativeStock.RemoveStock(5, 0, out _, out _));

        Assert.AreEqual(NativeStock.Ok, NativeStock.RemoveStock(10, 4, out var newAvailable, out var removed));
        Assert.AreEqual(6, newAvailable);
        Assert.AreEqual(4, removed);

        Assert.AreEqual(10, NativeStock.AddStock(90, 100, 20, out newAvailable));
        Assert.AreEqual(100, newAvailable);
    }
}
