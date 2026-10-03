using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUGD.UI
{
    /// <summary>
    /// Makes the <c>&lt;link&gt;</c> tags inside a TextMeshPro label clickable: a click that lands on one raises
    /// <see cref="LinkClicked"/>, and then, unless a handler took it or <see cref="OpenUrls"/> is off, opens the
    /// link's id with <see cref="Application.OpenURL"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Wiring a prefab.</b> Put it on the object carrying the label. <see cref="Text"/> is filled in with the
    /// <see cref="TMP_Text"/> on the same GameObject when the component is added in the editor, and at run time
    /// whenever it is found empty. Clicks arrive only if the label is a raycast target under a
    /// <c>GraphicRaycaster</c>, with an <c>EventSystem</c> in the scene; the whole label is the target, and which
    /// characters are links is then worked out from the text itself.
    /// </para>
    /// <para>
    /// <b>Any canvas mode.</b> The hit test uses the camera of the raycast that registered the press
    /// (<see cref="PointerEventData.pressEventCamera"/>): none for a <c>Screen Space - Overlay</c> canvas, the
    /// canvas camera for a camera-space or world-space one.
    /// </para>
    /// <para>
    /// <b>The link id is the URL.</b> Whatever the text author put in <c>&lt;link="..."&gt;</c> is what gets
    /// opened. Treat authored text as untrusted input if it comes from anywhere but your own build: validate the
    /// id in a <see cref="LinkClicked"/> handler and mark the click handled to refuse it, or turn
    /// <see cref="OpenUrls"/> off and open what you approve yourself.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — the hit test passed no camera, so links did not line up with the glyphs under a
    /// camera-space or world-space canvas; every link was opened before anything could see it, with no way to
    /// refuse it; and an unassigned <see cref="Text"/> threw on the first click (audit WG-11).
    /// <see cref="LinkClicked"/>, <see cref="OpenUrls"/>, <see cref="FindLinkId"/> and <see cref="OpenUrl"/> are
    /// new; <see cref="HyperlinkOpenEvent"/> is still raised for every link that is opened.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(TMP_Text))]
    public class HyperlinkText : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        /// <summary>
        /// The label that is searched for links, and the one <c>HyperlinkTextPresenter</c> writes its text into.
        /// Filled in with the <see cref="TMP_Text"/> on this GameObject when the component is added in the editor
        /// (<c>Reset</c>), and at run time by the first click or render that finds it empty. Any other instance can
        /// be assigned, but the clickable area is still this object's raycast target, so pointing it at a label
        /// somewhere else makes the hit test and the hit area disagree.
        /// </summary>
        public TMP_Text Text;

        /// <summary>
        /// Whether a clicked link that no <see cref="LinkClicked"/> handler marked
        /// <see cref="HyperlinkClick.Handled"/> is opened with <see cref="Application.OpenURL"/>. Default
        /// <c>true</c>. Turn it off to handle every link in code.
        /// </summary>
        public bool OpenUrls = true;

        /// <summary>
        /// Raised first, for every click that lands on a link, before anything is opened. Set
        /// <see cref="HyperlinkClick.Handled"/> to keep the link from being opened.
        /// </summary>
        /// <remarks>
        /// Ordinary multicast semantics: handlers run in subscription order on the main thread, inside the
        /// <c>EventSystem</c>'s click dispatch, and every handler sees the same <see cref="HyperlinkClick"/>, so a
        /// later one can see that an earlier one took the click. A handler that throws stops those behind it,
        /// nothing is opened, and the exception escapes into Unity's event handling. Unlike a lifetime-scoped
        /// <c>Signal</c>, nothing detaches handlers for you: this component holds every subscriber until it is
        /// destroyed.
        /// </remarks>
        public event Action<HyperlinkClick> LinkClicked;

        /// <summary>
        /// Raised with the link id immediately after that id has been opened with <see cref="OpenUrl"/> — for
        /// analytics, or for reacting in-game to a link the reader followed. Not raised for a click that a
        /// <see cref="LinkClicked"/> handler took, or while <see cref="OpenUrls"/> is off.
        /// </summary>
        /// <remarks>
        /// Multicast semantics as for <see cref="LinkClicked"/>. <i>Changed in 2.0.0</i> — it reports only what
        /// was actually opened; <see cref="LinkClicked"/> is the event to veto with.
        /// </remarks>
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

        // Takes the press, so that it does not reach a pointer-down handler further up the hierarchy (a Button
        // around the label, say).
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
        }

        /// <summary>
        /// The id of the link in <paramref name="text"/> at a screen position, or <c>null</c> for none.
        /// </summary>
        /// <remarks>
        /// The default asks <see cref="TMP_TextUtilities.FindIntersectingLink"/>, which looks at the text as
        /// last generated. Override it to change the hit test — a more forgiving one for small links, say.
        /// </remarks>
        /// <param name="text">The label: <see cref="Text"/>.</param>
        /// <param name="position">The click, in screen pixels.</param>
        /// <param name="camera">The camera the press was raycast with; <c>null</c> for an overlay canvas.</param>
        /// <returns>The link id, or <c>null</c> when the position is on no link.</returns>
        protected virtual string FindLinkId(TMP_Text text, Vector2 position, Camera camera)
        {
            var info = text.textInfo;
            if (info == null) return null;

            var index = TMP_TextUtilities.FindIntersectingLink(text, position, camera);
            return index < 0 || index >= info.linkInfo.Length ? null : info.linkInfo[index].GetLinkID();
        }

        /// <summary>
        /// Opens a link nobody took: <see cref="Application.OpenURL"/>. Override it to send links somewhere
        /// else, an in-app browser say.
        /// </summary>
        /// <param name="url">The link id.</param>
        protected virtual void OpenUrl(string url) => Application.OpenURL(url);

        /// <summary>
        /// Unity's <c>Reset</c>, run when the component is added in the editor: fills in <see cref="Text"/>.
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
    /// One click on a link of a <see cref="HyperlinkText"/>, as <see cref="HyperlinkText.LinkClicked"/> reports
    /// it.
    /// </summary>
    public sealed class HyperlinkClick
    {
        /// <summary>
        /// Creates the report of a click on <paramref name="linkId"/>, not yet handled.
        /// </summary>
        /// <param name="linkId">The id of the link that was clicked.</param>
        public HyperlinkClick(string linkId) => LinkId = linkId;

        /// <summary>
        /// The id the text author put in <c>&lt;link="..."&gt;</c>.
        /// </summary>
        public string LinkId { get; }

        /// <summary>
        /// Set it to <c>true</c> to take the click: the link is then not opened. Every handler of the same click
        /// sees the same value.
        /// </summary>
        public bool Handled { get; set; }
    }
}
