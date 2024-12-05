using System;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Snyk.VisualStudio.Extension.Language;

public class JsonRpcWrapper : IJsonRpc
{
    private readonly JsonRpc jsonRpc;

    public JsonRpcWrapper(JsonRpc jsonRpc)
    {
        this.jsonRpc = jsonRpc;
    }

    public event EventHandler<JsonRpcDisconnectedEventArgs> Disconnected
    {
        add => jsonRpc.Disconnected += value;
        remove => jsonRpc.Disconnected -= value;
    }

    public bool AllowModificationWhileListening
    {
        get => jsonRpc.AllowModificationWhileListening;
        set => jsonRpc.AllowModificationWhileListening = value;
    }

    public IActivityTracingStrategy ActivityTracingStrategy
    {
        get => jsonRpc.ActivityTracingStrategy;
        set => jsonRpc.ActivityTracingStrategy = value;
    }

    public Task<T> InvokeAsync<T>(string targetName, CancellationToken cancellationToken)
    {
        return jsonRpc.InvokeAsync<T>(targetName, cancellationToken);
    }

    public Task<T> InvokeWithParameterObjectAsync<T>(string targetName, object argument, CancellationToken cancellationToken)
    {
        return jsonRpc.InvokeWithParameterObjectAsync<T>(targetName, argument, cancellationToken);
    }

    public Task NotifyWithParameterObjectAsync(string targetName, object argument)
    {
        return jsonRpc.NotifyWithParameterObjectAsync(targetName, argument);
    }
}