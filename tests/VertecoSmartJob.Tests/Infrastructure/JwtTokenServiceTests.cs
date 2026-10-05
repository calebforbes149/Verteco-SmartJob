using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Infrastructure.Identity;

namespace VertecoSmartJob.Tests.Infrastructure;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "test-signing-key-at-least-32-characters-long",
            ["Jwt:Issuer"] = "VertecoSmartJob.Tests",
            ["Jwt:Audience"] = "VertecoSmartJob.Tests.Clients"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new JwtTokenService(configuration);
    }

    [Fact]
    public void IssueAccessToken_IncludesCorrectRoleAndNameIdentifierClaims()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var user = new User { FullName = "Test Manager", Email = "manager@verteco.example", Role = "Manager" };
        typeof(VertecoSmartJob.Domain.Common.Entity).GetProperty("Id")!.SetValue(user, userId);

        var token = service.IssueAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(userId.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("Manager", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "TechnicianId");
    }

    [Fact]
    public void IssueAccessToken_IncludesTechnicianIdClaim_WhenUserIsATechnician()
    {
        var service = CreateService();
        var technicianId = Guid.NewGuid();
        var user = new User { FullName = "Test Tech", Email = "tech@verteco.example", Role = "Technician", TechnicianId = technicianId };

        var token = service.IssueAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(technicianId.ToString(), jwt.Claims.Single(c => c.Type == "TechnicianId").Value);
    }

    [Fact]
    public void IssueAccessToken_SetsExpiryInTheFuture()
    {
        var service = CreateService();
        var user = new User { FullName = "Test", Email = "test@verteco.example", Role = "Manager" };

        var token = service.IssueAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(jwt.ValidTo > DateTime.UtcNow);
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(31)); // allow a few seconds of test runtime slack
    }
}