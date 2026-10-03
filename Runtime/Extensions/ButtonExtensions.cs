// © 2025 OpenUGD

using System;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace OpenUGD
{
    /// <summary>
    /// Scope-bound click handling for a <see cref="Button"/> that is not driven by a presenter.
    /// </summary>
    /// <remarks>
    /// <i>Changed in 2.0.0</i> — moved from the <c>UnityEngine.UI</c> namespace to <c>OpenUGD</c>, and the
    /// arguments are validated.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "UnityEngine.UI")]
    public static class ButtonExtensions
    {
        /// <summary>
        /// Adds <paramref name="listener"/> to <paramref name="button"/>'s click event, and removes it again
        /// when <paramref name="lifetime"/> terminates.
        /// </summary>
        /// <remarks>
        /// Exactly <see cref="UnityEventExtensions.Subscribe(UnityEvent, Lifetime, UnityAction)"/> on
        /// <see cref="Button.onClick"/>, returning the button so the call can be chained. The removal is tied
        /// to <paramref name="lifetime"/>, not to the button's destruction.
        /// </remarks>
        /// <param name="button">The button to listen to.</param>
        /// <param name="lifetime">The <i>subscriber's</i> scope: the listener is removed when it terminates.
        /// </param>
        /// <param name="listener">The click handler.</param>
        /// <returns><paramref name="button"/>.</returns>
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
