using UnityEngine;

namespace OpenUGD.Core.Widgets
{
    public class ResourcePrefabWidget : Widget<Transform, string>
    {
        private GameObject _instance;

        protected override void OnBeforeModelChange() => Remove();

        protected override void OnAfterModelChanged() => Update();

        protected override void OnViewAdded() => Update();

        protected override void OnViewAfterRemoved() => Remove();

        private void Remove()
        {
            if (_instance != null)
            {
                GameObject.Destroy(_instance);
                _instance = null;
            }
        }

        private void Update()
        {
            if (View != null && Model != null)
            {
                var prefab = Resources.Load<GameObject>(Model);
                _instance = GameObject.Instantiate(prefab, View, false);
            }
        }

        protected override void OnInitialize() => Lifetime.AddAction(Remove);
    }

    public static class ResourcePrefabWidgetExtensions
    {
        public static ResourcePrefabWidget AddResourcePrefab(this Widget parent, Transform parentTransform,
            string resourcePath)
        {
            var widget = parent.AddWidget(new ResourcePrefabWidget());
            widget.SetView(parentTransform);
            widget.SetModel(resourcePath);
            return widget;
        }
    }
}
