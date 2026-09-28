using System.Runtime.InteropServices;

namespace eShop.Catalog.API.Model;

internal static partial class NativeStock
{
    private const string LibraryName = "catalog";

    public const int Ok = 0;
    public const int EmptyStock = 1;
    public const int NonPositiveQuantity = 2;

    public static string LibraryFileName =>
        OperatingSystem.IsWindows() ? "catalog.dll"
        : OperatingSystem.IsMacOS() ? "libcatalog.dylib"
        : "libcatalog.so";

    static NativeStock()
    {
        if (!NativeLibrary.TryLoad(LibraryName, typeof(NativeStock).Assembly, null, out _))
        {
            throw new DllNotFoundException(
                $"Rust library '{LibraryName}' was not found. Expected {Path.Combine(AppContext.BaseDirectory, LibraryFileName)}. " +
                "Catalog.API.csproj builds it with `cargo build --release --manifest-path native/Cargo.toml -p catalog` " +
                "when cargo is installed (PATH or ~/.cargo/bin); rebuild after installing Rust.");
        }
    }

    [LibraryImport(LibraryName, EntryPoint = "catalog_remove_stock")]
    public static partial int RemoveStock(int available, int desired, out int newAvailable, out int removed);

    [LibraryImport(LibraryName, EntryPoint = "catalog_add_stock")]
    public static partial int AddStock(int available, int max, int quantity, out int newAvailable);
}
