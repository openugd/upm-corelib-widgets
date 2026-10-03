using OpenUGD.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenUGD.Samples.GesturesAndLinks
{
    /// <summary>
    /// The sample's controls, built in code by <see cref="Create"/>.
    /// </summary>
    public sealed class GesturesAndLinksView
    {
        /// <summary>The gesture area: a translucent image with a detector on it.</summary>
        public UIGestureDetector Area { get; private set; }

        /// <summary>The text inside the gesture area.</summary>
        public Text AreaLabel { get; private set; }

        /// <summary>What the area last reported.</summary>
        public Text GestureStatus { get; private set; }

        /// <summary>A panel over the screen, inactive until a link opens it; it has a detector of its own.</summary>
        public UIGestureDetector Panel { get; private set; }

        /// <summary>The text on the panel.</summary>
        public Text PanelLabel { get; private set; }

        /// <summary>A TextMeshPro label with links.</summary>
        public HyperlinkText Links { get; private set; }

        /// <summary>What happened to the last link clicked.</summary>
        public Text LinkStatus { get; private set; }

        /// <summary>
        /// Builds the screen on a new canvas under <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">The canvas's parent; destroying it destroys the screen.</param>
        /// <returns>The view.</returns>
        public static GesturesAndLinksView Create(Transform parent)
        {
            var canvas = SampleUi.CreateCanvas(parent, "Gestures and Links");
            var view = new GesturesAndLinksView();

            // An Image is a raycast target, which is all a detector needs to receive pointer events.
            var area = SampleUi.CreateRect("Gesture Area", canvas, new Vector2(0f, 110f), new Vector2(720f, 300f));
            area.gameObject.AddComponent<Image>().color = new Color(0.31f, 0.76f, 0.97f, 0.25f);
            view.Area = area.gameObject.AddComponent<UIGestureDetector>();
            view.AreaLabel = SampleUi.CreateLabel(area, Vector2.zero, new Vector2(700f, 60f), 28, TextAnchor.MiddleCenter);
            view.GestureStatus = SampleUi.CreateLabel(canvas, new Vector2(0f, -70f), new Vector2(900f, 40f), 20,
                TextAnchor.MiddleCenter);

            var links = SampleUi.CreateRect("Links", canvas, new Vector2(0f, -170f), new Vector2(900f, 80f));
            var text = links.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            // HyperlinkText.Text is left empty: it is filled in with this TMP_Text on the first render or click.
            view.Links = links.gameObject.AddComponent<HyperlinkText>();
            view.LinkStatus = SampleUi.CreateLabel(canvas, new Vector2(0f, -250f), new Vector2(900f, 40f), 20,
                TextAnchor.MiddleCenter);

            // Inactive before any component is added, so it has never been awake when the presenter subscribes.
            var panelObject = new GameObject("Panel", typeof(RectTransform));
            panelObject.SetActive(false);
            var panel = (RectTransform)panelObject.transform;
            panel.SetParent(canvas, false);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.sizeDelta = Vector2.zero;
            panelObject.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.94f);
            view.Panel = panelObject.AddComponent<UIGestureDetector>();
            view.PanelLabel = SampleUi.CreateLabel(panel, Vector2.zero, new Vector2(900f, 60f), 28, TextAnchor.MiddleCenter);

            return view;
        }
    }

    // Builds uGUI controls in code, so the sample needs no scene or prefab.
    internal static class SampleUi
    {
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

        public static RectTransform CreateRect(string name, RectTransform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Text CreateLabel(RectTransform parent, Vector2 position, Vector2 size, int fontSize,
            TextAnchor alignment)
        {
            var label = CreateRect("Label", parent, position, size).gameObject.AddComponent<Text>();
            label.font = Font;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
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
    }
}
