using System;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class WebView2MessageDispatcherTest
    {
        [Fact]
        public void Dispatch_KnownMethod_InvokesRegisteredHandlerWithArgs()
        {
            string capturedString = null;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("__saveIdeConfig__", 1, args => capturedString = args[0].Value<string>());

            dispatcher.Dispatch("{\"method\":\"__saveIdeConfig__\",\"args\":[\"{\\\"foo\\\":1}\"]}");

            Assert.Equal("{\"foo\":1}", capturedString);
        }

        [Fact]
        public void Dispatch_BooleanArgument_RoundTripsAsBool()
        {
            bool? captured = null;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("__onFormDirtyChange__", 1, args => captured = args[0].Value<bool>());

            dispatcher.Dispatch("{\"method\":\"__onFormDirtyChange__\",\"args\":[true]}");

            Assert.True(captured);
        }

        [Fact]
        public void Dispatch_MultipleArguments_PreservesOrder()
        {
            string[] captured = null;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("OpenFileInEditor", 5, args => captured = new[]
            {
                args[0].Value<string>(),
                args[1].Value<string>(),
                args[2].Value<string>(),
                args[3].Value<string>(),
                args[4].Value<string>(),
            });

            dispatcher.Dispatch(
                "{\"method\":\"OpenFileInEditor\",\"args\":[\"/path/to/file\",\"1\",\"5\",\"0\",\"10\"]}");

            Assert.Equal(new[] { "/path/to/file", "1", "5", "0", "10" }, captured);
        }

        [Fact]
        public void Dispatch_MissingArgsField_NoArgHandlerStillRuns()
        {
            JArray captured = null;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("FocusToolWindow", 0, args => captured = args);

            dispatcher.Dispatch("{\"method\":\"FocusToolWindow\"}");

            Assert.NotNull(captured);
            Assert.Empty(captured);
        }

        [Fact]
        public void Dispatch_TooFewArgs_DropsAndDoesNotInvokeHandler()
        {
            var called = false;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("OpenFileInEditor", 5, _ => called = true);

            // Only 3 args, handler requires 5
            dispatcher.Dispatch("{\"method\":\"OpenFileInEditor\",\"args\":[\"/path\",\"1\",\"2\"]}");

            Assert.False(called);
        }

        [Fact]
        public void Dispatch_MoreArgsThanExpected_StillInvokesHandler()
        {
            var called = false;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("OpenLink", 1, _ => called = true);

            // 2 args, only 1 expected — extras are passed through
            dispatcher.Dispatch("{\"method\":\"OpenLink\",\"args\":[\"https://snyk.io\",\"extra\"]}");

            Assert.True(called);
        }

        [Fact]
        public void Dispatch_UnknownMethod_DoesNotThrowAndDoesNotInvokeOtherHandlers()
        {
            var knownCalled = false;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("known", 0, _ => knownCalled = true);

            dispatcher.Dispatch("{\"method\":\"unknown\",\"args\":[]}");

            Assert.False(knownCalled);
        }

        [Theory]
        [InlineData("not json at all")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("{}")]
        [InlineData("{\"args\":[\"x\"]}")]
        [InlineData("{\"method\":\"\"}")]
        public void Dispatch_InvalidPayload_DoesNotThrow(string payload)
        {
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("foo", 0, _ => { });

            var ex = Record.Exception(() => dispatcher.Dispatch(payload));

            Assert.Null(ex);
        }

        [Fact]
        public void Dispatch_HandlerThrows_ExceptionDoesNotPropagate()
        {
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("explodes", 0, _ => throw new InvalidOperationException("boom"));

            var ex = Record.Exception(() => dispatcher.Dispatch("{\"method\":\"explodes\",\"args\":[]}"));

            Assert.Null(ex);
        }

        [Fact]
        public void Register_ReturnsSelf_ToAllowChaining()
        {
            var dispatcher = new WebView2MessageDispatcher();

            var returned = dispatcher
                .Register("a", 0, _ => { })
                .Register("b", 0, _ => { });

            Assert.Same(dispatcher, returned);
        }

        [Fact]
        public void Register_DuplicateMethod_LastRegistrationWins()
        {
            var calls = 0;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("foo", 0, _ => calls += 1);
            dispatcher.Register("foo", 0, _ => calls += 10);

            dispatcher.Dispatch("{\"method\":\"foo\",\"args\":[]}");

            Assert.Equal(10, calls);
        }

        [Fact]
        public void Register_NegativeExpectedArgCount_Throws()
        {
            var dispatcher = new WebView2MessageDispatcher();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => dispatcher.Register("foo", -1, _ => { }));
        }
    }
}
