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
            dispatcher.Register("__saveIdeConfig__", args => capturedString = args[0].Value<string>());

            dispatcher.Dispatch("{\"method\":\"__saveIdeConfig__\",\"args\":[\"{\\\"foo\\\":1}\"]}");

            Assert.Equal("{\"foo\":1}", capturedString);
        }

        [Fact]
        public void Dispatch_BooleanArgument_RoundTripsAsBool()
        {
            bool? captured = null;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("__onFormDirtyChange__", args => captured = args[0].Value<bool>());

            dispatcher.Dispatch("{\"method\":\"__onFormDirtyChange__\",\"args\":[true]}");

            Assert.True(captured);
        }

        [Fact]
        public void Dispatch_MultipleArguments_PreservesOrder()
        {
            string[] captured = null;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("OpenFileInEditor", args => captured = new[]
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
        public void Dispatch_MissingArgsField_HandlerReceivesEmptyArray()
        {
            JArray captured = null;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("FocusToolWindow", args => captured = args);

            dispatcher.Dispatch("{\"method\":\"FocusToolWindow\"}");

            Assert.NotNull(captured);
            Assert.Empty(captured);
        }

        [Fact]
        public void Dispatch_UnknownMethod_DoesNotThrowAndDoesNotInvokeOtherHandlers()
        {
            var knownCalled = false;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("known", _ => knownCalled = true);

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
            dispatcher.Register("foo", _ => { });

            var ex = Record.Exception(() => dispatcher.Dispatch(payload));

            Assert.Null(ex);
        }

        [Fact]
        public void Dispatch_HandlerThrows_ExceptionDoesNotPropagate()
        {
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("explodes", _ => throw new InvalidOperationException("boom"));

            var ex = Record.Exception(() => dispatcher.Dispatch("{\"method\":\"explodes\",\"args\":[]}"));

            Assert.Null(ex);
        }

        [Fact]
        public void Register_ReturnsSelf_ToAllowChaining()
        {
            var dispatcher = new WebView2MessageDispatcher();

            var returned = dispatcher
                .Register("a", _ => { })
                .Register("b", _ => { });

            Assert.Same(dispatcher, returned);
        }

        [Fact]
        public void Register_DuplicateMethod_LastRegistrationWins()
        {
            var calls = 0;
            var dispatcher = new WebView2MessageDispatcher();
            dispatcher.Register("foo", _ => calls += 1);
            dispatcher.Register("foo", _ => calls += 10);

            dispatcher.Dispatch("{\"method\":\"foo\",\"args\":[]}");

            Assert.Equal(10, calls);
        }
    }
}
