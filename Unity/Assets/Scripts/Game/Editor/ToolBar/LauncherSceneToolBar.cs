using ToolbarExtension;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameFramework.Extension.Editor;

namespace Game.Editor
{
    internal sealed class LauncherSceneToolBar
    {
        private static readonly GUIContent m_ButtonGUIContent = new GUIContent("Launcher", "Start Run Launcher Scene.");

        [Toolbar(OnGUISide.Left, 100)]
        static void OnToolbarGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            {
                if (GUILayout.Button(m_ButtonGUIContent))
                {
                    BuildSceneSetting.AllScenes();
                    SceneHelper.StartScene(EntryUtility.LauncherScenePath);
                }
            }
            EditorGUI.EndDisabledGroup();
        }
    }

    public static class SceneHelper
    {
        public static void Submit(string projectPath, string scenePath)
        {
            SessionState.SetString(RuntimeInstanceRequest.Key(projectPath, "Scene"), scenePath);
        }

        public static string Read(string projectPath)
        {
            return SessionState.GetString(RuntimeInstanceRequest.Key(projectPath, "Scene"), "");
        }

        public static void Clear(string projectPath)
        {
            SessionState.EraseString(RuntimeInstanceRequest.Key(projectPath, "Scene"));
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            string scenePath = Read(RuntimeInstanceRequest.ProjectPath);
            if (!string.IsNullOrEmpty(scenePath))
            {
                if (!SceneManager.GetActiveScene().path.Equals(scenePath))
                {
                    SceneManager.LoadScene(scenePath);
                }
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            Clear(RuntimeInstanceRequest.ProjectPath);
        }

        public static void StartScene(string scenePathName, RuntimeInstanceConfig config = null)
        {
            if (!InstanceLaunchWindow.CanLaunch(out string reason, config != null))
            {
                UnityGameFramework.Runtime.Log.Warning(reason);
                return;
            }
            RuntimeInstanceRequest.Clear(RuntimeInstanceRequest.ProjectPath);
            if (config != null)
            {
                RuntimeInstanceRequest.Submit(config);
            }
            Submit(RuntimeInstanceRequest.ProjectPath, scenePathName);
            EditorApplication.isPlaying = true;
        }
    }
}
