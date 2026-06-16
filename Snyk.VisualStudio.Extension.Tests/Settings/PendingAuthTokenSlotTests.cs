using Snyk.VisualStudio.Extension.Settings;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.Settings
{
    public class PendingAuthTokenSlotTests
    {
        [Fact]
        public void Take_WhenEmpty_ReturnsNull()
        {
            var slot = new PendingAuthTokenSlot();

            Assert.Null(slot.Take());
        }

        [Fact]
        public void Set_ThenTake_ReturnsToken_AndConsumesItOnce()
        {
            var slot = new PendingAuthTokenSlot();
            slot.Set("tok", "https://api.snyk.io");

            var taken = slot.Take();

            Assert.NotNull(taken);
            Assert.Equal("tok", taken.Token);
            Assert.Equal("https://api.snyk.io", taken.ApiUrl);
            Assert.Null(slot.Take()); // take-once: a second take yields nothing
        }

        [Fact]
        public void Set_Twice_Take_ReturnsTheLatest()
        {
            var slot = new PendingAuthTokenSlot();
            slot.Set("old", "https://old");
            slot.Set("new", "https://new");

            var taken = slot.Take();

            Assert.Equal("new", taken.Token);   // last write wins
            Assert.Equal("https://new", taken.ApiUrl);
        }
    }
}
