using System;
using Cysharp.Threading.Tasks;
using Game;
using NUnit.Framework;

namespace CarzyCooker.Tests
{
    public sealed class OwnedAsyncLifecycleTests
    {
        private AsyncRuntime m_Runtime;
        [SetUp] public void Setup() => m_Runtime = new AsyncRuntime { ErrorObserver = _ => { } };
        [TearDown] public void Cleanup() => m_Runtime.Stop();

        [Test] public void T1_CompletionRemovesRegistrationBeforeConsumption()
        {
            var scope = new AsyncScope(m_Runtime);
            var pending = new UniTaskCompletionSource<int>();
            var task = scope.RunAsync(_ => pending.Task);
            Assert.That(scope.PendingCount, Is.EqualTo(1));
            pending.TrySetResult(42);
            Assert.That(scope.PendingCount, Is.Zero);
            scope.Stop();
            Assert.That(task.GetAwaiter().GetResult(), Is.EqualTo(42));
        }

        [Test] public void T2_StopCancelsNeverCompletingWorkWithoutAnyFrame()
        {
            var scope = new AsyncScope(m_Runtime);
            var task = scope.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
            scope.Stop();
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(scope.PendingCount, Is.Zero);
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
        }

        [Test] public void T4_OldHandleAndContextCannotReachNewRound()
        {
            var old = new AsyncScope(m_Runtime);
            AsyncContext oldContext = default;
            var pending = new UniTaskCompletionSource<int>();
            var task = old.RunAsync(c => { oldContext = c; return pending.Task; });
            old.Stop();
            var current = new AsyncScope(m_Runtime);
            var next = current.RunAsync(_ => UniTask.FromResult(7));
            pending.TrySetResult(9);
            int writes = 0;
            Assert.That(oldContext.Apply(() => writes++), Is.False);
            Assert.That(writes, Is.Zero);
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(next.GetAwaiter().GetResult(), Is.EqualTo(7));
        }

        [Test] public void T5_UnconsumedFailureIsObservedOnce()
        {
            int failures = 0;
            m_Runtime.ErrorObserver = _ => failures++;
            var scope = new AsyncScope(m_Runtime);
            scope.RunAsync<int>(_ => UniTask.FromException<int>(new InvalidOperationException("actual failure")));
            Assert.That(failures, Is.EqualTo(1));
            Assert.That(scope.PendingCount, Is.Zero);
            var canceled = scope.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
            scope.Stop();
            Assert.That(canceled.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(failures, Is.EqualTo(1));
        }

        [Test] public void T6_CancellationReentryKeepsNewScopeAndTerminalFact()
        {
            var old = new AsyncScope(m_Runtime);
            var pending = old.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
            AsyncScope current = null;
            UniTask<int> next = default;
            pending.GetAwaiter().OnCompleted(() =>
            {
                old.Stop();
                current = new AsyncScope(m_Runtime);
                next = current.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
            });
            old.Stop();
            old.Stop();
            Assert.That(current, Is.Not.Null);
            Assert.That(current.PendingCount, Is.EqualTo(1));
            Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Canceled));
        }

        [Test] public void T6_CompletionContinuationMayStopItsOwner()
        {
            var scope = new AsyncScope(m_Runtime);
            var pending = new UniTaskCompletionSource<int>();
            var task = scope.RunAsync(_ => pending.Task);
            task.GetAwaiter().OnCompleted(scope.Stop);
            pending.TrySetResult(3);
            Assert.That(task.GetAwaiter().GetResult(), Is.EqualTo(3));
            Assert.That(scope.PendingCount, Is.Zero);
        }

        [Test] public void T7_OneOwnerStopLeavesOtherOwnerRunning()
        {
            var first = new AsyncScope(m_Runtime);
            var second = new AsyncScope(m_Runtime);
            var pending = new UniTaskCompletionSource<int>();
            var one = first.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
            var two = second.RunAsync(_ => pending.Task);
            first.Stop();
            pending.TrySetResult(5);
            Assert.That(one.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(two.GetAwaiter().GetResult(), Is.EqualTo(5));
        }

        [Test] public void T8_RuntimeInvalidatesEveryOwnerBeforeCancellationAndRejectsWork()
        {
            var first = new AsyncScope(m_Runtime);
            var second = new AsyncScope(m_Runtime);
            var one = first.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
            var two = second.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
            bool secondActive = true;
            one.GetAwaiter().OnCompleted(() => secondActive = second.IsActive);
            m_Runtime.Stop();
            Assert.That(secondActive, Is.False);
            Assert.That(one.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(two.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.Throws<ObjectDisposedException>(() => first.RunAsync(_ => UniTask.FromResult(1)));
            var newRun = new AsyncRuntime();
            var next = new AsyncScope(newRun);
            m_Runtime.Stop();
            Assert.That(next.IsActive, Is.True);
            newRun.Stop();
        }

        [Test] public void T9_IgnoredCancellationCannotApplyOrStartOwnerlessWork()
        {
            var scope = new AsyncScope(m_Runtime);
            AsyncContext context = default;
            var pending = new UniTaskCompletionSource<int>();
            var task = scope.RunAsync(c => { context = c; return pending.Task; });
            scope.Stop();
            Assert.That(context.Apply(() => Assert.Fail("stale write")), Is.False);
            Assert.Throws<InvalidOperationException>(() => ((IAsyncOwner)null).RunAsync(_ => UniTask.FromResult(1)));
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
        }
    }
}
