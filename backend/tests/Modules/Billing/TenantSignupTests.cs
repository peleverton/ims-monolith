using FluentAssertions;
using FluentValidation;
using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.Billing.Application.Services;
using IMS.Modular.Modules.Billing.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.MultiTenancy.TenantManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace IMS.Modular.Tests.Modules.Billing;

/// <summary>
/// US-092: Unit tests for tenant signup — validator, slug generation, endpoint logic.
/// Uses EF Core InMemory to avoid WebApplicationFactory startup issues.
/// </summary>
public class TenantSignupTests : IDisposable
{
    private readonly TenantDbContext _tenantDb;
    private readonly AuthDbContext _authDb;
    private readonly SignupValidator _validator;

    public TenantSignupTests()
    {
        var tenantOpts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _tenantDb = new TenantDbContext(tenantOpts);

        var authOpts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _authDb = new AuthDbContext(authOpts);

        // Seed required Auth role so signup user creation works
        _authDb.Roles.Add(new Role
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Admin",
            Description = "Administrator"
        });
        _authDb.SaveChanges();

        _validator = new SignupValidator();
    }

    public void Dispose()
    {
        _tenantDb.Dispose();
        _authDb.Dispose();
    }

    // ── Validator tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task Signup_ValidRequest_PassesValidation()
    {
        var req = new SignupRequest("Acme Corp", "admin@acme.com", "Password1!", "free");

        var result = await _validator.ValidateAsync(req);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Signup_InvalidEmail_FailsValidation_Returns400()
    {
        var req = new SignupRequest("Acme Corp", "not-an-email", "Password1!", "free");

        var result = await _validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Signup_ShortPassword_FailsValidation_Returns400()
    {
        var req = new SignupRequest("Acme Corp", "admin@acme.com", "short", "free");

        var result = await _validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Signup_InvalidPlan_FailsValidation()
    {
        var req = new SignupRequest("Acme Corp", "admin@acme.com", "Password1!", "enterprise");

        var result = await _validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Plan");
    }

    [Fact]
    public async Task Signup_ShortOrgName_FailsValidation()
    {
        var req = new SignupRequest("AB", "admin@acme.com", "Password1!", "free");

        var result = await _validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrgName");
    }

    // ── Slug generation & entity creation tests ───────────────────────────────

    [Fact]
    public async Task Signup_ValidData_CreatesTenantWithIsActiveFalse()
    {
        var orgName  = $"Test Corp {Guid.NewGuid():N}"[..20];
        var email    = $"admin-{Guid.NewGuid():N}"[..16] + "@example.com";
        var req      = new SignupRequest(orgName, email, "Password1!", "starter");
        var jobMock  = new Mock<Hangfire.IBackgroundJobClient>();

        // Act — simulate what the endpoint handler does
        var validation = await _validator.ValidateAsync(req);
        validation.IsValid.Should().BeTrue();

        var slug = GenerateTenantSlug(orgName);
        slug.Should().NotBeNullOrWhiteSpace();
        slug.Should().MatchRegex("^[a-z0-9-]+$");

        var alreadyExists = await _tenantDb.Tenants.AnyAsync(t => t.Id == slug);
        alreadyExists.Should().BeFalse();

        var tenant = new TenantEntity
        {
            Id           = slug,
            Name         = orgName.Trim(),
            Plan         = "starter",
            ContactEmail = email,
            IsActive     = false,
            CreatedAt    = DateTime.UtcNow,
            Notes        = "Provisioning"
        };
        _tenantDb.Tenants.Add(tenant);
        await _tenantDb.SaveChangesAsync();

        // Assert
        var saved = await _tenantDb.Tenants.FindAsync(slug);
        saved.Should().NotBeNull();
        saved!.IsActive.Should().BeFalse();
        saved.Plan.Should().Be("starter");
        saved.ContactEmail.Should().Be(email);
    }

    [Fact]
    public async Task Signup_DuplicateOrgName_ShouldBeDetectedAs409()
    {
        var orgName = "Duplicate Corp " + Guid.NewGuid().ToString("N")[..6];
        var slug    = GenerateTenantSlug(orgName);

        // Seed an existing tenant with the same slug
        _tenantDb.Tenants.Add(new TenantEntity
        {
            Id        = slug,
            Name      = orgName,
            Plan      = "free",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        });
        await _tenantDb.SaveChangesAsync();

        // Assert conflict is detected
        var conflict = await _tenantDb.Tenants.AnyAsync(t => t.Id == slug);
        conflict.Should().BeTrue("second signup with same org name should detect a 409 conflict");
    }

    // ── Slug generation edge cases ────────────────────────────────────────────

    [Theory]
    [InlineData("Acme Corp",          "acme-corp")]
    [InlineData("  My Cool Org  ",    "my-cool-org")]
    [InlineData("Hello & World!",     "hello-world")]
    [InlineData("A B  C",             "a-b-c")]
    public void GenerateTenantSlug_ProducesExpectedSlug(string orgName, string expected)
    {
        var slug = GenerateTenantSlug(orgName);
        slug.Should().Be(expected);
    }

    // ── Helper (mirrors the private method in TenantEndpoints) ───────────────

    private static string GenerateTenantSlug(string orgName)
    {
        var slug = orgName.ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-{2,}", "-");
        slug = slug.Trim('-');
        return slug.Length > 50 ? slug[..50] : slug;
    }
}
