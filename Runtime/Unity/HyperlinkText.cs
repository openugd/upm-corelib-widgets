using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUGD.UI
{
    /// <summary>
    /// Makes the <c>&lt;link&gt;</c> tags inside a TextMeshPro label clickable: a click that lands on one
    /// opens its id with <see cref="Application.OpenURL"/> and reports it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Wiring a prefab.</b> Put it on the object carrying the label. <c>[RequireComponent]</c> guarantees
    /// a <see cref="TMP_Text"/> is present but does <b>not</b> fill in <see cref="Text"/> — assign that
    /// yourself, or the first click throws. Clicks arrive only if the label is a raycast target under a
    /// <c>GraphicRaycaster</c>, with an <c>EventSystem</c> in the scene; the whole label is the target, and
    /// which characters are links is then worked out from the text itself.
    /// </para>
    /// <para>
    /// <b>Canvas mode matters.</b> The hit test is performed with no camera, which is what a
    /// <c>Screen Space - Overlay</c> canvas wants. Under a camera-space or world-space canvas the pointer
    /// position is interpreted in the wrong space and links will not line up with the glyphs.
    /// </para>
    /// <para>
    /// <b>The link id is the URL.</b> Whatever the text author put in <c>&lt;link="..."&gt;</c> is handed
    /// straight to the platform, unvalidated. Treat authored text as untrusted input if it comes from
    /// anywhere but your own build. <see cref="HyperlinkOpenEvent"/> reports what was opened but cannot
    /// prevent it, so validation has to happen where the text is authored.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(TMP_Text))]
    public class HyperlinkText : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        /// <summary>
        /// The label that is searched for links, and the one <c>HyperlinkTextPresenter</c> writes its text
        /// into. Nothing assigns it at runtime: leaving it empty in the inspector costs no warning at
        /// start-up and a <see cref="NullReferenceException"/> on the first click, wherever that click
        /// lands. Normally the <see cref="TMP_Text"/> on this same GameObject, though any instance can be
        /// dragged in — the clickable area is still this object's raycast target, so pointing it at a label
        /// somewhere else makes the hit test and the hit area disagree.
        /// </summary>
        public TMP_Text Text;

        /// <summary>
        /// Raised with the link id immediately after that id has been opened. For analytics, or for
        /// reacting in-game to a link the reader followed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>After, not instead of.</b> <see cref="Application.OpenURL"/> has already been called by the
        /// time a handler runs, so this reports rather than approves. There is no way to veto an open short
        /// of not authoring the link.
        /// </para>
        /// <para>
        /// Ordinary multicast semantics: handlers run in subscription order on the main thread, inside the
        /// <c>EventSystem</c>'s click dispatch, and one that throws stops those behind it and lets the
        /// exception escape into Unity's event handling. Unlike a lifetime-scoped <c>Signal</c>, nothing
        /// detaches handlers for you — this component holds every subscriber until it is destroyed.
        /// </para>
        /// </remarks>
        public event Action<string> HyperlinkOpenEvent;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(Text, eventData.position, null);
            if (linkIndex == -1 || linkIndex >= Text.textInfo.linkInfo.Length)
            {
                return;
            }

            string link = Text.textInfo.linkInfo[linkIndex].GetLinkID();
            Application.OpenURL(link);
            HyperlinkOpenEvent?.Invoke(link);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
        }
    }
}
