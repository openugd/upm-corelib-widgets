using OpenUGD.UI;

namespace OpenUGD.Core.Widgets
{
    public delegate void GestureDelegate(GestureWidget sender, Gesture gesture);

    public enum Gesture
    {
        Tap,
        Left,
        Right,
        Up,
        Down
    }

    public class GestureWidget : Widget<UIGestureDetector, GestureDelegate>
    {
        protected override void OnReady()
        {
            View.OnTap.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Tap); });

            View.OnSwipeLeft.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Left); });
            View.OnSwipeRight.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Right); });
            View.OnSwipeUp.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Up); });
            View.OnSwipeDown.Subscribe(Lifetime, () => { Model?.Invoke(this, Gesture.Down); });

            base.OnReady();
        }
    }

    public static class GestureWidgetExtensions
    {
        public static GestureWidget AddGesture(
            this Widget parent,
            UIGestureDetector view,
            GestureDelegate onGesture
        )
        {
            var widget = new GestureWidget();
            parent.AddWidget(widget);

            widget.SetView(view);
            widget.SetModel(onGesture);

            return widget;
        }
    }
}
