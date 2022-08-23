using System;
using UnityEngine.UI;

namespace OpenUGD.Core.Widgets
{
    public class SliderIntWidgetModel
    {
        public Action<float> OnValueChanged { get; }
        public long MinValue { get; }
        public long MaxValue { get; }
        public long Value { get; }

        public SliderIntWidgetModel(long minValue, long maxValue, long value, Action<float> onValueChanged = null)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Value = value;
            OnValueChanged = onValueChanged;
        }
    }

    public class SliderIntWidget : Widget<Slider, SliderIntWidgetModel>
    {
        protected override void OnViewAdded()
        {
            View.onValueChanged.AddListener(OnValueChanged);
            Lifetime.AddAction(() => { View.onValueChanged.RemoveListener(OnValueChanged); });
        }

        protected override void OnReady()
        {
            View.minValue = Model.MinValue;
            View.maxValue = Model.MaxValue;
            View.value = Model.Value;

            base.OnReady();
        }

        protected override void OnAfterModelChanged()
        {
            base.OnAfterModelChanged();

            if (View != null)
            {
                View.value = Model.Value;
            }
        }

        private void OnValueChanged(float value)
        {
            Model.OnValueChanged?.Invoke(value);
        }
    }

    public static class SliderIntWidgetExtensions
    {
        public static SliderIntWidget AddSliderInt(this Widget parent, Slider view, SliderIntWidgetModel model)
        {
            var widget = new SliderIntWidget();
            parent.AddWidget(widget);

            widget.SetView(view);
            widget.SetModel(model);

            return widget;
        }

        public static void UpdateSliderInt(this SliderIntWidget widget, SliderIntWidgetModel model)
        {
            widget.SetModel(model);
        }
    }
}
