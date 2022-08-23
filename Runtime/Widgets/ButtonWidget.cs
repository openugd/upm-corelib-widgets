using System;
using UnityEngine.UI;

namespace OpenUGD.Core.Widgets
{
    public class ButtonWidget : Widget<Button, Action>
    {
        private Signal _onClick;

        protected override void OnViewAdded()
        {
            View.onClick.AddListener(ClickHandler);
            Lifetime.AddAction(() => { View.onClick.RemoveListener(ClickHandler); });
        }

        public void SubscribeOnClick(Lifetime lifetime, Action listener)
        {
            if (_onClick == null)
            {
                _onClick = new Signal(Lifetime);
            }

            _onClick.Subscribe(lifetime, listener);
        }

        private void ClickHandler()
        {
            Model?.Invoke();

            _onClick?.Fire();
        }
    }

    public static class ButtonWidgetExtensions
    {
        public static ButtonWidget AddButton(this Widget parent, Button view, Action listener)
        {
            var widget = new ButtonWidget();
            parent.AddWidget(widget);

            widget.SetView(view);
            widget.SetModel(listener);

            return widget;
        }
    }
}
