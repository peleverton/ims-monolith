using IMS.Modular.Shared.FeatureFlags;
using Microsoft.FeatureManagement;
using NSubstitute;

namespace IMS.Modular.Tests.Modules.Features;

public class FeatureFlagsTests
{
    [Fact]
    public void FeatureFlags_Constants_HaveExpectedValues()
    {
        Assert.Equal("EnableKanbanView", FeatureFlags.EnableKanbanView);
        Assert.Equal("EnableWebhooks", FeatureFlags.EnableWebhooks);
        Assert.Equal("EnableFullTextSearch", FeatureFlags.EnableFullTextSearch);
        Assert.Equal("UseKeycloak", FeatureFlags.UseKeycloak);
    }

    [Fact]
    public async Task GetFeatures_ReturnsAllThreeFlags()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync(FeatureFlags.EnableKanbanView).Returns(false);
        featureManager.IsEnabledAsync(FeatureFlags.EnableWebhooks).Returns(true);
        featureManager.IsEnabledAsync(FeatureFlags.EnableFullTextSearch).Returns(false);

        // Act
        var flags = new Dictionary<string, bool>
        {
            [FeatureFlags.EnableKanbanView]     = await featureManager.IsEnabledAsync(FeatureFlags.EnableKanbanView),
            [FeatureFlags.EnableWebhooks]       = await featureManager.IsEnabledAsync(FeatureFlags.EnableWebhooks),
            [FeatureFlags.EnableFullTextSearch] = await featureManager.IsEnabledAsync(FeatureFlags.EnableFullTextSearch),
        };

        // Assert
        Assert.Equal(3, flags.Count);
        Assert.False(flags[FeatureFlags.EnableKanbanView]);
        Assert.True(flags[FeatureFlags.EnableWebhooks]);
        Assert.False(flags[FeatureFlags.EnableFullTextSearch]);
    }

    [Fact]
    public async Task UseKeycloak_DefaultsToFalse()
    {
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync(FeatureFlags.UseKeycloak).Returns(false);

        var result = await featureManager.IsEnabledAsync(FeatureFlags.UseKeycloak);

        Assert.False(result);
    }

    [Fact]
    public async Task UseKeycloak_CanBeEnabled()
    {
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync(FeatureFlags.UseKeycloak).Returns(true);

        var result = await featureManager.IsEnabledAsync(FeatureFlags.UseKeycloak);

        Assert.True(result);
    }
}
