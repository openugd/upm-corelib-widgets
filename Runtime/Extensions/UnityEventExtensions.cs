// © 2025 OpenUGD

using System;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace OpenUGD
{
    /// <summary>
    /// Lifetime-scoped listeners for Unity's <see cref="UnityEvent"/> family: the listener is added now and
    /// removed when a <see cref="Lifetime"/> ends.
    /// </summary>
    /// <remarks>
    /// The removal targets the event instance passed in, so it is correct even after the event's owner has been
    /// destroyed. Persistent listeners wired in the inspector are never touched. The button, toggle, slider and
    /// input field presenters wire their views with these methods.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "UnityEngine.UI")]
    public static class UnityEventExtensions
    {
        /// <summary>
        /// Adds <paramref name="listener"/> to <paramref name="unityEvent"/> and removes it when
        /// <paramref name="lifetime"/> ends.
        /// </summary>
        /// <remarks>
        /// On a terminated <paramref name="lifetime"/> nothing is added. Nothing is invoked on subscription. One
        /// closure stays registered on <paramref name="lifetime"/> until it ends; subscribe on a nested scope to
        /// remove the listener earlier.
        /// </remarks>
        /// <param name="unityEvent">The event.</param>
        /// <param name="lifetime">The subscriber's scope.</param>
        /// <param name="listener">The callback.</param>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
        public static void Subscribe(this UnityEvent unityEvent, Lifetime lifetime, UnityAction listener)
        {
            Validate(unityEvent, lifetime, listener);
            if (lifetime.IsTerminated) return;

            unityEvent.AddListener(listener);
            lifetime.AddAction(() => unityEvent.RemoveListener(listener));
        }

        /// <summary>
        /// The one-argument form of <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>.
        /// </summary>
        /// <typeparam name="T0">The type of the value the event carries.</typeparam>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The subscriber's scope.</param>
        /// <param name="listener">The callback, invoked with the event's argument.</param>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
        public static void Subscribe<T0>(this UnityEvent<T0> unityEvent, Lifetime lifetime,
            UnityAction<T0> listener)
        {
            Validate(unityEvent, lifetime, listener);
            if (lifetime.IsTerminated) return;

            unityEvent.AddListener(listener);
            lifetime.AddAction(() => unityEvent.RemoveListener(listener));
        }

        /// <summary>
        /// The two-argument form of <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first value.</typeparam>
        /// <typeparam name="T1">The type of the event's second value.</typeparam>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The subscriber's scope.</param>
        /// <param name="listener">The callback, invoked with the event's arguments.</param>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
        public static void Subscribe<T0, T1>(this UnityEvent<T0, T1> unityEvent, Lifetime lifetime,
            UnityAction<T0, T1> listener)
        {
            Validate(unityEvent, lifetime, listener);
            if (lifetime.IsTerminated) return;

            unityEvent.AddListener(listener);
            lifetime.AddAction(() => unityEvent.RemoveListener(listener));
        }

        /// <summary>
        /// The three-argument form of <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first value.</typeparam>
        /// <typeparam name="T1">The type of the event's second value.</typeparam>
        /// <typeparam name="T2">The type of the event's third value.</typeparam>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The subscriber's scope.</param>
        /// <param name="listener">The callback, invoked with the event's arguments.</param>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
        public static void Subscribe<T0, T1, T2>(this UnityEvent<T0, T1, T2> unityEvent, Lifetime lifetime,
            UnityAction<T0, T1, T2> listener)
        {
            Validate(unityEvent, lifetime, listener);
            if (lifetime.IsTerminated) return;

            unityEvent.AddListener(listener);
            lifetime.AddAction(() => unityEvent.RemoveListener(listener));
        }

        /// <summary>
        /// The four-argument form of <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first value.</typeparam>
        /// <typeparam name="T1">The type of the event's second value.</typeparam>
        /// <typeparam name="T2">The type of the event's third value.</typeparam>
        /// <typeparam name="T3">The type of the event's fourth value.</typeparam>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The subscriber's scope.</param>
        /// <param name="listener">The callback, invoked with the event's arguments.</param>
        /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
        public static void Subscribe<T0, T1, T2, T3>(this UnityEvent<T0, T1, T2, T3> unityEvent,
            Lifetime lifetime, UnityAction<T0, T1, T2, T3> listener)
        {
            Validate(unityEvent, lifetime, listener);
            if (lifetime.IsTerminated) return;

            unityEvent.AddListener(listener);
            lifetime.AddAction(() => unityEvent.RemoveListener(listener));
        }

        private static void Validate(UnityEventBase unityEvent, Lifetime lifetime, Delegate listener)
        {
            if (unityEvent == null)
                throw new ArgumentNullException(nameof(unityEvent), $"{nameof(unityEvent)} can't be null");
            if (lifetime == null)
                throw new ArgumentNullException(nameof(lifetime), $"{nameof(lifetime)} can't be null");
            if (listener == null)
                throw new ArgumentNullException(nameof(listener), $"{nameof(listener)} can't be null");
        }
    }
}
