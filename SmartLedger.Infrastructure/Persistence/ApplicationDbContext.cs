using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartLedger.Domain.Entities;
using SmartLedger.Infrastructure.Common;
using SmartLedger.Infrastructure.Identity;

namespace SmartLedger.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenantContext) : IdentityDbContext<ApplicationUser>(options)
{
    private readonly Guid _tenantId = tenantContext.CurrentTenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<GstEntry> GstEntries => Set<GstEntry>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TransactionEmbedding> TransactionEmbeddings => Set<TransactionEmbedding>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BusinessName).HasMaxLength(200).IsRequired();
            e.Property(x => x.OwnerEmail).HasMaxLength(256).IsRequired();
            e.Property(x => x.Gstin).HasMaxLength(15);
            e.HasIndex(x => x.OwnerEmail);
        });

        builder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.VendorName).HasMaxLength(300).IsRequired();
            e.Property(x => x.VendorGstin).HasMaxLength(15);
            e.Property(x => x.InvoiceNumber).HasMaxLength(100);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.BlobUrl).HasMaxLength(1000);
            e.Property(x => x.OriginalFileName).HasMaxLength(500);
            e.Property(x => x.AnomalyReason).HasMaxLength(1000);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxableAmount).HasPrecision(18, 2);
            e.Property(x => x.Cgst).HasPrecision(18, 2);
            e.Property(x => x.Sgst).HasPrecision(18, 2);
            e.Property(x => x.Igst).HasPrecision(18, 2);
            e.HasMany(x => x.LineItems)
                .WithOne()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.HasQueryFilter(x => _tenantId == Guid.Empty || x.TenantId == _tenantId);
            e.HasIndex(x => new { x.TenantId, x.InvoiceDate });
            e.Ignore(x => x.DomainEvents);
        });

        builder.Entity<InvoiceLineItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.GstRate).HasPrecision(5, 2);
            e.Ignore(x => x.DomainEvents);
        });

        builder.Entity<GstEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Period).HasMaxLength(7).IsRequired();
            e.Property(x => x.CounterpartyGstin).HasMaxLength(15);
            e.Property(x => x.InvoiceNumber).HasMaxLength(100);
            e.Property(x => x.TaxableValue).HasPrecision(18, 2);
            e.Property(x => x.Igst).HasPrecision(18, 2);
            e.Property(x => x.Cgst).HasPrecision(18, 2);
            e.Property(x => x.Sgst).HasPrecision(18, 2);
            e.HasQueryFilter(x => _tenantId == Guid.Empty || x.TenantId == _tenantId);
            e.HasIndex(x => new { x.TenantId, x.Period });
            e.Ignore(x => x.DomainEvents);
            e.Ignore(x => x.TotalTax);
            e.Ignore(x => x.MatchKey);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.Token).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasQueryFilter(x => _tenantId == Guid.Empty || x.TenantId == _tenantId);
            e.Ignore(x => x.DomainEvents);
            e.Ignore(x => x.IsActive);
        });

        builder.Entity<TransactionEmbedding>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Summary).HasMaxLength(2000);
            e.Property(x => x.EmbeddingJson).HasColumnType("nvarchar(max)");
            e.HasQueryFilter(x => _tenantId == Guid.Empty || x.TenantId == _tenantId);
            e.HasIndex(x => new { x.TenantId, x.DocumentId }).IsUnique();
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(200);
            e.HasIndex(x => x.TenantId);
        });
    }
}
