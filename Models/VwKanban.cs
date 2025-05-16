using System;
using System.Collections.Generic;

namespace kanbanBackend.Models
{
    public partial class VwKanban
    {
        public string? StrPartNumber { get; set; }
        public string? StrMaterialDescription { get; set; }
        public string? StrBin { get; set; }
        public int? IntQuantity { get; set; }
        public string? StrBinSize { get; set; }
        public string? StrProductionArea { get; set; }
        public string? ImagePath { get; set; }    // Add this property so EF maps the image path column from the view
    }
}
