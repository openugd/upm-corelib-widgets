using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Samples.RecycledList
{
    /// <summary>
    /// One row of the list: the part of the screen an <see cref="ItemPresenter"/> draws on while its item is in the
    /// viewport. Row views are pooled and shown again for other items.
    /// </summary>
    public sealed class RowView : MonoBehaviour
    {
        /// <summary>The row's own transform, positioned by the list.</summary>
        public RectTransform RectTransform;

        /// <summary>The item's title.</summary>
        public Text Title;

        /// <summary>The like count.</summary>
        public Text Likes;

        /// <summary>Likes the item.</summary>
        public Button Like;

        /// <summary>Stars the item.</summary>
        public Toggle Star;

        /// <summary>
        /// Builds a row under <paramref name="content"/>, anchored to its top.
        /// </summary>
        /// <param name="content">The list's content.</param>
        /// <param name="height">The row height.</param>
        /// <returns>The row.</returns>
        public static RowView Create(RectTransform content, float height)
        {
            var rect = SampleUi.CreateRect("Row", content);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height - 4f);
            rect.gameObject.AddComponent<Image>().color = SampleUi.Row;

            var row = rect.gameObject.AddComponent<RowView>();
            row.RectTransform = rect;
            row.Title = SampleUi.CreateLabel(rect, "", Vector2.zero, Vector2.zero, 20);
            Stretch(row.Title.rectTransform, 16f, 330f);
            row.Likes = SampleUi.CreateLabel(rect, "", Vector2.zero, Vector2.zero, 16);
            Stretch(row.Likes.rectTransform, 200f, 220f);

            row.Star = SampleUi.CreateToggle(rect, "Star");
            Place((RectTransform)row.Star.transform, -200f, 90f);
            row.Like = SampleUi.CreateButton(rect, "Like");
            Place((RectTransform)row.Like.transform, -16f, 96f);
            return row;
        }

        // Anchors a label between a left inset and a right inset.
        private static void Stretch(RectTransform rect, float left, float right)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-right, 0f);
        }

        // Pins a control to the row's right edge.
        private static void Place(RectTransform rect, float right, float width)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(right, 0f);
            rect.sizeDelta = new Vector2(width, 36f);
        }
    }
}
