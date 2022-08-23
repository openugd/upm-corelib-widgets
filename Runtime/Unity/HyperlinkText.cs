using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenUGD.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class HyperlinkText : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        public TMP_Text Text;

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
