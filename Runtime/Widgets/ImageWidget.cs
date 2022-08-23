using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Core.Widgets
{
    public class ImageWidget : Widget<Image, Sprite>
    {
        protected override void OnReady() => View.sprite = Model;
    }

    public static class ImageWidgetExtensions
    {
        public static ImageWidget AddImage(this Widget parent, Image view, Sprite model)
        {
            var widget = parent.AddWidget(new ImageWidget());
            widget.SetView(view);
            widget.SetModel(model);
            return widget;
        }
    }
}
