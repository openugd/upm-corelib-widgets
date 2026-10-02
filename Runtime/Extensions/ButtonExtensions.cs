// © 2025 OpenUGD

using OpenUGD;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    /// <summary>
    /// Scope-bound click handling for <see cref="Button"/> — the one case common enough to deserve a name of
    /// its own rather than <c>button.onClick.Subscribe(...)</c>.
    /// </summary>
    public static class ButtonExtensions
    {
        /// <summary>
        /// Adds <paramref name="listener"/> to <paramref name="button"/>'s click event, and removes it again
        /// when <paramref name="lifetime"/> terminates.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Behaves exactly like
        /// <see cref="UnityEventExtensions.Subscribe(UnityEvent, Lifetime, UnityAction)"/> applied to
        /// <see cref="Button.onClick"/>, and returns the button so a widget can be wired in a single
        /// expression. <b>On an already-terminated <paramref name="lifetime"/> it is a net no-op</b>: the
        /// listener is added and removed again before this returns.
        /// </para>
        /// <para>
        /// Only the runtime listener is affected. Anything wired into <see cref="Button.onClick"/> in the
        /// inspector is persistent, is not touched by this call, and survives the scope ending.
        /// </para>
        /// <para>
        /// The removal runs when <paramref name="lifetime"/> ends, which is not necessarily before
        /// <paramref name="button"/> is destroyed — nothing here ties the two together. If the handler must
        /// stop firing exactly when the button goes away, terminate the scope from whatever owns the button.
        /// </para>
        /// </remarks>
        /// <param name="button">The button to listen to.</param>
        /// <param name="lifetime">The <i>subscriber's</i> scope, not the button's: it decides when the
        /// listener is removed. A presenter passes its own <c>Lifetime</c>.</param>
        /// <param name="listener">The click handler. The button holds it — and everything it captured —
        /// until the scope ends.</param>
        /// <returns><paramref name="button"/>, so calls can be chained.</returns>
        /// <exception cref="System.NullReferenceException"><paramref name="button"/> or
        /// <paramref name="lifetime"/> is <c>null</c>: neither is validated, so the failure is the
        /// dereference itself rather than a named argument. A <c>null</c> <paramref name="listener"/> is not
        /// rejected here — it is handed to <c>AddListener</c> unchecked.</exception>
        public static Button SubscribeOnClick(this Button button, Lifetime lifetime, UnityAction listener)
        {
            button.onClick.AddListener(listener);
            lifetime.AddAction(() => { button.onClick.RemoveListener(listener); });
            return button;
        }
    }
}
