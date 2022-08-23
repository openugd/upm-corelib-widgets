// © 2025 OpenUGD

using OpenUGD;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    public static class UnityEventExtensions
    {
        public static void Subscribe(
            this UnityEvent unityEvent,
            Lifetime lifetime,
            UnityAction listener
        )
        {
            unityEvent.AddListener(listener);
            lifetime.AddAction(() => { unityEvent.RemoveListener(listener); });
        }

        public static void Subscribe<T0>(
            this UnityEvent<T0> unityEvent,
            Lifetime lifetime,
            UnityAction<T0> listener
        )
        {
            unityEvent.AddListener(listener);
            lifetime.AddAction(() => { unityEvent.RemoveListener(listener); });
        }

        public static void Subscribe<T0, T1>(
            this UnityEvent<T0, T1> unityEvent,
            Lifetime lifetime,
            UnityAction<T0, T1> listener
        )
        {
            unityEvent.AddListener(listener);
            lifetime.AddAction(() => { unityEvent.RemoveListener(listener); });
        }

        public static void Subscribe<T0, T1, T2>(
            this UnityEvent<T0, T1, T2> unityEvent,
            Lifetime lifetime,
            UnityAction<T0, T1, T2> listener
        )
        {
            unityEvent.AddListener(listener);
            lifetime.AddAction(() => { unityEvent.RemoveListener(listener); });
        }

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
