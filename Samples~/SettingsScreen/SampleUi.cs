using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenUGD.Samples.SettingsScreen
{
    // Builds uGUI controls in code, so the sample needs no scene or prefab. Nothing here is specific to the widgets
    // presenters: in a project these controls come from a prefab.
    internal static class SampleUi
    {
        public static readonly Color Panel = new Color(0.13f, 0.15f, 0.18f, 0.96f);
        public static readonly Color Accent = new Color(0.31f, 0.76f, 0.97f);
        public static readonly Color Track = new Color(0.32f, 0.35f, 0.40f);

        private static Font _font;

        private static Font Font => _font != null ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform CreateCanvas(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            EnsureEventSystem();
            return (RectTransform)go.transform;
        }

        // A centred panel that stacks its children from the top.
        public static RectTransform CreateColumn(RectTransform parent, Vector2 size)
        {
            var rect = Rect("Panel", parent);
            rect.sizeDelta = size;
            rect.gameObject.AddComponent<Image>().color = Panel;
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        public static Text CreateLabel(RectTransform parent, int size = 20, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var rect = Rect("Label", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            Height(rect, size + 12f);
            return text;
        }

        public static Button CreateButton(RectTransform parent, out Text label)
        {
            var go = DefaultControls.CreateButton(default);
            Attach(go, parent, 40f);
            label = go.GetComponentInChildren<Text>();
            label.font = Font;
            label.fontSize = 20;
            return go.GetComponent<Button>();
        }

        public static Toggle CreateToggle(RectTransform parent, out Text label)
        {
            var go = DefaultControls.CreateToggle(default);
            Attach(go, parent, 28f);
            var toggle = go.GetComponent<Toggle>();
            toggle.graphic.color = Accent;
            label = go.GetComponentInChildren<Text>();
            label.font = Font;
            label.fontSize = 20;
            label.color = Color.white;
            return toggle;
        }

        public static Slider CreateSlider(RectTransform parent)
        {
            var go = DefaultControls.CreateSlider(default);
            Attach(go, parent, 24f);
            var slider = go.GetComponent<Slider>();
            go.transform.Find("Background").GetComponent<Image>().color = Track;
            slider.fillRect.GetComponent<Image>().color = Accent;
            return slider;
        }

        // TextMeshPro draws only with its Essential Resources imported (Window > TextMeshPro > Import TMP Essential
        // Resources); without them the field still works but shows no text.
        public static TMP_InputField CreateInputField(RectTransform parent)
        {
            var go = TMP_DefaultControls.CreateInputField(default);
            Attach(go, parent, 40f);
            var field = go.GetComponent<TMP_InputField>();
            field.pointSize = 20f;
            return field;
        }

        public static Image CreateImage(RectTransform parent, float height)
        {
            var rect = Rect("Image", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            Height(rect, height);
            return image;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && OPENUGD_INPUT_SYSTEM_PACKAGE
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#else
            Debug.LogWarning("No input module for the EventSystem: install the Input System package, or enable the " +
                             "Input Manager in Player Settings > Active Input Handling.", go);
#endif
        }

        private static RectTransform Rect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Attach(GameObject go, RectTransform parent, float height)
        {
            go.transform.SetParent(parent, false);
            Height((RectTransform)go.transform, height);
        }

        private static void Height(RectTransform rect, float height)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
        }
    }
}
