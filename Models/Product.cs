namespace kanbanBackend.Models
{
    [Keyless]
    public class Product
    {
        public string Material { get; set; }
        public string Description { get; set; }
        public string ProdLoc { get; set; }
        public string BinSize { get; set; }
        public int Qty { get; set; }
        public string ProductionLine { get; set; }
    }

}
