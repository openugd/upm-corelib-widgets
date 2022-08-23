using System;
using UnityEngine.UI;

namespace OpenUGD.Core.Widgets
{
    public class ToggleWidgetModel
    {
        public Action<bool> OnChanged { get; }
        public bool Value { get; }

        public ToggleWidgetModel(Action<bool> onChanged, bool value = false)
        {
            OnChanged = onChanged;
            Value = value;
        }
    }

    public class ToggleWidget : Widget<Toggle, ToggleWidgetModel>
    {
        private Signal<bool> _onClick;

        protected override void OnViewAdded()
        {
            View.onValueChanged.AddListener(ClickHandler);
            Lifetime.AddAction(() => { View.onValueChanged.RemoveListener(ClickHandler); });
        }

        protected override void OnReady()
        {
            View.SetIsOnWithoutNotify(Model.Value);
            base.OnReady();
        }

        public void SubscribeOnClick(Lifetime lifetime, Action<bool> listener)
        {
            if (_onClick == null)
            {
                _onClick = new Signal<bool>(Lifetime);
            }

            _onClick.Subscribe(lifetime, listener);
        }

        private void ClickHandler(bool state)
        {
            Model.OnChanged?.Invoke(state);

            _onClick?.Fire(state);
        }
    }

    public static class ToggleWidgetExtensions
    {
        public static ToggleWidget AddToggle(this Widget parent, Toggle view, ToggleWidgetModel model)
        {
            var widget = new ToggleWidget();
            parent.AddWidget(widget);

            widget.SetView(view);
            widget.SetModel(model);

            return widget;
        }

        public static void RegisterToggleInGroup(this ToggleWidget toggle, ToggleGroup group)
        {
            if (toggle.View == null || group == null)
            {
                return;
            }

            group.RegisterToggle(toggle.View);
            toggle.View.group = group;
        }
    }
}
