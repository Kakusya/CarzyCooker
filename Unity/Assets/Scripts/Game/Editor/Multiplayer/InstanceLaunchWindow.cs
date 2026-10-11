using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Extension.Editor;

namespace Game.Editor
{
    public sealed class InstanceLaunchWindow : EditorWindow
    {
        private string m_RunId = "local-run";
        private string m_InstanceId = "development";
        private string m_Role = "host";
        private string m_Listen = "127.0.0.1:0";
        private string m_Connect = "";
        private string m_Error;

        [MenuItem("Game/Multiplayer/Instance Launch")]
        private static void Open()
        {
            GetWindow<InstanceLaunchWindow>("Instance Launch");
        }

        private void OnGUI()
        {
            m_RunId = EditorGUILayout.TextField("runId", m_RunId);
            m_InstanceId = EditorGUILayout.TextField("instanceId", m_InstanceId);
            string[] roles = { "solo", "host", "client", "legacy" };
            m_Role = roles[EditorGUILayout.Popup("role", Math.Max(0, Array.IndexOf(roles, m_Role)), roles)];
            if (m_Role == "host") m_Listen = EditorGUILayout.TextField("listen (host)", m_Listen);
            if (m_Role == "client") m_Connect = EditorGUILayout.TextField("connect (client)", m_Connect);
            EditorGUILayout.LabelField("Config", RuntimeInstanceContext.State.ToString());
            EditorGUILayout.LabelField("Product", RuntimeInstanceContext.ProductStage ?? "未启动");
            if (GUILayout.Button("提交并启动 Launcher"))
            {
                try
                {
                    RuntimeInstanceConfig config = RuntimeInstanceConfig.Create(m_RunId, m_InstanceId, m_Role,
                        m_Role == "host" ? m_Listen : null, m_Role == "client" ? m_Connect : null);
                    if (!CanLaunch(out string reason))
                    {
                        throw new InvalidOperationException(reason);
                    }
                    BuildSceneSetting.AllScenes();
                    SceneHelper.StartScene(EntryUtility.LauncherScenePath, config);
                    m_Error = null;
                }
                catch (Exception e)
                {
                    m_Error = e.Message;
                }
            }
            if (!string.IsNullOrEmpty(m_Error))
            {
                EditorGUILayout.HelpBox(m_Error, MessageType.Error);
            }
            if (!string.IsNullOrEmpty(RuntimeInstanceContext.Failure))
            {
                EditorGUILayout.HelpBox(RuntimeInstanceContext.Failure, MessageType.Error);
            }
        }

        public static bool CanLaunch(out string reason, bool requireSceneReload = true)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                reason = "EditorBusy: Play、编译或导入中。";
                return false;
            }
            if (requireSceneReload && EditorSettings.enterPlayModeOptionsEnabled &&
                (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableSceneReload) != 0)
            {
                reason = "SceneReloadDisabled: 请保留 Scene Reload。";
                return false;
            }
            // UTF 1.1.33 没有公开 active API，沿用现有 AutoTesting 的只读注册表查询。
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "UnityEditor.TestRunner");
            var holderType = assembly?.GetType("UnityEditor.TestTools.TestRunner.TestRun.TestJobDataHolder");
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy;
            var holder = holderType?.GetProperty("instance", flags)?.GetValue(null);
            var runs = holderType?.GetField("TestRuns")?.GetValue(holder) as IEnumerable;
            if (runs == null)
            {
                reason = "TestStateUnavailable: 无法核对 UTF 当前状态。";
                return false;
            }
            foreach (object run in runs)
            {
                var field = run.GetType().GetField("isRunning");
                if (field == null || (bool)field.GetValue(run))
                {
                    reason = "EditorBusy: UTF 测试正在运行或状态不可用。";
                    return false;
                }
            }
            reason = null;
            return true;
        }
    }

    [InitializeOnLoad]
    internal static class InstanceLaunchLifecycle
    {
        static InstanceLaunchLifecycle()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += OnReload;
            if (SessionState.GetBool(RuntimeInstanceRequest.Key(RuntimeInstanceRequest.ProjectPath, "Interrupted"), false))
            {
                RuntimeInstanceContext.Fail("PlayDomainReloadInterrupted");
                UnityGameFramework.Runtime.Log.Warning("显式实例运行因 Play 中编译重载而中断，请结束本轮后重新提交。 ");
                EditorApplication.delayCall += StopInterruptedPlay;
            }
        }

        private static void OnReload()
        {
            if (EditorApplication.isPlaying && SessionState.GetBool(RuntimeInstanceRequest.Key(RuntimeInstanceRequest.ProjectPath, "Active"), false))
            {
                SessionState.SetBool(RuntimeInstanceRequest.Key(RuntimeInstanceRequest.ProjectPath, "Interrupted"), true);
                RuntimeInstanceContext.Fail("PlayDomainReloadInterrupted");
                // UGF 壳没有 Play 编译后的静态 owner 恢复；只结束已提交的本轮实例。
                if (GameEntry.Procedure != null && GameEntry.Procedure.CurrentProcedure is ProcedureET procedure)
                {
                    procedure.StopOwnedEntry();
                }
                if (GameEntry.Base != null)
                {
                    GameEntry.Base.gameObject.SetActive(false);
                }
                EditorApplication.isPlaying = false;
            }
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SessionState.EraseBool(RuntimeInstanceRequest.Key(RuntimeInstanceRequest.ProjectPath, "Interrupted"));
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SceneHelper.Clear(RuntimeInstanceRequest.ProjectPath);
                RuntimeInstanceRequest.Clear(RuntimeInstanceRequest.ProjectPath, true);
                RuntimeInstanceContext.Stop();
            }
        }

        private static void StopInterruptedPlay()
        {
            if (EditorApplication.isPlaying && SessionState.GetBool(RuntimeInstanceRequest.Key(RuntimeInstanceRequest.ProjectPath, "Interrupted"), false))
            {
                EditorApplication.isPlaying = false;
            }
        }
    }
}
