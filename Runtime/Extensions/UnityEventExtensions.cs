// © 2025 OpenUGD

using OpenUGD;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    /// <summary>
    /// Scope-bound subscription for Unity's <see cref="UnityEvent"/> family: adds the listener and registers
    /// its removal on a <see cref="Lifetime"/> in one statement, so the two cannot drift apart.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> A <see cref="UnityEvent"/> normally outlives the object listening to it, and
    /// a listener that is never removed keeps its delegate — and everything that delegate captured — alive
    /// for as long as the event's owner. Binding the <c>AddListener</c> to a scope at the call site makes
    /// the matching <c>RemoveListener</c> impossible to forget, because there is no path that adds without
    /// registering the removal.
    /// </para>
    /// <para>
    /// <b>Five overloads, differing only in arity</b>, mirroring <c>UnityEvent</c> through
    /// <c>UnityEvent&lt;T0, T1, T2, T3&gt;</c>. They all behave identically; see
    /// <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/> for the semantics.
    /// </para>
    /// </remarks>
    public static class UnityEventExtensions
    {
        /// <summary>
        /// Adds <paramref name="listener"/> to <paramref name="unityEvent"/>, and removes it again when
        /// <paramref name="lifetime"/> terminates.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>On an already-terminated <paramref name="lifetime"/> this is a net no-op</b>: the listener is
        /// added, then <see cref="Lifetime.AddAction"/> runs the removal immediately, before this method
        /// returns. That is the point of subscribing through a scope — a caller never has to test
        /// <see cref="Lifetime.IsTerminated"/> first, and a subscription made during teardown cannot leak.
        /// </para>
        /// <para>
        /// One closure is allocated per call and stays registered on <paramref name="lifetime"/> until it
        /// ends, so repeatedly subscribing on a long-lived scope accumulates entries. Subscribe on a
        /// <c>Lifetime.DefineNested</c> definition and terminate that when the subscription should end
        /// earlier than the scope — there is deliberately no unsubscribe handle.
        /// </para>
        /// <para>
        /// Nothing is invoked on subscription: the listener sees only invocations that happen after it.
        /// </para>
        /// </remarks>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The <i>subscriber's</i> scope, not the event's. It decides when the
        /// listener is removed; the event is free to outlive it.</param>
        /// <param name="listener">The callback. The event holds it — and everything it captured — until the
        /// scope ends.</param>
        /// <exception cref="System.NullReferenceException"><paramref name="unityEvent"/> or
        /// <paramref name="lifetime"/> is <c>null</c>: neither is validated, so the failure is the
        /// dereference itself rather than a named argument. A <c>null</c> <paramref name="listener"/> is not
        /// rejected here — it is handed to <c>AddListener</c> unchecked.</exception>
        public static void Subscribe(
            this UnityEvent unityEvent,
            Lifetime lifetime,
            UnityAction listener
        )
        {
            unityEvent.AddListener(listener);
            lifetime.AddAction(() => { unityEvent.RemoveListener(listener); });
        }

        /// <summary>
        /// The one-argument form of <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>: same
        /// add-then-register-the-removal, same behaviour on a terminated scope.
        /// </summary>
        /// <typeparam name="T0">The type of the value the event carries.</typeparam>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The subscriber's scope; the listener is removed when it terminates.</param>
        /// <param name="listener">The callback, invoked with the event's argument.</param>
        /// <exception cref="System.NullReferenceException"><paramref name="unityEvent"/> or
        /// <paramref name="lifetime"/> is <c>null</c>; neither is validated, and a <c>null</c>
        /// <paramref name="listener"/> is not rejected.</exception>
        public static void Subscribe<T0>(
            this UnityEvent<T0> unityEvent,
            Lifetime lifetime,
            UnityAction<T0> listener
        )
        {
            unityEvent.AddListener(listener);
            lifetime.AddAction(() => { unityEvent.RemoveListener(listener); });
        }

        /// <summary>
        /// The two-argument form of <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>: same
        /// add-then-register-the-removal, same behaviour on a terminated scope.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first value.</typeparam>
        /// <typeparam name="T1">The type of the event's second value.</typeparam>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The subscriber's scope; the listener is removed when it terminates.</param>
        /// <param name="listener">The callback, invoked with the event's arguments.</param>
        /// <exception cref="System.NullReferenceException"><paramref name="unityEvent"/> or
        /// <paramref name="lifetime"/> is <c>null</c>; neither is validated, and a <c>null</c>
        /// <paramref name="listener"/> is not rejected.</exception>
        public static void Subscribe<T0, T1>(
            this UnityEvent<T0, T1> unityEvent,
            Lifetime lifetime,
            UnityAction<T0, T1> listener
        )
        {
            unityEvent.AddListener(listener);
            lifetime.AddAction(() => { unityEvent.RemoveListener(listener); });
        }

        /// <summary>
        /// The three-argument form of <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>: same
        /// add-then-register-the-removal, same behaviour on a terminated scope.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first value.</typeparam>
        /// <typeparam name="T1">The type of the event's second value.</typeparam>
        /// <typeparam name="T2">The type of the event's third value.</typeparam>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The subscriber's scope; the listener is removed when it terminates.</param>
        /// <param name="listener">The callback, invoked with the event's arguments.</param>
        /// <exception cref="System.NullReferenceException"><paramref name="unityEvent"/> or
        /// <paramref name="lifetime"/> is <c>null</c>; neither is validated, and a <c>null</c>
        /// <paramref name="listener"/> is not rejected.</exception>
        public static void Subscribe<T0, T1, T2>(
            this UnityEvent<T0, T1, T2> unityEvent,
            Lifetime lifetime,
            UnityAction<T0, T1, T2> listener
        )
        {
            unityEvent.AddListener(listener);
            lifetime.AddAction(() => { unityEvent.RemoveListener(listener); });
        }

        /// <summary>
        /// The four-argument form of <see cref="Subscribe(UnityEvent, Lifetime, UnityAction)"/>, and the
        /// widest one Unity's <c>UnityEvent</c> generics go: same add-then-register-the-removal, same
        /// behaviour on a terminated scope.
        /// </summary>
        /// <typeparam name="T0">The type of the event's first value.</typeparam>
        /// <typeparam name="T1">The type of the event's second value.</typeparam>
        /// <typeparam name="T2">The type of the event's third value.</typeparam>
        /// <typeparam name="T3">The type of the event's fourth value.</typeparam>
        /// <param name="unityEvent">The event to listen to.</param>
        /// <param name="lifetime">The subscriber's scope; the listener is removed when it terminates.</param>
        /// <param name="listener">The callback, invoked with the event's arguments.</param>
        /// <exception cref="System.NullReferenceException"><paramref name="unityEvent"/> or
        /// <paramref name="lifetime"/> is <c>null</c>; neither is validated, and a <c>null</c>
        /// <paramref name="listener"/> is not rejected.</exception>
        public static void Subscribe<T0, T1, T2, T3>(
            this UnityEvent<T0, T1, T2, T3> unityEvent,
            Lifetime lifetime,
            UnityAction<T0, T1, T2, T3> listener
        )
        {
            unityEvent.AddListener(listener);
            lifetime.AddAction(() => { unityEvent.RemoveListener(listener); });
        }
    }
}
