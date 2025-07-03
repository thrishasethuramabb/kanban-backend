using System.ComponentModel.DataAnnotations;

namespace kanbanBackend.Models
{
    public class MaterialUploadModel
    {
        [Required]
        public string Material { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string ProdLoc { get; set; }

        [Required]
        public int Qty { get; set; }

        [Required]
        public string BinSize { get; set; }

        [Required]
        public string ProductionLine { get; set; }

        public string? StationCode { get; set; }

        // Make this optional:
        public IFormFile? Picture { get; set; }

        public int? ExternalKanbanId { get; set; }
    }
}
