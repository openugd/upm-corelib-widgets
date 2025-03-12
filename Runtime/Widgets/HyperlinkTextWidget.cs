using OpenUGD.UI;

namespace OpenUGD.Core.Widgets
{
    public class HyperlinkTextWidget : Widget<HyperlinkText, string>
    {
        protected override void OnViewAdded() => Refresh();

        protected override void OnAfterModelChanged() => Refresh();

        private void Refresh()
        {
            if (View != null)
            {
                if (Model != null)
                {
                    View.Text.text = Model;
                }
                else
                {
                    View.Text.text = "";
                }
            }
        }
    }

    public static class HyperlinkWidgetExtensions
    {
        public static HyperlinkTextWidget AddHyperlinkText(this Widget parent, HyperlinkText view, string text)
        {
            var value = text;
            if (parent is IResolve resolve)
            {
                var localization = resolve.Resolve<ILocalization>();
                value = localization.Get(text);
            }

            if (string.IsNullOrEmpty(value))
                value = text;

            var widget = new HyperlinkTextWidget();
            parent.AddWidget(widget);

            widget.SetView(view);
            widget.SetModel(value);

            return widget;
        }

        public static HyperlinkTextWidget AddText(this Widget parent, HyperlinkText view, string format,
            params object[] keys)
        {
            if (parent is IResolve resolve)
            {
                var localization = resolve.Resolve<ILocalization>();
                for (var i = 0; i < keys.Length; i++)
                {
                    if (keys[i] is string)
                    {
                        keys[i] = localization.Get((string)keys[i]);
                    }
                }
            }

            var value = string.Format(format, keys);

            var widget = new HyperlinkTextWidget();
            parent.AddWidget(widget);

            widget.SetView(view);
            widget.SetModel(value);

            return widget;
        }

        public static void UpdateText(this Widget parent, HyperlinkTextWidget textWidget, string format,
            params object[] keys)
        {
            if (parent is IResolve resolve)
            {
                var localization = resolve.Resolve<ILocalization>();
                for (var i = 0; i < keys.Length; i++)
                {
                    if (keys[i] is string)
                    {
                        keys[i] = localization.Get((string)keys[i]);
                    }
                }
            }

            var value = string.Format(format, keys);

            textWidget.SetModel(value);
        }

        public static void AddText(this Widget parent, HyperlinkTextWidget textWidget, string text)
        {
            var value = text;
            if (parent is IResolve resolve)
            {
                var localization = resolve.Resolve<ILocalization>();
                value = localization.Get(text);
            }

            if (string.IsNullOrEmpty(value))
                value = text;

            textWidget.SetModel(value);
        }
    }
}
