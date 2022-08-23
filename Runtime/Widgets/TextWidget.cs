using UnityEngine.UI;

namespace OpenUGD.Core.Widgets
{
    public class TextWidget : Widget<Text, TextModel>
    {
        protected override void OnInitialize()
        {
            var localizationChanged = this.Resolve<ILocalizationChanged>();
            if (localizationChanged != null)
            {
                localizationChanged.Subscribe(Lifetime, () => {
                    if (Model != null)
                    {
                        SetModel(Model);
                    }
                });
            }

            base.OnInitialize();
        }

        protected override void OnViewAdded() => Refresh();

        protected override void OnAfterModelChanged() => Refresh();

        private void Refresh()
        {
            if (View != null)
            {
                if (Model != null)
                {
                    var value = (string)Model;
                    var localization = this.Resolve<ILocalization>();
                    if (localization != null)
                    {
                        var keys = Model.Keys;
                        if (keys != null)
                        {
                            var values = new object[keys.Length];
                            for (var i = 0; i < keys.Length; i++)
                            {
                                if (keys[i] is string)
                                {
                                    values[i] = localization.Get((string)keys[i]);
                                }
                                else
                                {
                                    values[i] = keys[i];
                                }
                            }

                            value = localization.Get(Model.Format);
                            value = string.Format(value, values);
                        }
                        else
                        {
                            value = localization.Get(Model);
                        }
                    }

                    View.text = value;
                }
                else
                {
                    View.text = "";
                }
            }
        }
    }

    public static class TextWidgetExtensions
    {
        public static TextWidget AddText(this Widget parent, Text view, string text)
        {
            var widget = parent.AddWidget(new TextWidget());

            widget.SetView(view);
            widget.SetModel(text);

            return widget;
        }

        public static TextWidget AddText(this Widget parent, Text view, string format, params object[] keys)
        {
            var widget = new TextWidget();
            parent.AddWidget(widget);

            widget.SetView(view);
            widget.SetModel(new TextModel {
                Format = format,
                Keys = keys
            });

            return widget;
        }
    }
}
