using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Game;
using NUnit.Framework;
using UnityEngine;

namespace CarzyCooker.Tests
{
    public sealed class OwnedMonoWidget : AUIWidget { }
    public sealed class OwnedMonoEntity : AEntity { }
    public sealed class OwnedMonoExWidget : AExUIWidget { }

    [Category("OwnedIntegration")]
    public sealed class OwnedMonoLifecycleTests
    {
        [Test] [Category("OwnedRemaining")]
        public void OwnedGFView_HideKeepsLogicalOwnerAndStartsNewViewRound()
        {
            var runtime = AsyncRuntime.Begin();
            var logicalOwner = new AsyncScope(runtime);
            var go = new GameObject("OwnedGFView", typeof(UnityGameFramework.Runtime.Entity), typeof(OwnedMonoEntity));
            var view = go.GetComponent<OwnedMonoEntity>();
            var gfEntity = go.GetComponent<UnityGameFramework.Runtime.Entity>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                typeof(AEntity).GetMethod("OnInit", flags).Invoke(view, new object[] { null });
                typeof(UnityGameFramework.Runtime.Entity).GetField("m_EntityLogic", flags).SetValue(gfEntity, view);
                typeof(AEntity).GetMethod("OnShow", flags).Invoke(view, new object[] { null });
                var task = view.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
                gfEntity.OnHide(false, null);
                Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
                Assert.That(logicalOwner.IsActive, Is.True);
                typeof(AEntity).GetMethod("OnShow", flags).Invoke(view, new object[] { null });
                Assert.That(view.AsyncScope.IsActive, Is.True);
            }
            finally { runtime.Stop(); UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test] [Category("OwnedRemaining")]
        public void OwnedBase_PreTeardownCancelsSynchronouslyAndOldBaseCannotStopNewRuntime()
        {
            var runtime = AsyncRuntime.Begin();
            var owner = new AsyncScope(runtime);
            var task = owner.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
            var root = new GameObject("OwnedInactiveBase");
            root.SetActive(false);
            var originalBase = root.AddComponent<UnityGameFramework.Runtime.BaseComponent>();
            originalBase.ShuttingDown += runtime.Stop;
            var notify = typeof(UnityGameFramework.Runtime.BaseComponent).GetMethod("NotifyShutdown", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                notify.Invoke(originalBase, null);
                Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
                Assert.That(originalBase.ModulesStopped, Is.True);
                var next = AsyncRuntime.Begin();
                var nextOwner = new AsyncScope(next);
                notify.Invoke(originalBase, null);
                Assert.That(nextOwner.IsActive, Is.True);
                next.Stop();
            }
            finally { runtime.Stop(); UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test] public void OwnedWidget_CloseReopenAndVisibilityKeepTheirActualContracts()
        {
            var runtime = AsyncRuntime.Begin();
            var go = new GameObject("OwnedWidget", typeof(RectTransform), typeof(OwnedMonoWidget));
            var widget = go.GetComponent<OwnedMonoWidget>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                typeof(AUIWidget).GetMethod("OnInit", flags).Invoke(widget, new object[] { null });
                widget.SetInitialized(true);
                typeof(AUIWidget).GetMethod("OnOpen", flags).Invoke(widget, new object[] { null });
                var old = widget.AsyncScope;
                var pending = new UniTaskCompletionSource<int>();
                AsyncContext oldContext = default;
                var result = widget.RunAsync(context => { oldContext = context; return pending.Task; });
                widget.Visible = false;
                Assert.That(old.IsActive, Is.True);
                typeof(AUIWidget).GetMethod("OnClose", flags).Invoke(widget, new object[] { false, null });
                Assert.That(result.Status, Is.EqualTo(UniTaskStatus.Canceled));
                typeof(AUIWidget).GetMethod("OnOpen", flags).Invoke(widget, new object[] { null });
                pending.TrySetResult(1);
                Assert.That(oldContext.Apply(() => Assert.Fail("old widget write")), Is.False);
                Assert.That(widget.AsyncScope.IsActive, Is.True);
                UnityEngine.Object.DestroyImmediate(go);
                Assert.That(widget == null, Is.True);
                runtime.Stop();
            }
            finally { runtime.Stop(); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test] public void OwnedDelay_StopIsImmediateAndDoesNotNeedPlayerLoop()
        {
            var runtime = new AsyncRuntime();
            var owner = new AsyncScope(runtime);
            var delay = owner.RunAsync(context => context.Delay(TimeSpan.FromHours(1)));
            owner.Stop();
            Assert.That(delay.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(owner.PendingCount, Is.Zero);
            runtime.Stop();
        }
    }
}
