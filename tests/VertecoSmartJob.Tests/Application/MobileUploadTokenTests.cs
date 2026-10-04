using Microsoft.Extensions.Configuration;
using VertecoSmartJob.Api.Controllers.Mobile;

namespace VertecoSmartJob.Tests.Application;

public class MobileUploadTokenTests
{
    private static MobileUploadTokenService CreateService() => new(
        new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "local-test-signing-key-with-enough-length-123"
            }).Build());

    [Fact]
    public void UploadToken_IsBoundToJobTechnicianAndKind()
    {
        var service = CreateService();
        var jobId = Guid.NewGuid();
        var technicianId = Guid.NewGuid();
        var token = service.Issue("abc.jpg", jobId, technicianId,
            MobileUploadKind.Evidence, DateTimeOffset.UtcNow.AddMinutes(10));

        Assert.True(service.TryRead(token, jobId, technicianId,
            MobileUploadKind.Evidence, out var blobName));
        Assert.Equal("abc.jpg", blobName);
        Assert.False(service.TryRead(token, Guid.NewGuid(), technicianId,
            MobileUploadKind.Evidence, out _));
        Assert.False(service.TryRead(token, jobId, Guid.NewGuid(),
            MobileUploadKind.Evidence, out _));
        Assert.False(service.TryRead(token, jobId, technicianId,
            MobileUploadKind.Signature, out _));
    }

    [Fact]
    public void UploadToken_RejectsExpiryAndTampering()
    {
        var service = CreateService();
        var jobId = Guid.NewGuid();
        var technicianId = Guid.NewGuid();
        var expired = service.Issue("abc.jpg", jobId, technicianId,
            MobileUploadKind.Evidence, DateTimeOffset.UtcNow.AddSeconds(-1));
        Assert.False(service.TryRead(expired, jobId, technicianId,
            MobileUploadKind.Evidence, out _));

        var valid = service.Issue("abc.jpg", jobId, technicianId,
            MobileUploadKind.Evidence, DateTimeOffset.UtcNow.AddMinutes(10));
        var tampered = "X" + valid[1..];
        Assert.False(service.TryRead(tampered, jobId, technicianId,
            MobileUploadKind.Evidence, out _));
    }
}
