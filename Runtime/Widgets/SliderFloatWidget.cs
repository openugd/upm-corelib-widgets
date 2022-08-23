using System;
using UnityEngine.UI;

namespace OpenUGD.Core.Widgets
{
    public class SliderFloatWidget : Widget<Slider, float>, ISignal<float>
    {
        private Signal<float> _onChange;

        protected override void OnInitialize()
        {
            _onChange = new Signal<float>(Lifetime);
        }

        public bool Subscribe(Lifetime lifetime, Action<float> listener) => _onChange.Subscribe(lifetime, listener);

        protected override void OnAfterModelChanged()
        {
            if (View != null && !float.IsNaN(Model))
            {
                View.value = Model;
            }
        }

        protected override void OnReady()
        {
            if (!float.IsNaN(Model))
            {
                View.value = Model;
            }

            View.onValueChanged.AddListener(ValueChangedHandler);
            Lifetime.AddAction(() => { View.onValueChanged.RemoveListener(ValueChangedHandler); });
        }

        private void ValueChangedHandler(float newValue)
        {
            _onChange.Fire(newValue);
        }
    }

    public static class SliderWidgetExtensions
    {
        public static SliderFloatWidget AddFloatSlider(this Widget parent, Slider view, float value = float.NaN)
        {
            var widget = new SliderFloatWidget();
            parent.AddWidget(widget);

            widget.SetModel(value);
            widget.SetView(view);
            return widget;
        }

        public static SliderFloatWidget AddFloatSlider(this Widget parent, Slider view, float value,
            Action<float> onChange)
        {
            var widget = new SliderFloatWidget();
            parent.AddWidget(widget);

            widget.SetModel(value);
            widget.SetView(view);
            widget.Subscribe(widget.Lifetime, onChange);

            return widget;
        }

        public static SliderFloatWidget AddFloatSlider(this Widget parent, Slider view, Action<float> onChange)
        {
            var widget = new SliderFloatWidget();
            parent.AddWidget(widget);

            widget.SetModel(float.NaN);
            widget.SetView(view);
            widget.Subscribe(widget.Lifetime, onChange);

            return widget;
        }
    }
}
