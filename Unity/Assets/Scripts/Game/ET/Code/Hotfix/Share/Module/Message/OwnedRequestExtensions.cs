#if !DOTNET
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    [FriendOf(typeof(ProcessInnerSender))]
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
                long sessionInstance = session.InstanceId;
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
                catch (RpcException) when (session.InstanceId != sessionInstance)
                {
                    // Session destruction ends outstanding waits, including replies after a committed snapshot.
                    throw new OperationCanceledException(inner.CancellationToken);
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
            ActorId target, IRequest request, Func<IResponse, T> read, Action physicalFinished = null)
        {
            bool handedOff = false;
            try
            {
                context.ThrowIfStopped();
                if (sender == null || sender.IsDisposed || sender.IsStopped)
                    throw new ObjectDisposedException(nameof(ProcessInnerSender));
                return context.Scope.RunAsync(async inner =>
                {
                    handedOff = true;
                    IResponse response = null;
                    try
                    {
                        response = await sender.Call(target, request, false);
                        inner.ThrowIfStopped();
                        if (response.Error == ErrorCore.ERR_Cancel) throw new OperationCanceledException(inner.CancellationToken);
                        T value = default;
                        inner.Apply(() => value = read(response));
                        return value;
                    }
                    finally
                    {
                        try { (response as MessageObject)?.Dispose(); }
                        finally { physicalFinished?.Invoke(); }
                    }
                });
            }
            catch
            {
                if (!handedOff)
                {
                    try { (request as MessageObject)?.Dispose(); }
                    finally { physicalFinished?.Invoke(); }
                }
                throw;
            }
        }
    }
}
#endif
