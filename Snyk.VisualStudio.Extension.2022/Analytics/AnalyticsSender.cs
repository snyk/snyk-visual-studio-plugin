using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Snyk.Common;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Analytics
{
    public class AnalyticsSender
    {
        // left = event, right = callback function
        private readonly ConcurrentQueue<(IAbstractAnalyticsEvent Event, Action<object> Callback)> eventQueue = new();
        private readonly ISnykOptions settings;
        private readonly ILogger logger = LogManager.ForContext<AnalyticsSender>();

        private static AnalyticsSender _instance;

        private AnalyticsSender(ISnykOptions settings)
        {
            this.settings = settings;
#pragma warning disable VSTHRD110
            Task.Run(StartAsync);
#pragma warning restore VSTHRD110
        }


        public static AnalyticsSender Instance(ISnykOptions settings)
        {
            return _instance ??= new AnalyticsSender(settings);
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

                var copyForSending = new List<(IAbstractAnalyticsEvent Event, Action<object> Callback)>(eventQueue);
                foreach (var (analyticsEvent, callback) in copyForSending)
                {
                    try
                    {
                        var cancellationToken = new CancellationToken();
                        await LanguageClientHelper.LanguageClientManager()
                            .InvokeReportAnalytics(analyticsEvent, cancellationToken);
                        callback(null);
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
            eventQueue.Enqueue((analyticsEvent, callback));
        }
    }
}