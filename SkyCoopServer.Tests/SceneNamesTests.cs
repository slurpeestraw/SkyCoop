using Xunit;

namespace SkyCoopServer.Tests
{
    public class SceneNamesTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Empty")]
        [InlineData("Boot")]
        [InlineData("MainMenu")]
        [InlineData("MainMenuDLC")]
        public void IsGameplay_RejectsMenuAndTransitionScenes(string? scene)
        {
            Assert.False(SceneNames.IsGameplay(scene));
        }

        [Theory]
        [InlineData("LakeRegion")]
        [InlineData("CoastalRegion")]
        public void IsGameplay_AcceptsRegions(string scene)
        {
            Assert.True(SceneNames.IsGameplay(scene));
        }
    }
}
