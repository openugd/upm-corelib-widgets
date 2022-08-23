using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Core.Widgets
{
    public class RawImageWidget : Widget<RawImage, Texture>
    {
        protected override void OnViewAdded() => Refresh();

        protected override void OnAfterModelChanged() => Refresh();

        private void Refresh()
        {
            if (View == null)
            {
                return;
            }

            if (Model == null)
            {
                View.enabled = false;
                return;
            }

            View.enabled = true;
            View.texture = Model;
        }
    }

    public static class RawImageWidgetExtensions
    {
        public static RawImageWidget AddRawImage(this Widget parent, RawImage view, Texture model = null)
        {
            var widget = parent.AddWidget(new RawImageWidget());
            widget.SetView(view);
            widget.SetModel(model);
            return widget;
        }

        public static void UpdateRawImage(this RawImageWidget widget, Texture model)
        {
            widget.SetModel(model);
        }
    }
}
