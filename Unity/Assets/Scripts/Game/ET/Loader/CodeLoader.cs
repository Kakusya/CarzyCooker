using System.Reflection;
using Cysharp.Threading.Tasks;
using System;
using Game;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Extension;

namespace ET
{
    public class CodeLoader : ICodeLoader
    {
        private Assembly m_Model;
        private Assembly m_ModelView;
        private Init m_Owner;
        private World m_World;

        private void CheckOwner()
        {
            if (m_Owner == null || !m_Owner.IsCurrent(m_World))
            {
                throw new OperationCanceledException("ET code load owner stopped");
            }
        }

        public async UniTask StartAsync()
        {
            m_Owner = Init.Instance;
            m_World = m_Owner.OwnedWorld;
            CheckOwner();
            m_Model = null;
            m_ModelView = null;

            if (Define.EnableHotfix && GameEntry.CodeRunner.EnableCodeBytesMode)
            {
                // 使用Unity编辑器的Model dll，可以让ModelView中的Monobehaviour正常使用
                if (Define.IsEditor && Define.UseUnityEditorModelDll)
                {
                    Assembly[] assemblies = Utility.Assembly.GetAssemblies();
                    foreach (Assembly ass in assemblies)
                    {
                        string name = ass.GetName().Name;
                        if (name == "Game.ET.Code.Model")
                        {
                            m_Model = ass;
                        }
                        else if (name == "Game.ET.Code.ModelView")
                        {
                            m_ModelView = ass;
                        }

                        if (m_Model != null && m_ModelView != null)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    byte[] assBytesModel = await LoadCodeBytesAsync("Game.ET.Code.Model.dll.bytes");
                    byte[] pdbBytesModel = await LoadCodeBytesAsync("Game.ET.Code.Model.pdb.bytes");
                    byte[] assBytesModelView = await LoadCodeBytesAsync("Game.ET.Code.ModelView.dll.bytes");
                    byte[] pdbBytesModelView = await LoadCodeBytesAsync("Game.ET.Code.ModelView.pdb.bytes");
                    m_Model = Assembly.Load(assBytesModel, pdbBytesModel);
                    m_ModelView = Assembly.Load(assBytesModelView, pdbBytesModelView);
                }
                var hotfixAssemblies = await LoadHotfixAsync();
                CheckOwner();
                m_World.AddSingleton<CodeTypes, Assembly[]>(new []
                    {typeof (World).Assembly, typeof(Init).Assembly, m_Model, m_ModelView, hotfixAssemblies.Item1, hotfixAssemblies.Item2});
            }
            else
            {
                Assembly[] assemblies = Utility.Assembly.GetAssemblies();
                Assembly hotfix = null;
                Assembly hotfixView = null;
                foreach (Assembly ass in assemblies)
                {
                    string name = ass.GetName().Name;
                    if (name == "Game.ET.Code.Model")
                    {
                        m_Model = ass;
                    }
                    else if (name == "Game.ET.Code.ModelView")
                    {
                        m_ModelView = ass;
                    }
                    else if (name == "Game.ET.Code.Hotfix")
                    {
                        hotfix = ass;
                    }
                    else if (name == "Game.ET.Code.HotfixView")
                    {
                        hotfixView = ass;
                    }

                    if (m_Model != null && m_ModelView != null && hotfix != null && hotfixView != null)
                    {
                        break;
                    }
                }
                CheckOwner();
                m_World.AddSingleton<CodeTypes, Assembly[]>(new []
                    {typeof (World).Assembly, typeof(Init).Assembly, m_Model, m_ModelView, hotfix, hotfixView});
            }
            
            CheckOwner();
            IStaticMethod start = new StaticMethod(m_Model, "ET.Entry", "Start");
            start.Run();
        }

        public async UniTask ReloadAsync()
        {
            var hotfixAssemblies = await LoadHotfixAsync();
            CheckOwner();
            m_World.AddSingleton<CodeTypes, Assembly[]>(new[]
                { typeof(World).Assembly, typeof(Init).Assembly, m_Model, m_ModelView, hotfixAssemblies.Item1, hotfixAssemblies.Item2 });
            CodeTypes.Instance.CreateCode();
        }

        private async UniTask<(Assembly, Assembly)> LoadHotfixAsync()
        {
            if (!Define.EnableHotfix)
            {
                throw new GameFrameworkException("Client ET LoadHotfix only run when EnableHotfix!");
            }
            byte[] assBytesHotfix = await LoadCodeBytesAsync("Game.ET.Code.Hotfix.dll.bytes");
            byte[] pdbBytesHotfix = await LoadCodeBytesAsync("Game.ET.Code.Hotfix.pdb.bytes");
            byte[] assBytesHotfixView = await LoadCodeBytesAsync("Game.ET.Code.HotfixView.dll.bytes");
            byte[] pdbBytesHotfixView = await LoadCodeBytesAsync("Game.ET.Code.HotfixView.pdb.bytes");
            Assembly hotfix = Assembly.Load(assBytesHotfix, pdbBytesHotfix);
            Assembly hotfixView = Assembly.Load(assBytesHotfixView, pdbBytesHotfixView);
            return (hotfix, hotfixView);
        }

        private async UniTask<byte[]> LoadCodeBytesAsync(string fileName)
        {
            CheckOwner();
            string assetName = AssetUtility.GetETAsset(Utility.Text.Format("Code/{0}", fileName));
            byte[] bytes = await m_Owner.ReadAssetAsync<TextAsset, byte[]>(assetName, asset => asset.bytes);
            CheckOwner();
            return bytes;
        }
    }
}
