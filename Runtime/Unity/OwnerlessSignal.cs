using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace OpenUGD.UI
{
    // An ISignal with no Lifetime of its own, behind UIGestureDetector's signals, which must take subscriptions
    // before the detector is first active. A Signal needs an owning Lifetime: one created before Awake would be
    // ended only by an OnDestroy that Unity never sends to an object that was never active, and corelib's
    // GetLifetime/LifetimeBehaviour refuse such an object for that reason. Here each registration is held by this
    // object and ended by the subscriber's lifetime, so nothing roots a never-activated detector. Close() is the
    // owner's OnDestroy: it drops every handler and refuses new ones.
    //
    // Otherwise Signal's semantics: subscription order; a dispatch skips registrations that end during it; duplicate
    // handlers allowed; a throwing handler does not stop the others, and failures are rethrown at the end, one as
    // itself, two or more as an AggregateException. Fire on the main thread; Subscribe and the end of a registration
    // from any thread.
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
