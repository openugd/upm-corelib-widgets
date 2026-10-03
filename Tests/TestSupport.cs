using System;
using System.Threading;
using NUnit.Framework;
using OpenUGD.Presenters;

namespace OpenUGD.Widgets.Tests
{
    // The smallest IPresenterFactory: constructs with Activator and injects nothing. Enough for every presenter
    // here whose [Inject] members are optional.
    internal sealed class PlainFactory : IPresenterFactory
    {
        public Presenter Create(Type presenterType) => (Presenter)Activator.CreateInstance(presenterType);

        public void Inject(Presenter presenter)
        {
        }
    }

    // A presenter tree per test, on a scope the fixture ends in TearDown.
    public abstract class PresenterFixture
    {
        private Lifetime.Definition _definition;

        protected Presenter Root { get; private set; }

        protected Lifetime TestLifetime => _definition.Lifetime;

        [SetUp]
        public void CreateRoot()
        {
            _definition = Lifetime.Eternal.DefineNested("test");
            Root = new Presenter.Root(_definition.Lifetime, new PlainFactory());
        }

        [TearDown]
        public void EndRoot() => _definition.Terminate();
    }

    // Records ILocalization lookups; translates by prefixing, so a test can tell translated text from raw text.
    internal sealed class PrefixLocalization : ILocalization
    {
        public string Prefix = "t:";

        public string Get(string key) => Prefix + key;
    }

    // The implementation ILocalizationChanged's docs recommend: a Signal that also declares the interface.
    internal sealed class LanguageChanged : Signal, ILocalizationChanged
    {
        public LanguageChanged(Lifetime lifetime) : base(lifetime)
        {
        }
    }

    internal static class Contexts
    {
        // Builds a real OpenUGD.Context on the test thread. BuildAsync completes on the thread pool when there is
        // no synchronization context, so blocking on it cannot deadlock.
        public static Context Build(Lifetime lifetime, Action<ContextBuilder> register)
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                var builder = Context.CreateBuilder(lifetime);
                register(builder);
                var task = builder.BuildAsync();
                if (!task.Wait(15000)) Assert.Fail("the context did not build within 15 s");
                return task.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }
    }
}
