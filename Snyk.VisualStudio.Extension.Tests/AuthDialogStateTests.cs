using Snyk.VisualStudio.Extension;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class AuthDialogStateTests
    {
        [Fact]
        public void New_ShouldShow_WhenNotVisible()
        {
            var state = new AuthDialogState();

            Assert.False(state.ResultArrived);
            Assert.True(state.ShouldShow(isVisible: false));
        }

        [Fact]
        public void ShouldShow_IsFalse_WhenAlreadyVisible()
        {
            var state = new AuthDialogState();

            // Re-showing a visible modal would throw, so the show must be skipped.
            Assert.False(state.ShouldShow(isVisible: true));
        }

        [Fact]
        public void RecordResult_SuppressesAPendingShow()
        {
            // The "result arrived before the show ran" race: hide-before-show must suppress the show.
            var state = new AuthDialogState();
            state.Arm();

            state.RecordResult();

            Assert.True(state.ResultArrived);
            Assert.False(state.ShouldShow(isVisible: false));
        }

        [Fact]
        public void Arm_ResetsAPriorResult_SoTheNextShowProceeds()
        {
            var state = new AuthDialogState();
            state.RecordResult();
            Assert.False(state.ShouldShow(isVisible: false));

            state.Arm();

            Assert.False(state.ResultArrived);
            Assert.True(state.ShouldShow(isVisible: false));
        }
    }
}
