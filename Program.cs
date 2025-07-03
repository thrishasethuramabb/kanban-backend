global using kanbanBackend.Models;
global using Microsoft.EntityFrameworkCore;
using ImageMagick;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using System.Text;
       
using System.IO;           

var builder = WebApplication.CreateBuilder(args);

// MVC + Swagger + EF + Auth + CORS…
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<LabbelMainContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("KanbanConnectionString")));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                                          Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var _policyName = "GeeksPolicy";
builder.Services.AddCors(o => o.AddPolicy(_policyName, p =>
  p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
));

var app = builder.Build();

// ─── ADD THIS STARTUP HEIC→JPEG SWEEP ────────────────────────────────────
var webRoot = app.Environment.WebRootPath;
var materialsDir = Path.Combine(webRoot, "images", "materials");
if (Directory.Exists(materialsDir))
{
    foreach (var heic in Directory.EnumerateFiles(materialsDir, "*.heic"))
    {
        var jpg = Path.ChangeExtension(heic, ".jpg");
        if (File.Exists(jpg))
            continue;

        try
        {
            using var img = new MagickImage(heic);
            img.Format = MagickFormat.Jpeg;
            img.Write(jpg);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to convert {heic}: {ex.Message}");
        }
    }
}


// Swagger in dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Static‐file mappings (including HEIC)
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".heic"] = "image/heic";
provider.Mappings[".heif"] = "image/heif";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    }
});

app.UseRouting();
app.UseCors(_policyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
