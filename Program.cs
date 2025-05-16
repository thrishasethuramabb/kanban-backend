global using kanbanBackend.Models;
global using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1) Add MVC controllers
builder.Services.AddControllers();

// 2) Swagger (dev only)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3) EF Core
builder.Services.AddDbContext<LabbelMainContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("KanbanConnectionString")));

// 4) JWT Auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
                                      Encoding.UTF8.GetBytes(
                                        builder.Configuration["Jwt:Key"]))
    };
});

// 5) CORS
var _policyName = "GeeksPolicy";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(name: _policyName, policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 6) Dev-only Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 7) Global CORS
app.UseCors(_policyName);

// 8) Make sure static files (wwwroot) are served
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    }
});

// 9) Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 10) Map your API controllers (incl. LabelConfigController)
app.MapControllers();

// 11) (Optional but recommended for Angular client-side routes)
//     Any unknown path (e.g. /theme-editor) returns index.html so Angular can handle it:
app.MapFallbackToFile("index.html");

app.Run();
