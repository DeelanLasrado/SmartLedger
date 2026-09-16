using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SmartLedger.AI.DependencyInjection;
using SmartLedger.API.Middleware;
using SmartLedger.Application.DependencyInjection;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Domain.ValueObjects;
using SmartLedger.Infrastructure.Common;
using SmartLedger.Infrastructure.DependencyInjection;
using SmartLedger.Infrastructure.Identity;
using SmartLedger.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAI(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartLedger API",
        Version = "v1",
        Description = "AI-Powered Financial Compliance & Expense Intelligence for Indian SMBs"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue("UseInMemoryDatabase", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Azure App Service terminates TLS at the front door — skip redirect in non-dev.
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

var hangfirePath = builder.Configuration["Hangfire:DashboardPath"];
if (string.IsNullOrWhiteSpace(hangfirePath) || !hangfirePath.StartsWith('/'))
    hangfirePath = "/hangfire";

app.UseHangfireDashboard(hangfirePath, new DashboardOptions
{
    Authorization = [new SmartLedger.API.Hangfire.AllowAllDashboardAuthorizationFilter()]
});

app.MapControllers();

// SPA: serve React app from wwwroot; keep API/Swagger/Hangfire intact.
app.MapFallbackToFile("index.html");

await SeedDemoDataAsync(app);

app.Run();

static async Task SeedDemoDataAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("UseInMemoryDatabase", false))
        return;

    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    tenantContext.CurrentTenantId = Guid.Empty;

    var db = sp.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (await db.Tenants.AnyAsync())
        return;

    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    var invoiceRepo = sp.GetRequiredService<IInvoiceRepository>();
    var tenantRepo = sp.GetRequiredService<ITenantRepository>();
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    var embeddingService = sp.GetRequiredService<IEmbeddingService>();
    var vectorStore = sp.GetRequiredService<IVectorStore>();

    var tenant = Tenant.Create("Demo Kirana Store", "demo@smartledger.local", "29AABCD1234G1Z7");
    await tenantRepo.AddAsync(tenant);

    var user = new ApplicationUser
    {
        UserName = "demo@smartledger.local",
        Email = "demo@smartledger.local",
        EmailConfirmed = true,
        TenantId = tenant.Id,
        FullName = "Demo Owner"
    };
    await userManager.CreateAsync(user, "Demo@12345");

    var samples = new[]
    {
        new ExtractedInvoiceData
        {
            VendorName = "Reliance Retail Ltd",
            VendorGstin = "27AABCR1234A1Z5",
            InvoiceNumber = "RR-1001",
            InvoiceDate = DateTime.UtcNow.Date.AddDays(-20),
            TotalAmount = 8500m,
            TaxableAmount = 7203.39m,
            Cgst = 648.31m,
            Sgst = 648.30m,
            Category = "Inventory",
            LineItems = [new ExtractedLineItem("Wholesale groceries", 1, 7203.39m, 7203.39m, 18)]
        },
        new ExtractedInvoiceData
        {
            VendorName = "BESCOM Electricity",
            VendorGstin = "29AABCB1111E1Z3",
            InvoiceNumber = "BE-2201",
            InvoiceDate = DateTime.UtcNow.Date.AddDays(-12),
            TotalAmount = 3200m,
            TaxableAmount = 2711.86m,
            Cgst = 244.07m,
            Sgst = 244.07m,
            Category = "Utilities",
            LineItems = [new ExtractedLineItem("Electricity charges", 1, 2711.86m, 2711.86m, 18)]
        },
        new ExtractedInvoiceData
        {
            VendorName = "Hindustan Petroleum",
            VendorGstin = "27AAACH1234F1Z6",
            InvoiceNumber = "HP-3310",
            InvoiceDate = DateTime.UtcNow.Date.AddDays(-5),
            TotalAmount = 5400m,
            TaxableAmount = 4576.27m,
            Cgst = 411.87m,
            Sgst = 411.86m,
            Category = "Fuel",
            LineItems = [new ExtractedLineItem("Diesel", 1, 4576.27m, 4576.27m, 18)]
        },
        new ExtractedInvoiceData
        {
            VendorName = "Flipkart Internet Pvt Ltd",
            VendorGstin = "29AABCF4321D1Z8",
            InvoiceNumber = "FK-8890",
            InvoiceDate = DateTime.UtcNow.Date.AddDays(-2),
            TotalAmount = 15200m,
            TaxableAmount = 12881.36m,
            Cgst = 1159.32m,
            Sgst = 1159.32m,
            Category = "Inventory",
            LineItems = [new ExtractedLineItem("Inventory restock", 1, 12881.36m, 12881.36m, 18)]
        }
    };

    foreach (var data in samples)
    {
        var invoice = Invoice.Create(tenant.Id, data, "/uploads/demo.pdf", "demo.pdf");
        await invoiceRepo.AddAsync(invoice);
        tenant.IncrementInvoiceUsage();
        var summary = invoice.ToSummary();
        var embedding = await embeddingService.EmbedAsync(summary);
        await vectorStore.UpsertAsync(tenant.Id, invoice.Id, summary, embedding);
    }

    await tenantRepo.UpdateAsync(tenant);
    await unitOfWork.SaveChangesAsync();

    app.Logger.LogInformation(
        "Seeded demo tenant '{Business}' ({Email} / Demo@12345)",
        tenant.BusinessName, user.Email);
}

public partial class Program;
