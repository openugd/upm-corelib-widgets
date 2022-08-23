using System;
using TMPro;

namespace OpenUGD.Core.Widgets
{
    public class InputFieldWidget : Widget<TMP_InputField, string>
    {
        private Signal<string> _onValueChangedSignal;

        public string InputValue => View.text;

        protected override void OnViewAdded()
        {
            View.onValueChanged.AddListener(ValueChangedHandler);
            Lifetime.AddAction(() => { View.onValueChanged.RemoveListener(ValueChangedHandler); });

            Refresh();
        }

        protected override void OnAfterModelChanged() => Refresh();

        private void Refresh()
        {
            if (View != null)
            {
                if (Model != null)
                {
                    View.text = Model;
                }
                else
                {
                    View.text = "";
                }
            }
        }

        private void ValueChangedHandler(string value)
        {
            _onValueChangedSignal?.Fire(value);
        }

        public void SubscribeOnValueChanged(Lifetime lifetime, Action<string> listener)
        {
            if (_onValueChangedSignal == null)
            {
                _onValueChangedSignal = new Signal<string>(Lifetime);
            }

            _onValueChangedSignal.Subscribe(lifetime, listener);
        }
    }

    public static class InputFieldExtensions
    {
        public static InputFieldWidget AddInputField(this Widget parent, TMP_InputField view, string value)
        {
            var widget = new InputFieldWidget();
            parent.AddWidget(widget);

            widget.SetView(view);
            widget.SetModel(value);

            return widget;
        }

        public static InputFieldWidget AddInputField(this Widget parent, TMP_InputField view, string value,
            int maxLenght)
        {
            view.characterLimit = maxLenght;
            var widget = new InputFieldWidget();
            parent.AddWidget(widget);

            widget.SetView(view);
            widget.SetModel(value);

            return widget;
        }
    }
}
