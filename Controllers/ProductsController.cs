using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using kanbanBackend.Models; 

namespace kanbanBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly LabbelMainContext _mainContext;

        public ProductsController(LabbelMainContext mainContext)
        {
            _mainContext = mainContext;
        }

        [HttpGet("binSize")]
        public async Task<IActionResult> GetBinSizes()
        {
            try
            {
                var binSizes = await _mainContext.TblBinSizes
                                   .Select(b => new { b.Id, b.StrShortName, b.StrName })
                                   .ToListAsync();
                return Ok(binSizes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("prodLine")]
        public async Task<ActionResult<List<TblArea>>> GetProductionLines()
        {
            return Ok(await _mainContext.TblAreas.ToListAsync());
        }

        [HttpPost("addMaterial")]
        public async Task<IActionResult> AddMaterial([FromForm] MaterialUploadModel model)
        {
            if (model == null)
                return BadRequest("Model is null.");

            if (string.IsNullOrWhiteSpace(model.Material) ||
                string.IsNullOrWhiteSpace(model.Description) ||
                string.IsNullOrWhiteSpace(model.ProdLoc) ||
                model.Qty <= 0 ||
                string.IsNullOrWhiteSpace(model.BinSize) ||
                string.IsNullOrWhiteSpace(model.ProductionLine))
            {
                return BadRequest("One or more required fields are missing or invalid.");
            }

            try
            {
                var materialParam = new SqlParameter("@partNumber", model.Material);
                var descriptionParam = new SqlParameter("@materialDescription", model.Description);
                var prodLocParam = new SqlParameter("@bin", model.ProdLoc);
                var qtyParam = new SqlParameter("@quantity", model.Qty);

                var binSizeParam = new SqlParameter
                {
                    ParameterName = "@binSize",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    IsNullable = true,
                    Value = string.IsNullOrWhiteSpace(model.BinSize)
                                        ? DBNull.Value
                                        : (object)model.BinSize
                };

                var prodLineParam = new SqlParameter("@productionArea", model.ProductionLine);

                await _mainContext.Database.ExecuteSqlRawAsync(
                    "exec sprInsertKanbanFromExcel @partNumber, @materialDescription, @bin, @quantity, @binSize, @productionArea",
                    materialParam, descriptionParam, prodLocParam, qtyParam, binSizeParam, prodLineParam
                );

                var materialRecord = await _mainContext.TblMaterials
                    .FirstOrDefaultAsync(m => m.StrName == model.Material);
                if (materialRecord == null)
                    return NotFound("Material not found after insertion.");

                if (model.Picture != null)
                {
                    using var ms = new MemoryStream();
                    await model.Picture.CopyToAsync(ms);
                    var imageData = ms.ToArray();

                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "materials");
                    Directory.CreateDirectory(folderPath);

                    var extension = Path.GetExtension(model.Picture.FileName)?.ToLower() ?? ".jpg";
                    var fileName = materialRecord.StrName + extension;
                    var filePath = Path.Combine(folderPath, fileName);

                    await System.IO.File.WriteAllBytesAsync(filePath, imageData);

                    materialRecord.ImagePath = "/images/materials/" + fileName;
                    _mainContext.TblMaterials.Update(materialRecord);
                    await _mainContext.SaveChangesAsync();
                }

                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadData([FromForm] FileModel file)
        {
            try
            {
                var targetDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploaded");
                Directory.CreateDirectory(targetDir);
                var path = Path.Combine(targetDir, file.fileName + ".csv");

                using (var stream = new FileStream(path, FileMode.Create))
                    await file.formFile.CopyToAsync(stream);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    IgnoreBlankLines = true,
                                   
                };

                using var reader = new StreamReader(path);
                using var csv = new CsvReader(reader, config);
                csv.Context.RegisterClassMap<ProductMap>();
                var records = csv.GetRecords<Product>().ToList();

                foreach (var record in records)
                {
                    if (string.IsNullOrWhiteSpace(record.Material))
                        continue;

                    var p1 = new SqlParameter("@partNumber", record.Material);
                    var p2 = new SqlParameter("@materialDescription", record.Description);
                    var p3 = new SqlParameter("@bin", record.ProdLoc);
                    var p4 = new SqlParameter("@quantity", record.Qty);
                    var p5 = new SqlParameter(
                        "@binSize",
                        string.IsNullOrWhiteSpace(record.BinSize)
                          ? DBNull.Value
                          : (object)record.BinSize);
                    var p6 = new SqlParameter("@productionArea", record.ProductionLine);

                    await _mainContext.Database.ExecuteSqlRawAsync(
                        "exec sprInsertKanbanFromExcel @partNumber, @materialDescription, @bin, @quantity, @binSize, @productionArea",
                        p1, p2, p3, p4, p5, p6
                    );
                }

                return Ok(new { message = "Upload and import succeeded" });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpGet("production-areas")]
        public async Task<IActionResult> GetProductionAreas()
        {
            var list = await _mainContext.VwKanbans
                .Select(k => k.StrProductionArea)
                .Where(x => x != null && x != "")
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Ok(list);
        }



        [HttpGet("by-production-area/{area}")]
        public async Task<IActionResult> GetMaterialsByProductionArea(string area)
        {
            var list = await _mainContext.VwKanbans
                .Where(k => k.StrProductionArea.ToLower() == area.ToLower())
                .Select(k => new { k.StrPartNumber, k.StrBinSize }) // <-- must match frontend
                .Distinct()
                .ToListAsync();

            return Ok(list);
        }

        

        [HttpGet("materials")]
        public async Task<IActionResult> GetMaterials()
        {
            var list = await _mainContext.TblMaterials
                             .Select(m => new { m.StrName, m.StrDescription })
                             .ToListAsync();
            return Ok(list);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMaterials(string term)
        {
            var q = term?.Trim().ToLower() ?? "";
            var list = await _mainContext.TblMaterials
                          .Where(m => m.StrName.ToLower().Contains(q))
                          .Select(m => new { m.StrName })
                          .Take(20)
                          .ToListAsync();
            return Ok(list);
        }

        [HttpGet("kanban/{partNumber}")]
        public async Task<IActionResult> GetKanbanData(string partNumber)
        {
            try
            {
                var kanbanData = await _mainContext.VwKanbans
                                      .FirstOrDefaultAsync(k => k.StrPartNumber == partNumber);

                if (kanbanData == null)
                    return NotFound("No label found for the provided part number.");

                if (string.IsNullOrEmpty(kanbanData.ImagePath))
                {
                    foreach (var ext in new[] { ".jpg", ".png", ".jpeg" })
                    {
                        var rel = "/images/materials/" + partNumber + ext;
                        var abs = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "materials", partNumber + ext);
                        if (System.IO.File.Exists(abs))
                        {
                            kanbanData.ImagePath = rel;
                            break;
                        }
                    }
                }

                return Ok(kanbanData);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
