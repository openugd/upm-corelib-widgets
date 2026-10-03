using System;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace OpenUGD
{
    /// <summary>
    /// Lifetime-scoped click listeners for a <see cref="Button"/> that no presenter drives.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "UnityEngine.UI")]
    public static class ButtonExtensions
    {
        /// <summary>
        /// Adds <paramref name="listener"/> to <see cref="Button.onClick"/> and removes it when
        /// <paramref name="lifetime"/> ends; see <see cref="UnityEventExtensions.Subscribe(UnityEvent, Lifetime, UnityAction)"/>.
        /// </summary>
        /// <param name="button">The button.</param>
        /// <param name="lifetime">The subscriber's scope.</param>
        /// <param name="listener">The click handler.</param>
        /// <returns><paramref name="button"/>, for chaining.</returns>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
        public static Button SubscribeOnClick(this Button button, Lifetime lifetime, UnityAction listener)
        {
            if (ReferenceEquals(button, null))
                throw new ArgumentNullException(nameof(button), $"{nameof(button)} can't be null");

            button.onClick.Subscribe(lifetime, listener);
            return button;
        }
    }
}
