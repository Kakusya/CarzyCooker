using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game
{
    public interface IAsyncOwner
    {
        AsyncScope AsyncScope { get; }
    }

    // One runtime and one scope per entry. Neither completion records nor scopes are pooled.
    public sealed class AsyncRuntime
    {
        private readonly HashSet<AsyncScope> m_Scopes = new HashSet<AsyncScope>();
        private readonly ConditionalWeakTable<Exception, object> m_ObservedErrors = new ConditionalWeakTable<Exception, object>();
        public static AsyncRuntime Current { get; private set; }
        public bool IsActive { get; private set; } = true;
        public Action<Exception> ErrorObserver { get; set; } = e => UnityGameFramework.Runtime.Log.Error(e.ToString());

        public static AsyncRuntime Begin()
        {
            Current = new AsyncRuntime();
            return Current;
        }

        internal void Register(AsyncScope scope)
        {
            if (!IsActive) throw new ObjectDisposedException(nameof(AsyncRuntime));
            m_Scopes.Add(scope);
        }

        internal void Remove(AsyncScope scope) => m_Scopes.Remove(scope);
        internal void Report(Exception e)
        {
            if (m_ObservedErrors.TryGetValue(e, out _)) return;
            m_ObservedErrors.Add(e, new object());
            try { ErrorObserver?.Invoke(e); }
            catch (Exception observerError) { UnityGameFramework.Runtime.Log.Error(observerError.ToString()); }
        }

        public void Stop()
        {
            if (!IsActive) return;
            IsActive = false;
            var scopes = new List<AsyncScope>(m_Scopes);
            m_Scopes.Clear();
            // Invalidate the entire runtime before any synchronous cancellation continuation.
            foreach (var scope in scopes) scope.Invalidate();
            foreach (var scope in scopes) scope.CompleteStop();
            if (ReferenceEquals(Current, this)) Current = null;
        }
    }

    public sealed class AsyncScope : IAsyncOwner
    {
        private readonly HashSet<IWork> m_Work = new HashSet<IWork>();
        private readonly CancellationTokenSource m_Cancellation = new CancellationTokenSource();
        private readonly Func<bool> m_Identity;
        private readonly CancellationToken m_Token;
        private bool m_Active = true;
        private bool m_StopCompleted;
        private ResourceContainer m_Resources;
        public AsyncRuntime Runtime { get; }
        public AsyncScope Parent { get; }
        AsyncScope IAsyncOwner.AsyncScope => this;
        public bool IsActive => m_Active && Runtime.IsActive && (Parent == null || Parent.IsActive) && (m_Identity == null || m_Identity());
        public int PendingCount => m_Work.Count;
        public CancellationToken CancellationToken => m_Token;
        public event Action Stopped;

        public ResourceContainer GetResources()
        {
            if (!IsActive) throw new ObjectDisposedException(nameof(AsyncScope));
            return m_Resources ?? (m_Resources = ResourceContainer.Create(this));
        }

        public AsyncScope(AsyncRuntime runtime, Func<bool> identity = null, AsyncScope parent = null)
        {
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            Parent = parent;
            m_Identity = identity;
            m_Token = m_Cancellation.Token;
            if (parent != null && (!ReferenceEquals(parent.Runtime, runtime) || !parent.IsActive))
                throw new InvalidOperationException("The parent scope must belong to the active runtime.");
            Runtime.Register(this);
            if (parent != null) parent.Stopped += Stop;
        }

        internal interface IWork { void Cancel(); }
        internal bool Apply(Action action)
        {
            if (!IsActive) return false;
            action();
            return true;
        }

        public UniTask<T> RunAsync<T>(Func<AsyncContext, UniTask<T>> operation)
            => RunAsync(operation, true);

        internal UniTask<T> RunAsync<T>(Func<AsyncContext, UniTask<T>> operation, bool observeErrors)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (!IsActive) throw new ObjectDisposedException(nameof(AsyncScope));
            var work = new Work<T>(this, observeErrors);
            m_Work.Add(work);
            work.Observe(operation).Forget();
            return work.Completion.Task;
        }

        public UniTask RunAsync(Func<AsyncContext, UniTask> operation)
        {
            return RunAsync(async context => { await operation(context); return true; }).AsUniTask();
        }

        public void Stop()
        {
            Invalidate();
            CompleteStop();
        }

        internal void Invalidate() => m_Active = false;

        internal void CompleteStop()
        {
            if (m_StopCompleted) return;
            m_StopCompleted = true;
            var work = new List<IWork>(m_Work);
            m_Work.Clear();
            Runtime.Remove(this);
            if (Parent != null) Parent.Stopped -= Stop;
            foreach (var item in work)
            {
                try { item.Cancel(); }
                catch (Exception e) { Runtime.Report(e); }
            }
            try { m_Cancellation.Cancel(); }
            catch (Exception e) { Runtime.Report(e); }
            var stopped = Stopped;
            Stopped = null;
            if (stopped != null)
            {
                foreach (Action callback in stopped.GetInvocationList())
                {
                    try { callback(); }
                    catch (Exception e) { Runtime.Report(e); }
                }
            }
            m_Cancellation.Dispose();
            var resources = m_Resources;
            m_Resources = null;
            if (resources != null)
            {
                resources.UnloadAllAssets();
                GameFramework.ReferencePool.Release(resources);
            }
        }

        private sealed class Work<T> : IWork
        {
            private readonly AsyncScope m_Scope;
            private readonly bool m_ObserveErrors;
            internal readonly OwnedCompletion<T> Completion = new OwnedCompletion<T>();
            public Work(AsyncScope scope, bool observeErrors) { m_Scope = scope; m_ObserveErrors = observeErrors; }
            public void Cancel() => Completion.Cancel(m_Scope.CancellationToken);

            internal async UniTaskVoid Observe(Func<AsyncContext, UniTask<T>> operation)
            {
                try
                {
                    T value = await operation(new AsyncContext(m_Scope));
                    m_Scope.m_Work.Remove(this);
                    if (m_Scope.IsActive) Completion.Succeed(value);
                    else Cancel();
                }
                catch (OperationCanceledException)
                {
                    m_Scope.m_Work.Remove(this);
                    Cancel();
                }
                catch (Exception e)
                {
                    m_Scope.m_Work.Remove(this);
                    if (m_ObserveErrors) m_Scope.Runtime.Report(e);
                    Completion.Fail(e);
                }
            }
        }
    }

    public readonly struct AsyncContext
    {
        public AsyncScope Scope { get; }
        public CancellationToken CancellationToken => Scope.CancellationToken;
        public bool IsActive => Scope != null && Scope.IsActive;
        public AsyncContext(AsyncScope scope) => Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        public bool Apply(Action action) => Scope.Apply(action);
        public void ThrowIfStopped()
        {
            if (!IsActive) throw new OperationCanceledException(CancellationToken);
        }
        public UniTask Delay(TimeSpan delay)
        {
            ThrowIfStopped();
            return UniTask.Delay(delay, cancellationToken: CancellationToken, cancelImmediately: true);
        }
        public UniTask<T> LoadAssetAsync<T>(string assetName) where T : UnityEngine.Object
        {
            ThrowIfStopped();
            return Scope.GetResources().LoadAssetAsync<T>(assetName);
        }
    }

    public static class AsyncOwnerExtensions
    {
        // Fire-and-forget is still observed by RunAsync's single internal observer.
        public static void Run(this IAsyncOwner owner, Func<AsyncContext, UniTask> operation) => owner.RunAsync(operation);
        public static UniTask<TResult> ReadAssetAsync<T, TResult>(this IAsyncOwner owner, string assetName, Func<T, TResult> read) where T : UnityEngine.Object
        {
            return owner.RunAsync(async context =>
            {
                ResourceContainer resources = context.Scope.GetResources();
                T asset = await resources.LoadAssetAsync<T>(assetName);
                try
                {
                    context.ThrowIfStopped();
                    TResult value = default;
                    context.Apply(() => value = read(asset));
                    return value;
                }
                finally { if (context.IsActive) resources.UnloadAsset(asset); }
            });
        }
        public static UniTask<T> RunAsync<T>(this IAsyncOwner owner, Func<AsyncContext, UniTask<T>> operation)
        {
            if (owner?.AsyncScope == null) throw new InvalidOperationException("An active async owner is required.");
            return owner.AsyncScope.RunAsync(operation);
        }
        public static UniTask RunAsync(this IAsyncOwner owner, Func<AsyncContext, UniTask> operation)
        {
            if (owner?.AsyncScope == null) throw new InvalidOperationException("An active async owner is required.");
            return owner.AsyncScope.RunAsync(operation);
        }
    }

    // Completion owns only the caller's immutable terminal result; cleanup is done by the operation.
    // Retained handles always point to this record, even when the caller never reads the result.
    internal sealed class OwnedCompletion<T> : IUniTaskSource<T>
    {
        private UniTaskStatus m_Status;
        private T m_Value;
        private ExceptionDispatchInfo m_Error;
        private CancellationToken m_Token;
        private Action<object> m_Continuation;
        private object m_State;
        public UniTask<T> Task => new UniTask<T>(this, 0);
        public UniTaskStatus GetStatus(short token) => m_Status;
        public UniTaskStatus UnsafeGetStatus() => m_Status;
        public bool Succeed(T value) { if (m_Status != UniTaskStatus.Pending) return false; m_Value = value; Finish(UniTaskStatus.Succeeded); return true; }
        public bool Fail(Exception e) { if (m_Status != UniTaskStatus.Pending) return false; m_Error = ExceptionDispatchInfo.Capture(e); Finish(UniTaskStatus.Faulted); return true; }
        public bool Cancel(CancellationToken token) { if (m_Status != UniTaskStatus.Pending) return false; m_Token = token; Finish(UniTaskStatus.Canceled); return true; }
        private void Finish(UniTaskStatus status)
        {
            m_Status = status;
            var continuation = m_Continuation;
            var state = m_State;
            m_Continuation = null;
            m_State = null;
            continuation?.Invoke(state);
        }
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (m_Status != UniTaskStatus.Pending) { continuation(state); return; }
            if (m_Continuation != null) throw new InvalidOperationException("Await this task once.");
            m_Continuation = continuation;
            m_State = state;
        }
        public T GetResult(short token)
        {
            if (m_Status == UniTaskStatus.Pending) throw new InvalidOperationException("The task is pending.");
            if (m_Status == UniTaskStatus.Canceled) throw new OperationCanceledException(m_Token);
            m_Error?.Throw();
            return m_Value;
        }
        void IUniTaskSource.GetResult(short token) => GetResult(token);
    }
}
