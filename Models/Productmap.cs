using CsvHelper.Configuration;

public sealed class ProductMap : ClassMap<Product>
{
    public ProductMap()
    {
        Map(m => m.Material).Name("Material");
        Map(m => m.Description).Name("Description");
        Map(m => m.ProdLoc).Name("ProdLoc");
        Map(m => m.BinSize).Name("BinSize");
        Map(m => m.Qty).Name("Qty")
                                    // treat blank as 0
                                    .Default(0)
                                    // and/or allow "" as null
                                    .TypeConverterOption.NullValues(string.Empty);
        Map(m => m.ProductionLine).Name("ProductionLine");
    }
}
