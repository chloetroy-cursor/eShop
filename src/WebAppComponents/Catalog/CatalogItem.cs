namespace eShop.WebAppComponents.Catalog;

public record CatalogItem(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string PictureUrl,
    int CatalogBrandId,
    // The paged list endpoint leaves both navigation properties null; only items/{id} fills them.
    CatalogBrand? CatalogBrand,
    int CatalogTypeId,
    CatalogItemType? CatalogType,
    int AvailableStock,
    int RestockThreshold,
    int MaxStockThreshold,
    bool OnReorder);

public record CatalogResult(int PageIndex, int PageSize, int Count, List<CatalogItem> Data);
public record CatalogBrand(int Id, string Brand);
public record CatalogItemType(int Id, string Type);
