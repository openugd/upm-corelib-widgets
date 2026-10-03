using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenUGD.Samples.RecycledList
{
    /// <summary>
    /// The list's controls: a vertical <see cref="ScrollRect"/> whose content is as tall as all the rows together,
    /// and a status line. Built in code by <see cref="Create"/>.
    /// </summary>
    public sealed class RecycledListView
    {
        /// <summary>The scroll view.</summary>
        public ScrollRect Scroll { get; private set; }

        /// <summary>The scrolled content, anchored to the top; rows are its children.</summary>
        public RectTransform Content { get; private set; }

        /// <summary>The status line.</summary>
        public Text Status { get; private set; }

        /// <summary>The height of one row, in canvas units.</summary>
        public float RowHeight { get; private set; }

        /// <summary>
        /// Builds the list on a new canvas under <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">The canvas's parent; destroying it destroys the list.</param>
        /// <returns>The view.</returns>
        public static RecycledListView Create(Transform parent)
        {
            var canvas = SampleUi.CreateCanvas(parent, "Recycled List");
            var view = new RecycledListView { RowHeight = 56f };

            SampleUi.CreateLabel(canvas, "Scroll, then press Like on a row: the like goes to the item the row shows now.",
                new Vector2(0f, 320f), new Vector2(900f, 32f), 20);
            view.Status = SampleUi.CreateLabel(canvas, "", new Vector2(0f, -320f), new Vector2(900f, 32f), 18);

            // A fixed size, so the viewport's height is known before the first layout pass.
            var scroll = SampleUi.CreateRect("Scroll View", canvas);
            scroll.sizeDelta = new Vector2(640f, 560f);
            scroll.gameObject.AddComponent<Image>().color = SampleUi.Panel;
            view.Scroll = scroll.gameObject.AddComponent<ScrollRect>();
            view.Scroll.horizontal = false;
            view.Scroll.movementType = ScrollRect.MovementType.Clamped;
            view.Scroll.scrollSensitivity = 30f;

            var viewport = SampleUi.CreateRect("Viewport", scroll);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 8f);
            viewport.offsetMax = new Vector2(-8f, -8f);
            viewport.gameObject.AddComponent<RectMask2D>();
            view.Scroll.viewport = viewport;

            view.Content = SampleUi.CreateRect("Content", viewport);
            view.Content.anchorMin = new Vector2(0f, 1f);
            view.Content.anchorMax = new Vector2(1f, 1f);
            view.Content.pivot = new Vector2(0.5f, 1f);
            view.Content.sizeDelta = Vector2.zero;
            view.Scroll.content = view.Content;

            return view;
        }
    }

    // Builds uGUI controls in code, so the sample needs no scene or prefab.
    internal static class SampleUi
    {
        public static readonly Color Panel = new Color(0.13f, 0.15f, 0.18f, 0.96f);
        public static readonly Color Row = new Color(0.20f, 0.23f, 0.27f, 1f);
        public static readonly Color Accent = new Color(0.31f, 0.76f, 0.97f);

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

        public static RectTransform CreateRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Text CreateLabel(RectTransform parent, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var rect = CreateRect("Label", parent);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        public static Button CreateButton(RectTransform parent, string text)
        {
            var go = DefaultControls.CreateButton(default);
            go.transform.SetParent(parent, false);
            var label = go.GetComponentInChildren<Text>();
            label.font = Font;
            label.fontSize = 18;
            label.text = text;
            return go.GetComponent<Button>();
        }

        public static Toggle CreateToggle(RectTransform parent, string text)
        {
            var go = DefaultControls.CreateToggle(default);
            go.transform.SetParent(parent, false);
            var toggle = go.GetComponent<Toggle>();
            toggle.graphic.color = Accent;
            var label = go.GetComponentInChildren<Text>();
            label.font = Font;
            label.fontSize = 18;
            label.color = Color.white;
            label.text = text;
            return toggle;
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
