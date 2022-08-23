using System;
using System.Collections;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Core.Widgets
{
    public static class TMPWidgetIntervalUpdateExtensions
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);

        public static TMPWidget WithIntervalUpdate(this TMPWidget parent, Func<IDisposable, string> text)
        {
            return parent.WithIntervalUpdate((disposable) => (TextModel)text(disposable), DefaultInterval);
        }

        public static TMPWidget WithIntervalUpdate(this TMPWidget parent, Func<IDisposable, string> text,
            TimeSpan interval)
        {
            return parent.WithIntervalUpdate((disposable) => (TextModel)text(disposable), interval);
        }

        public static TMPWidget WithIntervalUpdate(this TMPWidget parent, Func<IDisposable, TextModel> text)
        {
            return parent.WithIntervalUpdate(text, DefaultInterval);
        }

        public static TMPWidget WithIntervalUpdate(this TMPWidget parent, Func<IDisposable, TextModel> text,
            TimeSpan interval)
        {
            IEnumerator IntervalCoroutine(Lifetime lifetime, IDisposable df)
            {
                while (!lifetime.IsTerminated)
                {
                    parent.SetModel(text(df));
                    yield return new WaitForSeconds((float)interval.TotalSeconds);
                }
            }

            var df = parent.Lifetime.DefineNested();
            var coroutineProvider = parent.Resolve<ICoroutineProvider>();
            var coroutine = coroutineProvider.StartCoroutine(IntervalCoroutine(df.Lifetime, df));
            if (coroutine != null)
            {
                df.Lifetime.AddAction(() => coroutineProvider.StopCoroutine(coroutine));
            }

            return parent;
        }
    }
}
