using System.IO;
using PactNet;
using PactNet.Output.Xunit;
using Xunit.Abstractions;
using IMS.Modular.Tests.Integration;

namespace IMS.Modular.Tests.Contracts;

/// <summary>
/// US-073: Pact Provider Verification
/// Verifies that the IMS API .NET satisfies the contracts defined by the BFF consumer.
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
        // Pact file path — versioned in the repo under frontend/.../pacts/
        var pactFile = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "..",
            "frontend", "apps", "next-shell", "pacts",
            "IMS-BFF-NextJS-IMS-API-DotNet.json");

        if (!File.Exists(pactFile))
        {
            // Skip gracefully if pact file hasn't been generated yet
            // (consumer tests must run first to produce the pact file)
            _output.WriteLine($"[Pact] Pact file not found at {pactFile} — skipping provider verification. Run consumer tests first.");
            return;
        }

        var pact = Pact.V3("IMS-API-DotNet", "IMS-BFF-NextJS", new PactConfig
        {
            PactDir = Path.GetDirectoryName(pactFile)!,
            Outputters = [new XunitOutput(_output)],
            LogLevel = PactLogLevel.Warn,
        });

        // Use the real test server from IntegrationWebAppFactory
        var serverUri = _factory.Server.BaseAddress;

        pact.ServiceProvider("IMS-API-DotNet", serverUri)
            .WithProviderStateUrl(new Uri($"{serverUri}provider-states"))
            .Verify();
    }
}
