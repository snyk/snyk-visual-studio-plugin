using System;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Snyk.VisualStudio.Extension.Language;

public interface IJsonRpc
{
    Task<T> InvokeAsync<T>(string targetName, CancellationToken cancellationToken);
    Task<T> InvokeWithParameterObjectAsync<T>(string targetName, object argument, CancellationToken cancellationToken);
    Task NotifyWithParameterObjectAsync(string targetName, object argument);
    event EventHandler<JsonRpcDisconnectedEventArgs> Disconnected;
    bool AllowModificationWhileListening { get; set; }
    IActivityTracingStrategy ActivityTracingStrategy { get; set; }
}