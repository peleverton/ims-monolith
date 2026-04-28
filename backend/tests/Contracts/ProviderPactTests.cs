using System.IO;
using PactNet;
using PactNet.Verifier;
using Xunit.Abstractions;
using IMS.Modular.Tests.Integration;

namespace IMS.Modular.Tests.Contracts;

/// <summary>
/// US-073: Pact Provider Verification
/// Verifies that the IMS API .NET satisfies the contracts defined by the BFF consumer.
/// Uses PactNet v5 IPactVerifier API.
/// </summary>
public class ProviderPactTests : IClassFixture<IntegrationWebAppFactory>
{
    private readonly IntegrationWebAppFactory _factory;
    private readonly ITestOutputHelper _output;

    public ProviderPactTests(IntegrationWebAppFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public void VerifyPactWithBff()
    {
        var pactFile = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "..",
            "frontend", "apps", "next-shell", "pacts",
            "IMS-BFF-NextJS-IMS-API-DotNet.json"));

        if (!File.Exists(pactFile))
        {
            _output.WriteLine($"[Pact] Pact file not found at {pactFile} — skipping. Run consumer tests first.");
            return;
        }

        var serverUri = _factory.Server.BaseAddress;

        var verifier = new PactVerifier("IMS-API-DotNet", new PactVerifierConfig
        {
            LogLevel = PactLogLevel.Warn,
        });

        verifier
            .WithHttpEndpoint(serverUri)
            .WithFileSource(new FileInfo(pactFile))
            .WithProviderStateUrl(new Uri($"{serverUri}provider-states"))
            .Verify();
    }
}
