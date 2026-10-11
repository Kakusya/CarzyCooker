#if !DOTNET
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    public static class OwnedRequestExtensions
    {
        public static UniTask DelayAsync(this AsyncContext context, TimerComponent timer, long milliseconds)
        {
            context.ThrowIfStopped();
            return context.Scope.RunAsync(inner => timer.WaitAsync(milliseconds, inner.CancellationToken));
        }

        // The adapter owns the outgoing Session request and its returned response, and returns a value.
        public static UniTask<T> CallSessionValueAsync<T>(this AsyncContext context, Session session, IRequest request,
            Func<IResponse, T> read, int timeoutMilliseconds = 0)
        {
            context.ThrowIfStopped();
            return context.Scope.RunAsync(async inner =>
            {
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(inner.CancellationToken);
                bool timeout = false;
                bool finished = false;
                if (timeoutMilliseconds > 0)
                {
                    async UniTaskVoid Timeout()
                    {
                        try
                        {
                            await session.Root().GetComponent<TimerComponent>().WaitAsync(timeoutMilliseconds, cancellation.Token);
                            if (!finished && !inner.CancellationToken.IsCancellationRequested)
                            {
                                timeout = true;
                                cancellation.Cancel();
                            }
                        }
                        catch (OperationCanceledException) { }
                    }
                    Timeout().Forget();
                }
                IResponse response = null;
                try
                {
                    response = await session.Call(request, cancellation.Token);
                    inner.ThrowIfStopped();
                    if (response.Error == ErrorCore.ERR_Cancel)
                    {
                        if (timeout) throw new TimeoutException("Session request timed out.");
                        throw new OperationCanceledException(inner.CancellationToken);
                    }
                    T value = default;
                    inner.Apply(() => value = read(response));
                    return value;
                }
                finally
                {
                    finished = true;
                    cancellation.Cancel();
                    (response as MessageObject)?.Dispose();
                    (request as MessageObject)?.Dispose();
                }
            });
        }

        // Sender/receiver keep input ownership. Single-owner cancellation does not Stop the sender.
        public static UniTask<T> CallLocalValueAsync<T>(this AsyncContext context, ProcessInnerSender sender,
            ActorId target, IRequest request, Func<IResponse, T> read)
        {
            context.ThrowIfStopped();
            return context.Scope.RunAsync(async inner =>
            {
                IResponse response = await sender.Call(target, request, false);
                try
                {
                    inner.ThrowIfStopped();
                    if (response.Error == ErrorCore.ERR_Cancel) throw new OperationCanceledException(inner.CancellationToken);
                    T value = default;
                    inner.Apply(() => value = read(response));
                    return value;
                }
                finally { (response as MessageObject)?.Dispose(); }
            });
        }
    }
}
#endif
