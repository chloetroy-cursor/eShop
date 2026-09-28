using System.Diagnostics;
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

        if (OperatingSystem.IsWindows())
        {
            Assert.AreEqual("catalog.dll", NativeStock.LibraryFileName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            Assert.AreEqual("libcatalog.dylib", NativeStock.LibraryFileName);
        }
        else
        {
            Assert.AreEqual("libcatalog.so", NativeStock.LibraryFileName);
        }
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

    [TestMethod]
    public void MissingNativeLibrary_NewProcessFailsWithDllNotFoundException()
    {
        var source = AppContext.BaseDirectory;
        var dest = Path.Combine(Path.GetTempPath(), "catalog-missing-lib-" + Guid.NewGuid().ToString("N"));
        CopyDirectoryExcept(source, dest, NativeStock.LibraryFileName);

        var dll = Path.Combine(dest, "Catalog.UnitTests.dll");
        var start = new ProcessStartInfo("dotnet")
        {
            Arguments = $"exec \"{dll}\" --filter FullyQualifiedName~RemoveStock_EmptyStock_MessageIncludesItemName",
            WorkingDirectory = dest,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(start);
        Assert.IsNotNull(process);
        Assert.IsTrue(process.WaitForExit(120_000), "missing-library host timed out");
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();

        Assert.AreNotEqual(0, process.ExitCode, output);
        Assert.Contains("TypeInitializationException", output);
    }

    private static void CopyDirectoryExcept(string source, string dest, string skipFileName)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
        {
            var name = Path.GetFileName(file);
            if (name.Equals(skipFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.Copy(file, Path.Combine(dest, name));
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectoryExcept(dir, Path.Combine(dest, Path.GetFileName(dir)), skipFileName);
        }
    }
}
