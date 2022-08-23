// © 2025 OpenUGD

using OpenUGD;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    public static class ButtonExtensions
    {
        public static Button SubscribeOnClick(this Button button, Lifetime lifetime, UnityAction listener)
        {
            button.onClick.AddListener(listener);
            lifetime.AddAction(() => { button.onClick.RemoveListener(listener); });
            return button;
        }
    }
}
