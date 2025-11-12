using ChainGuard.Core.Services;
using ChainGuard.Data;
using ChainGuard.Data.Repositories;
using ChainGuard.Data.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() 
    { 
        Title = "ChainGuard API", 
        Version = "v1",
        Description = "Blockchain Integrity & Audit SDK for .NET Applications"
    });
});

// Configure SQLite for demo (can be changed to SQL Server in production)
builder.Services.AddDbContext<ChainGuardDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ChainGuard") 
        ?? "Data Source=chainguard.db"));

// Register RSA singleton for signing/verification
builder.Services.AddSingleton<RSA>(provider =>
{
    var rsa = RSA.Create(2048);
    // In production, load keys from secure storage (Azure Key Vault, AWS Secrets Manager, etc.)
    return rsa;
});

// Register encryption service
builder.Services.AddSingleton<ChainGuard.Core.Services.IEncryptionService>(provider =>
{
    // In production, load encryption key from secure storage (Azure Key Vault, AWS Secrets Manager, etc.)
    var encryptionService = new ChainGuard.Core.Services.AesEncryptionService(
        builder.Configuration["Encryption:Key"] ?? GenerateEncryptionKey());
    return encryptionService;
});

// Register repositories
builder.Services.AddScoped<IChainRepository, ChainRepository>();
builder.Services.AddScoped<IBlockRepository, BlockRepository>();
builder.Services.AddScoped<ChainGuard.Data.Repositories.IOffChainDataRepository, ChainGuard.Data.Repositories.OffChainDataRepository>();

// Register services
builder.Services.AddScoped<IAuditChainService, AuditChainService>();

static string GenerateEncryptionKey()
{
    var key = new byte[32]; // 256 bits
    System.Security.Cryptography.RandomNumberGenerator.Fill(key);
    return Convert.ToBase64String(key);
}

var app = builder.Build();

// Apply database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChainGuardDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ChainGuard API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
