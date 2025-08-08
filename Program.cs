using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NuGet.Common;
using System.Net;
using System.Reflection;
using System.Text;
using WEBAPI_m1IL_1.Models;
using WEBAPI_m1IL_1.Services;
using WEBAPI_m1IL_1.Controllers;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.AspNetCore.Diagnostics;
using WEBAPI_m1IL_1.Config;
using WEBAPI_m1IL_1.Utils;

var configCompose = new ConfigCompose();
configCompose.SetupAndRun();
var builder = WebApplication.CreateBuilder(args);
var postgres = $"Host=localHost;Port={builder.Configuration["Postgres:Port"]};Database={builder.Configuration["Postgres:Database"]};Username={builder.Configuration["Postgres:PostgresUser"]};Password={builder.Configuration["Postgres:PostgresPassword"]};SSL Mode=Disable;Timeout=15";

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 524288000; // 500 MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);

});
builder.Services.AddSingleton<ConfigCompose>();
// Add services to the container.  
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle  
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DocumentationDbContext>(options =>
    options.UseNpgsql(postgres));

builder.Services.AddMvc();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "V1",
        Title = "documentation API",
        Description = "Mon API de documentation permettant de tranformer un documentation en markdown et de la distribuer",
        TermsOfService = new Uri("https://google.com"),
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "axel teyssier",
            Email = "",
            Url = new Uri("https://google.com")
        }

    });

    // Ajout de la lecture des commentaires XML  
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: Authorization: Bearer { token }"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
 {
     {
         new OpenApiSecurityScheme
         {
             Reference = new OpenApiReference
             {
                 Type = ReferenceType.SecurityScheme,
                 Id = "Bearer"
             }
         },
         new string[] {}
     }
 });
});

builder.Services.AddSingleton<LuceneSearchService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<AIService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();
app.UseCors("AllowAll");
// Configure the HTTP request pipeline.  
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var client = new HttpClient();

    try
    {
        // Attendre un peu que le serveur soit prêt (utile si Swagger n’est pas encore monté)
        await Task.Delay(2000);

        var url = "https://localhost:7234/swagger/v1/swagger.yaml";
        var response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();

        var yaml = await response.Content.ReadAsStringAsync();
        var OpenApiPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "OpenApi"));

        var path = Path.Combine(OpenApiPath, "flat.yaml");

        await File.WriteAllTextAsync(path, yaml);

        Console.WriteLine($"✅ Swagger.yaml généré à : {path}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erreur lors de la génération de swagger.yaml : {ex.Message}");
    }
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();
