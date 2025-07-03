using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using kanbanBackend.Models;
using ImageMagick;


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
                var stationParam = new SqlParameter
                {
                    ParameterName = "@stationCode",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    IsNullable = true,
                    Value = string.IsNullOrWhiteSpace(model.StationCode)
                  ? DBNull.Value
                  : (object)model.StationCode
                };

                await _mainContext.Database.ExecuteSqlRawAsync(
                   "exec sprInsertKanbanFromExcel @partNumber, @materialDescription, @bin, @quantity, @binSize, @productionArea, @stationCode",
                   materialParam, descriptionParam, prodLocParam, qtyParam, binSizeParam, prodLineParam, stationParam
               );




                var materialRecord = await _mainContext.TblMaterials
                    .FirstOrDefaultAsync(m => m.StrName == model.Material);
                if (materialRecord == null)
                    return NotFound("Material not found after insertion.");

                if (model.Picture != null)
                {
                    // Read the uploaded file into memory
                    using var ms = new MemoryStream();
                    await model.Picture.CopyToAsync(ms);
                    var imageData = ms.ToArray();

                    // Where we’ll save the images
                    string folderPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot", "images", "materials"
                    );
                    Directory.CreateDirectory(folderPath);

                    // Normalize extension & build base filename
                    var ext = Path.GetExtension(model.Picture.FileName)?.ToLower() ?? "";
                    var baseName = materialRecord.StrName;
                    string savedFileName;

                    if (ext == ".heic" || ext == ".heif")
                    {
                        // Convert HEIC/HEIF → JPEG
                        using var img = new MagickImage(imageData);
                        img.Format = MagickFormat.Jpeg;

                        savedFileName = baseName + ".jpg";
                        var outPath = Path.Combine(folderPath, savedFileName);
                        img.Write(outPath);
                    }
                    else
                    {
                        // Just save the original file (e.g. .jpg, .png, etc.)
                        savedFileName = baseName + ext;
                        var outPath = Path.Combine(folderPath, savedFileName);
                        await System.IO.File.WriteAllBytesAsync(outPath, imageData);
                    }

                    // Point your label-generator to the newly saved JPEG/PNG
                    materialRecord.ImagePath = "/images/materials/" + savedFileName;
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
                // 1) Save the incoming CSV to wwwroot/uploaded
                var targetDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploaded");
                Directory.CreateDirectory(targetDir);
                var path = Path.Combine(targetDir, file.fileName + ".csv");
                using (var stream = new FileStream(path, FileMode.Create))
                    await file.formFile.CopyToAsync(stream);

                // 2) Configure CsvReader
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    IgnoreBlankLines = true,
                };

                // 3) Parse records
                using var reader = new StreamReader(path);
                using var csv = new CsvReader(reader, config);
                csv.Context.RegisterClassMap<ProductMap>();
                var records = csv.GetRecords<Product>().ToList();

                // 4) Loop & call stored proc for each row
                foreach (var record in records)
                {
                    if (string.IsNullOrWhiteSpace(record.Material))
                        continue;

                    // ─── required fields ───────────────────────────────────────
                    var p1 = new SqlParameter("@partNumber", record.Material);
                    var p2 = new SqlParameter("@materialDescription", record.Description);
                    var p3 = new SqlParameter("@bin", record.ProdLoc);
                    var p4 = new SqlParameter("@quantity", record.Qty);
                    var p5 = new SqlParameter("@binSize",
                                  string.IsNullOrWhiteSpace(record.BinSize)
                                    ? (object)DBNull.Value
                                    : record.BinSize);
                    var p6 = new SqlParameter("@productionArea", record.ProductionLine);
                    var p7 = new SqlParameter
                    {
                        ParameterName = "@stationCode",
                        SqlDbType = System.Data.SqlDbType.NVarChar,
                        Size = 50,
                        IsNullable = true,
                        Value = string.IsNullOrWhiteSpace(record.StationCode)
                                           ? (object)DBNull.Value
                                           : record.StationCode
                    };

                    // ─── NEW: externalKanbanId ──────────────────────────────
                    var p8 = new SqlParameter
                    {
                        ParameterName = "@externalKanbanId",
                        SqlDbType = System.Data.SqlDbType.Int,
                        IsNullable = true,
                        Value = record.ExternalKanbanId.HasValue
                                           ? (object)record.ExternalKanbanId.Value
                                           : DBNull.Value
                    };

                    // 5) Execute the proc with ALL 8 params
                    await _mainContext.Database.ExecuteSqlRawAsync(
                       "EXEC dbo.sprInsertKanbanFromExcel " +
                       "@partNumber, @materialDescription, @bin, @quantity, @binSize, " +
                       "@productionArea, @stationCode, @externalKanbanId",
                       p1, p2, p3, p4, p5, p6, p7, p8
                    );
                }

                return Ok(new { message = "Upload and import succeeded" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }





        [HttpPost("uploadImages")]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> files)
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "materials");
            Directory.CreateDirectory(folder);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName).ToLower();
                var partNumber = Path.GetFileNameWithoutExtension(file.FileName);
                var targetName = partNumber + (ext == ".heic" || ext == ".heif" ? ".jpg" : ext);
                var outPath = Path.Combine(folder, targetName);

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var data = ms.ToArray();

                if (ext == ".heic" || ext == ".heif")
                {
                    using var img = new MagickImage(data);
                    img.Format = MagickFormat.Jpeg;
                    img.Write(outPath);
                }
                else
                {
                    await System.IO.File.WriteAllBytesAsync(outPath, data);
                }

                var mat = await _mainContext.TblMaterials
                              .FirstOrDefaultAsync(m => m.StrName == partNumber);
                if (mat != null)
                {
                    mat.ImagePath = "/images/materials/" + targetName;
                    _mainContext.Update(mat);
                }
            }

            await _mainContext.SaveChangesAsync();
            return Ok();
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



        [HttpGet("kanban-full-empty")]
        public async Task<IActionResult> GetKanbanFullEmpty()
        {
            try
            {
                var list = await _mainContext.TblKanbans
                    .Include(k => k.Material)   // Correct navigation property
                    .Include(k => k.Area)       // Correct navigation property
                    .Select(k => new {
                        id = k.Id,
                        partNumber = k.Material.StrName,
                        supplyArea = k.Area.StrName,
                        externalKanbanId = k.ExternalKanbanId
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }




        [HttpGet("kanban/{partNumber}")]
        public async Task<IActionResult> GetKanbanData(string partNumber)
        {
            try
            {
                var kanbanDataList = await _mainContext.VwKanbans
                    .Where(k => k.StrPartNumber == partNumber)
                    .ToListAsync();

                if (!kanbanDataList.Any())
                    return NotFound("No labels found for the provided part number.");

                // Try to attach image paths
                foreach (var k in kanbanDataList)
                {
                    if (string.IsNullOrEmpty(k.ImagePath))
                    {
                        foreach (var ext in new[] { ".jpg", ".png", ".jpeg" })
                        {
                            var rel = "/images/materials/" + k.StrPartNumber + ext;
                            var abs = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "materials", k.StrPartNumber + ext);
                            if (System.IO.File.Exists(abs))
                            {
                                k.ImagePath = rel;
                                break;
                            }
                        }
                    }
                }

                return Ok(kanbanDataList);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
