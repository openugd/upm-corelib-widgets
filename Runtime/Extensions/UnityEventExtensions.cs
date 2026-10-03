// © 2025 OpenUGD

using System;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace OpenUGD
{
    /// <summary>
    /// Scope-bound subscription for Unity's <see cref="UnityEvent"/> family: adds the listener and registers
    /// its removal on a <see cref="Lifetime"/> in one call, so the two cannot drift apart.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="UnityEvent"/> usually outlives the object listening to it, and a listener that is never
    /// removed keeps its delegate, and everything the delegate captured, alive for as long as the event's
    /// owner. Subscribing through a scope makes the matching <c>RemoveListener</c> impossible to forget.
    /// Every presenter in this package wires its view this way, on its <c>ViewLifetime</c>.
    /// </para>
    /// <para>
    /// The removal targets the event instance passed in, not whatever a property returns later, so it stays
    /// correct after the component that owns the event has been swapped out or destroyed.
    /// </para>
    /// <para>
    /// Five overloads, one per <see cref="UnityEvent"/> arity, all with the semantics of
    /// <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>.
    /// </para>
    /// <para>
    /// <i>Changed in 2.0.0</i> — moved from the <c>UnityEngine.UI</c> namespace to <c>OpenUGD</c>, and the
    /// arguments are validated.
    /// </para>
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "UnityEngine.UI")]
    public static class UnityEventExtensions
    {
        /// <summary>
        /// Adds <paramref name="listener"/> to <paramref name="unityEvent"/>, and removes it again when
        /// <paramref name="lifetime"/> terminates.
        /// </summary>
        /// <remarks>
        /// <para>
        /// On an already-terminated <paramref name="lifetime"/> this is a no-op: nothing stays subscribed. Only
        /// the runtime listener is affected: listeners wired in the inspector are persistent and are never
        /// touched.
        /// </para>
        /// <para>
        /// Nothing is invoked on subscription. One closure stays registered on <paramref name="lifetime"/>
        /// until it ends, so subscribe on a nested scope (<c>Lifetime.DefineNested</c>) when the listener should
        /// go away earlier than the scope you have.
        /// </para>
        /// </remarks>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The <i>subscriber's</i> scope: the listener is removed when it terminates.
        /// </param>
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
        /// <param name="lifetime">The subscriber's scope; the listener is removed when it terminates.</param>
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
        /// <param name="lifetime">The subscriber's scope; the listener is removed when it terminates.</param>
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
        /// <param name="lifetime">The subscriber's scope; the listener is removed when it terminates.</param>
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
        /// The four-argument form of <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>, the widest
        /// <see cref="UnityEvent"/> goes.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first value.</typeparam>
        /// <typeparam name="T1">The type of the event's second value.</typeparam>
        /// <typeparam name="T2">The type of the event's third value.</typeparam>
        /// <typeparam name="T3">The type of the event's fourth value.</typeparam>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The subscriber's scope; the listener is removed when it terminates.</param>
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
