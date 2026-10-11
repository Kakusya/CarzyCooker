using System.IO;
using System.Text;
using ToolbarExtension;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal sealed class GitBranchToolBar
    {
        private const string GitDirPrefix = "gitdir:";
        private const string RefPrefix = "ref: refs/heads/";

        private static readonly GUIContent s_BranchLabelGUIContent = new GUIContent();
        private static readonly GUIStyle s_BranchLabelStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 13,
        };
        private static bool s_Resolved;

        [Toolbar(OnGUISide.Right, 100)]
        private static void OnToolbarGUI()
        {
            if (!s_Resolved)
            {
                s_Resolved = true;
                ResolveBranchLabel();
            }
            GUILayout.Label(s_BranchLabelGUIContent, s_BranchLabelStyle);
        }

        private static void ResolveBranchLabel()
        {
            s_BranchLabelGUIContent.text = "-";
            s_BranchLabelGUIContent.tooltip = "Git info unavailable";
            try
            {
                string repoRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
                string gitPath = Path.Combine(repoRoot, ".git");

                string headPath;
                if (Directory.Exists(gitPath))
                {
                    headPath = Path.Combine(gitPath, "HEAD");
                }
                else if (File.Exists(gitPath))
                {
                    // worktree：.git 文件指向该工作区的 Git 元数据目录。
                    string gitDir = ReadFirstLine(gitPath);
                    if (gitDir == null || !gitDir.StartsWith(GitDirPrefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    gitDir = gitDir.Substring(GitDirPrefix.Length).Trim();
                    if (string.IsNullOrEmpty(gitDir))
                    {
                        return;
                    }

                    if (!Path.IsPathRooted(gitDir))
                    {
                        gitDir = Path.GetFullPath(Path.Combine(repoRoot, gitDir));
                    }
                    headPath = Path.Combine(gitDir, "HEAD");
                }
                else
                {
                    return;
                }

                string head = ReadFirstLine(headPath)?.Trim();
                if (string.IsNullOrEmpty(head))
                {
                    return;
                }

                if (head.StartsWith(RefPrefix, System.StringComparison.Ordinal))
                {
                    s_BranchLabelGUIContent.text = head.Substring(RefPrefix.Length);
                    s_BranchLabelGUIContent.tooltip = head;
                }
                else
                {
                    // detached HEAD：显示短提交号。
                    s_BranchLabelGUIContent.text = head.Length >= 7 ? head.Substring(0, 7) : head;
                    s_BranchLabelGUIContent.tooltip = "Detached HEAD";
                }
            }
            catch
            {
                // Git 信息为可选显示；读取失败保留占位符，不阻断工具栏绘制。
            }
        }

        private static string ReadFirstLine(string path)
        {
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                return reader.ReadLine();
            }
        }
    }
}
