using System.Threading;
using System.Threading.Tasks;
using OpenUGD.Core;
using OpenUGD.Presenters;
using UnityEngine;

namespace OpenUGD.Samples.SettingsScreen
{
    /// <summary>
    /// The entry point. Put it on an empty GameObject and enter Play mode: it boots a context with a two-language
    /// localisation, builds a settings screen in code, and binds it with the widgets presenters.
    /// </summary>
    [AddComponentMenu("OpenUGD/Samples/Settings Screen")]
    public sealed class SettingsScreenSample : ContextBehaviour
    {
        /// <inheritdoc />
        protected override bool PersistAcrossScenes => false;

        /// <inheritdoc />
        protected override Task<Context> CreateContextAsync(CancellationToken cancellationToken)
        {
            var builder = Context.CreateBuilder(Lifetime);

            // One object under two contracts: the text presenters inject the interface, the screen injects the
            // class to switch the language.
            builder.Services.AddInstance(new SampleLocalization()).As<ILocalization>();
            builder.Services.AddInstance(new LanguageChanged(Lifetime)).As<ILocalizationChanged>();

            return builder.BuildAsync(cancellationToken);
        }

        /// <inheritdoc />
        protected override void OnStarted(Context context)
        {
            var view = SettingsScreenView.Create(transform);

            // The tree ends with this component's Lifetime: destroying the GameObject closes every presenter and
            // removes every listener they added.
            var root = new Presenter.Root(Lifetime, new ContextPresenterFactory(context));
            var screen = root.AddPresenter(new SettingsScreenPresenter());
            screen.SetModel(new Settings());
            screen.SetView(view);
        }
    }
}
