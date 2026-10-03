using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace OpenUGD.UI
{
    // An ISignal with no Lifetime of its own, for a component that cannot own one (audit WG-10).
    //
    // Signal needs an owning Lifetime, and every Lifetime is nested, directly or not, in Lifetime.Eternal. A
    // component ends its scope in OnDestroy, but Unity never sends OnDestroy to an object that was never active, so
    // a scope created for such an object - by a field initializer or a lazy getter - stays on Eternal for the rest
    // of the process. Here a registration is held by this object and ended by the subscriber's lifetime; nothing
    // is rooted anywhere else, so a detector that is never activated is collected with its subscribers. Close() is
    // the owner's OnDestroy: it drops every handler and refuses new ones.
    //
    // Otherwise the semantics of Signal: handlers run in subscription order; a dispatch works on the registrations
    // present when it starts and skips any that end during it; the same handler may be subscribed twice; a handler
    // that throws does not stop the others, and the failures are reported at the end, one as itself and two or
    // more as an AggregateException. Dispatch is meant for Unity's main thread; subscribing and ending a
    // registration are safe from any thread.
    internal sealed class OwnerlessSignal : ISignal
    {
        private readonly object _lock = new object();

        // Copy-on-write: a published array is never written again, so Fire reads it without the lock.
        private volatile Registration[] _registrations = Array.Empty<Registration>();
        private volatile bool _closed;

        public bool IsClosed => _closed;

        // For tests: how many registrations are held.
        internal int Count => _registrations.Length;

        public void Subscribe(Lifetime lifetime, Action handler)
        {
            if (lifetime == null)
                throw new ArgumentNullException(nameof(lifetime), $"{nameof(lifetime)} can't be null");
            if (handler == null)
                throw new ArgumentNullException(nameof(handler), $"{nameof(handler)} can't be null");

            if (_closed || lifetime.IsTerminated) return;

            var registration = new Registration(this, handler);
            if (!Add(registration)) return;

            // After the insertion: if the lifetime ended in between, AddAction runs End at once and undoes it.
            lifetime.AddAction(registration.End);
        }

        public void Fire()
        {
            var registrations = _registrations;
            Exception failure = null;
            List<Exception> failures = null;

            for (var i = 0; i < registrations.Length; i++)
            {
                var handler = registrations[i].Handler;
                if (handler == null) continue;

                try
                {
                    handler();
                }
                catch (Exception exception)
                {
                    if (failure == null) failure = exception;
                    else (failures ??= new List<Exception> { failure }).Add(exception);
                }
            }

            if (failure == null) return;
            if (failures == null) ExceptionDispatchInfo.Capture(failure).Throw();

            throw new AggregateException(
                $"{failures.Count} gesture handlers threw during one dispatch. Every other live handler was " +
                "still invoked.", failures);
        }

        public void Close()
        {
            Registration[] registrations;
            lock (_lock)
            {
                _closed = true;
                registrations = _registrations;
                _registrations = Array.Empty<Registration>();
            }

            // Let go of the handlers, and of this signal from the subscribers' lifetimes, which keep the emptied
            // registrations until they end.
            foreach (var registration in registrations) registration.Release();
        }

        private bool Add(Registration registration)
        {
            lock (_lock)
            {
                if (_closed) return false;

                var current = _registrations;
                var next = new Registration[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = registration;
                _registrations = next;
                return true;
            }
        }

        private void Remove(Registration registration)
        {
            lock (_lock)
            {
                var current = _registrations;
                var index = Array.IndexOf(current, registration);
                if (index < 0) return;

                var next = new Registration[current.Length - 1];
                Array.Copy(current, 0, next, 0, index);
                Array.Copy(current, index + 1, next, index, current.Length - index - 1);
                _registrations = next;
            }
        }

        private sealed class Registration
        {
            private volatile Action _handler;
            private volatile OwnerlessSignal _owner;

            public Registration(OwnerlessSignal owner, Action handler)
            {
                _owner = owner;
                _handler = handler;
            }

            public Action Handler => _handler;

            // The subscriber's lifetime ended.
            public void End()
            {
                var owner = _owner;
                Release();
                owner?.Remove(this);
            }

            public void Release()
            {
                _handler = null;
                _owner = null;
            }
        }
    }
}
