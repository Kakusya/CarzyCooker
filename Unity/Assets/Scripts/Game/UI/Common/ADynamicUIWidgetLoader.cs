using Cysharp.Threading.Tasks;
using Game;
using GameFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Extension;

namespace ET
{
    [RequireComponent(typeof(RectTransform))]
    public abstract partial class ADynamicUIWidgetLoader : MonoBehaviour
    {
        [SerializeField, ReadOnly]
        private string m_UIWidgetAssetPath;

        private AUIWidget m_UIWidget;
        private AUIWidget m_ParentWidget;
        private AExUIForm m_ParentForm;
        private AsyncScope m_Scope;

        /// <summary>
        /// UIWidget资源路径
        /// </summary>
        public string UIWidgetAssetPath => m_UIWidgetAssetPath;

        /// <summary>
        /// 动态加载的UIWidget实例
        /// </summary>
        public AUIWidget UIWidget => m_UIWidget;

        public bool IsLoaded => m_UIWidget != null;

        private void Start()
        {
            m_ParentWidget = GetComponentInParent<AUIWidget>(true);
            m_ParentForm = GetComponentInParent<AExUIForm>(true);
            if (m_ParentWidget != null) m_ParentWidget.AsyncScopeOpened += BeginLoad;
            else if (m_ParentForm != null) m_ParentForm.AsyncScopeOpened += BeginLoad;
            else throw new GameFrameworkException("Dynamic UIWidget requires an async UI owner.");
            BeginLoad(m_ParentWidget != null ? m_ParentWidget.AsyncScope : m_ParentForm.AsyncScope);
        }

        private void BeginLoad(AsyncScope parent)
        {
            if (parent == null || !parent.IsActive || m_Scope != null) return;
            var scope = new AsyncScope(parent.Runtime, () => this != null, parent);
            m_Scope = scope;
            AUIWidget created = null;
            scope.Stopped += () =>
            {
                if (ReferenceEquals(m_Scope, scope)) m_Scope = null;
                if (created != null)
                {
                    OnUIWidgetUnloading(created);
                    Destroy(created.gameObject);
                }
                if (m_UIWidget == created) m_UIWidget = null;
            };
            scope.RunAsync(async context =>
            {
                GameObject go = await context.LoadAssetAsync<GameObject>(m_UIWidgetAssetPath);
                context.Apply(() => created = CreateUIWidget(go));
            });
        }

        private void OnDestroy()
        {
            if (m_ParentWidget != null) m_ParentWidget.AsyncScopeOpened -= BeginLoad;
            else if (m_ParentForm != null) m_ParentForm.AsyncScopeOpened -= BeginLoad;
            var old = m_Scope;
            m_Scope = null;
            old?.Stop();
        }

        protected virtual void OnUIWidgetUnloading(AUIWidget widget)
        {
            if (widget is AExUIWidget owned)
            {
                owned.TryClose();
                if (owned.Has()) owned.Remove();
            }
        }

        private AUIWidget CreateUIWidget(GameObject go)
        {
            GameObject uiWidgetInstance = Instantiate(go, transform);
            AUIWidget uiWidget = uiWidgetInstance.GetComponent<AUIWidget>();
            if (uiWidget == null)
            {
                Destroy(uiWidgetInstance);
                throw new GameFrameworkException(Utility.Text.Format("DynamicMonoUIWidget: Loaded asset at path {0} does not contain an AUIWidget component.", m_UIWidgetAssetPath));
            }
            RectTransform uiWidgetRectTransform = uiWidgetInstance.GetComponent<RectTransform>();
            uiWidgetRectTransform.localRotation = Quaternion.identity;
            uiWidgetRectTransform.localPosition = Vector3.zero;
            uiWidgetRectTransform.localScale = Vector3.one;
            uiWidgetRectTransform.anchorMin = Vector2.zero;
            uiWidgetRectTransform.anchorMax = Vector2.one;
            uiWidgetRectTransform.pivot = new Vector2(0.5f, 0.5f);
            uiWidgetRectTransform.anchoredPosition3D = Vector3.zero;
            uiWidgetRectTransform.sizeDelta = Vector2.zero;

            m_UIWidget = uiWidget;
            OnUIWidgetLoaded(uiWidget);
            return uiWidget;
        }

        protected abstract void OnUIWidgetLoaded(AUIWidget uiWidget);
    }
}
