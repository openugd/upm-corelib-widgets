using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUGD.UI
{
    /// <summary>
    /// Makes the <c>&lt;link&gt;</c> tags of a TextMeshPro label clickable: a click on a link raises
    /// <see cref="LinkClicked"/>, then opens the link id with <see cref="OpenUrl"/> unless a handler marked the
    /// click handled or <see cref="OpenUrls"/> is off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Clicks arrive only when the label is a raycast target under a <c>GraphicRaycaster</c> and the scene has an
    /// <c>EventSystem</c>. The hit test uses the press camera (<see cref="PointerEventData.pressEventCamera"/>),
    /// so it works on overlay, camera-space and world-space canvases. The component takes pointer-down, so a
    /// press on the label does not reach a pointer-down handler above it.
    /// </para>
    /// <para>
    /// The link id is opened as written. If the text is not authored by you, validate the id in a
    /// <see cref="LinkClicked"/> handler, or turn <see cref="OpenUrls"/> off.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(TMP_Text))]
    public class HyperlinkText : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        /// <summary>
        /// The label searched for links, and the one <c>HyperlinkTextPresenter</c> writes into. Filled in with this
        /// GameObject's <see cref="TMP_Text"/> by <c>Reset</c> in the editor, and at run time when a click or a
        /// render finds it empty. A label on another GameObject would not match this object's hit area.
        /// </summary>
        public TMP_Text Text;

        /// <summary>
        /// Whether a clicked link that no <see cref="LinkClicked"/> handler handled is opened. Default <c>true</c>.
        /// </summary>
        public bool OpenUrls = true;

        /// <summary>
        /// Raised for every click on a link, before anything is opened. Set <see cref="HyperlinkClick.Handled"/>
        /// to keep the link from being opened.
        /// </summary>
        /// <remarks>
        /// A C# event: handlers run in subscription order on the main thread and share one
        /// <see cref="HyperlinkClick"/>. A handler that throws stops the handlers after it, nothing is opened, and
        /// the exception reaches Unity's event system. Unsubscribe yourself, for example from a lifetime:
        /// <c>lifetime.AddAction(() =&gt; link.LinkClicked -= OnLink)</c>.
        /// </remarks>
        public event Action<HyperlinkClick> LinkClicked;

        /// <summary>
        /// Raised with the link id after <see cref="OpenUrl"/> has opened it. Not raised for a handled click or
        /// while <see cref="OpenUrls"/> is off. Event semantics as for <see cref="LinkClicked"/>.
        /// </summary>
        public event Action<string> HyperlinkOpenEvent;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            var text = ResolveText();
            if (text == null) return;

            var linkId = FindLinkId(text, eventData.position, eventData.pressEventCamera);
            if (linkId == null) return;

            var click = new HyperlinkClick(linkId);
            LinkClicked?.Invoke(click);
            if (click.Handled || !OpenUrls) return;

            OpenUrl(linkId);
            HyperlinkOpenEvent?.Invoke(linkId);
        }

        // Takes the press, so that it does not reach a pointer-down handler above it (a Button around the label).
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
        }

        /// <summary>
        /// The id of the link at a screen position, or <c>null</c>. The default uses
        /// <see cref="TMP_TextUtilities.FindIntersectingLink"/> on the text as last generated.
        /// </summary>
        /// <param name="text">The label.</param>
        /// <param name="position">The click, in screen pixels.</param>
        /// <param name="camera">The press camera; <c>null</c> for an overlay canvas.</param>
        /// <returns>The link id, or <c>null</c> when no link is there.</returns>
        protected virtual string FindLinkId(TMP_Text text, Vector2 position, Camera camera)
        {
            var info = text.textInfo;
            if (info == null) return null;

            var index = TMP_TextUtilities.FindIntersectingLink(text, position, camera);
            return index < 0 || index >= info.linkInfo.Length ? null : info.linkInfo[index].GetLinkID();
        }

        /// <summary>
        /// Opens a link that no handler handled. The default calls <see cref="Application.OpenURL"/>.
        /// </summary>
        /// <param name="url">The link id.</param>
        protected virtual void OpenUrl(string url) => Application.OpenURL(url);

        /// <summary>
        /// Unity's <c>Reset</c>: fills in <see cref="Text"/>.
        /// </summary>
        protected virtual void Reset() => Text = GetComponent<TMP_Text>();

        // The label, filling Text in from this GameObject when it is empty. Null only if there is no TMP_Text here.
        internal TMP_Text ResolveText()
        {
            if (Text == null) Text = GetComponent<TMP_Text>();
            return Text;
        }
    }

    /// <summary>
    /// A click on a link, as <see cref="HyperlinkText.LinkClicked"/> reports it.
    /// </summary>
    public sealed class HyperlinkClick
    {
        /// <summary>
        /// Creates an unhandled click on <paramref name="linkId"/>.
        /// </summary>
        /// <param name="linkId">The link id.</param>
        public HyperlinkClick(string linkId) => LinkId = linkId;

        /// <summary>
        /// The id in <c>&lt;link="..."&gt;</c>.
        /// </summary>
        public string LinkId { get; }

        /// <summary>
        /// Set to <c>true</c> to keep the link from being opened.
        /// </summary>
        public bool Handled { get; set; }
    }
}
