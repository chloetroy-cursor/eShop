using System.Runtime.InteropServices;

namespace eShop.Catalog.API.Model;

internal static partial class NativeCatalog
{
    internal const int EmptyStock = 1;
    internal const int NonPositiveQuantity = 2;

    static NativeCatalog()
    {
        NativeLibrary.Load("catalog", typeof(NativeCatalog).Assembly, DllImportSearchPath.AssemblyDirectory);
    }

    [LibraryImport("catalog", EntryPoint = "catalog_remove_stock")]
    internal static partial NativeStockResult RemoveStock(int availableStock, int quantityDesired);

    [LibraryImport("catalog", EntryPoint = "catalog_add_stock")]
    internal static partial NativeStockResult AddStock(int availableStock, int quantity, int maxStockThreshold);
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeStockResult
{
    internal int Code;
    internal int AvailableStock;
    internal int AppliedDelta;
}
