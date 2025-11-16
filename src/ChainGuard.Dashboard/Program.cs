using ChainGuard.Core.Services;
using ChainGuard.Data;
using ChainGuard.Data.Repositories;
using ChainGuard.Data.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure SQLite for demo (can be changed to SQL Server in production)
builder.Services.AddDbContext<ChainGuardDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ChainGuard") 
        ?? "Data Source=chainguard_dashboard.db"));

// Register RSA singleton for signing/verification
builder.Services.AddSingleton<RSA>(provider =>
{
    var rsa = RSA.Create(2048);
    // In production, load keys from secure storage
    return rsa;
});

// Register encryption service
builder.Services.AddSingleton<IEncryptionService>(provider =>
{
    // In production, load encryption key from secure storage
    var encryptionService = new AesEncryptionService(
        builder.Configuration["Encryption:Key"] ?? GenerateEncryptionKey());
    return encryptionService;
});

// Register repositories
builder.Services.AddScoped<IChainRepository, ChainRepository>();
builder.Services.AddScoped<IBlockRepository, BlockRepository>();
builder.Services.AddScoped<IOffChainDataRepository, OffChainDataRepository>();

// Register services
builder.Services.AddScoped<IAuditChainService, AuditChainService>();

static string GenerateEncryptionKey()
{
    var key = new byte[32]; // 256 bits
    RandomNumberGenerator.Fill(key);
    return Convert.ToBase64String(key);
}

var app = builder.Build();

// Apply database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChainGuardDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
