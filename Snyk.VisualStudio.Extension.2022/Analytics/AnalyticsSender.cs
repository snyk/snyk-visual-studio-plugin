using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.Common;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Analytics
{
    public class AnalyticsSender
    {
        private class Pair
        {
            private Pair()
            {
            }

            public Pair(IAbstractAnalyticsEvent analyticsEvent, Action<object> callback)
            {
                this.Callback = callback;
                this.Event = analyticsEvent;
            }

            public IAbstractAnalyticsEvent Event { get; set; }
            public Action<object> Callback { get; set; }
        }

        // left = event, right = callback function
        private readonly ConcurrentQueue<Pair> eventQueue = new ConcurrentQueue<Pair>();
        private readonly ISnykOptions settings;
        private readonly ILogger logger = LogManager.ForContext<AnalyticsSender>();

        private static AnalyticsSender _instance;

        private AnalyticsSender(ISnykOptions settings, ILanguageClientManager languageClientManager)
        {
            this.settings = settings;
            ThreadHelper.JoinableTaskFactory.RunAsync(StartAsync).FireAndForget();
        }


        public static AnalyticsSender Instance(ISnykOptions settings, ILanguageClientManager languageClientManager)
        {
            return _instance ??= new AnalyticsSender(settings, languageClientManager);
        }

        private async Task StartAsync()
        {
            while (true)
            {
                var authToken = settings.ApiToken.ToString();
                var initialized = LanguageClientHelper.IsLanguageServerReady();
                var iAmTired = !initialized || eventQueue.IsEmpty || string.IsNullOrWhiteSpace(authToken);
                if (iAmTired)
                {
                    await Task.Delay(1000);
                    continue;
                }

                var copyForSending = new List<Pair>(eventQueue);
                foreach (var pair in copyForSending)
                {
                    try
                    {
                        await LanguageClientHelper.LanguageClientManager()
                            .InvokeReportAnalyticsAsync(pair.Event, SnykVSPackage.Instance.DisposalToken);
                        pair.Callback(null);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "An error occured while sending analytics event");
                    }
                    finally
                    {
                        // this is a FIFO queue - so the first element should be the one we just sent
                        eventQueue.TryDequeue(out _);
                    }
                }
            }
        }

        public void LogEvent(IAbstractAnalyticsEvent analyticsEvent, Action<object> callback)
        {
            eventQueue.Enqueue(new Pair(analyticsEvent, callback));
        }
    }
}