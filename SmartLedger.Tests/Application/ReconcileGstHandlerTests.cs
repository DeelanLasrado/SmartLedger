using FluentAssertions;
using Moq;
using SmartLedger.Application.Gst.Commands;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Tests.Application;

public class ReconcileGstHandlerTests
{
    [Fact]
    public async Task Handle_Returns_Cached_Result_When_Present()
    {
        var tenantId = Guid.NewGuid();
        var cached = new GstReconciliationResult("2026-01", 2, 0, 100m, []);

        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<GstReconciliationResult>(
                $"gst:recon:{tenantId}:2026-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var recon = new Mock<IGstReconciliationService>(MockBehavior.Strict);

        var handler = new ReconcileGstHandler(recon.Object, cache.Object);
        var result = await handler.Handle(new ReconcileGstCommand(tenantId, "2026-01"), CancellationToken.None);

        result.Should().BeSameAs(cached);
        recon.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Reconciles_And_Caches_When_Miss()
    {
        var tenantId = Guid.NewGuid();
        var fresh = new GstReconciliationResult("2026-02", 1, 1, 50m,
        [
            new GstMismatch("INV-1", "27AABCR1234A1Z5", 100, 80, "mismatch")
        ]);

        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<GstReconciliationResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GstReconciliationResult?)null);

        var recon = new Mock<IGstReconciliationService>();
        recon.Setup(r => r.ReconcileAsync(tenantId, "2026-02", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fresh);

        var handler = new ReconcileGstHandler(recon.Object, cache.Object);
        var result = await handler.Handle(new ReconcileGstCommand(tenantId, "2026-02"), CancellationToken.None);

        result.MismatchCount.Should().Be(1);
        cache.Verify(c => c.SetAsync(
            $"gst:recon:{tenantId}:2026-02",
            fresh,
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
