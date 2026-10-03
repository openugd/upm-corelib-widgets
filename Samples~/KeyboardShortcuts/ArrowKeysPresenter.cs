using System;
using OpenUGD.Presenters;
using OpenUGD.Utils;
using UnityEngine;

namespace OpenUGD.Samples.KeyboardShortcuts
{
    /// <summary>
    /// Arrow-key controls for a game screen, written the 2.0.0 way. Attach it under the presenter that owns
    /// the screen; the shortcuts end when either of them closes.
    /// </summary>
    /// <example>
    /// <code>
    /// AddPresenter(new ArrowKeysPresenter(direction => controller.Steer(direction)));
    /// </code>
    /// </example>
    public sealed class ArrowKeysPresenter : Presenter
    {
        // Required: attaching throws if the context has no ICoroutineProvider. ContextBehaviour is one -
        // register it with builder.Services.AddInstance<ICoroutineProvider>(this).
        [Inject] private ICoroutineProvider _coroutines;

        private readonly Action<Vector2Int> _onArrow;

        /// <summary>Creates the presenter. Nothing is polled until it is attached.</summary>
        /// <param name="onArrow">Receives the direction of each arrow key pressed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="onArrow"/> is <c>null</c>.</exception>
        public ArrowKeysPresenter(Action<Vector2Int> onArrow)
        {
            _onArrow = onArrow ?? throw new ArgumentNullException(nameof(onArrow));
        }

        /// <inheritdoc />
        protected override void OnInitialize()
        {
            // 0.5.0: this.AddKeyboard(KeyCode.LeftArrow, onKeyDown: () => ...);
            this.SubscribeOnKeyDown(_coroutines, KeyCode.LeftArrow, () => _onArrow(Vector2Int.left));
            this.SubscribeOnKeyDown(_coroutines, KeyCode.RightArrow, () => _onArrow(Vector2Int.right));
            this.SubscribeOnKeyDown(_coroutines, KeyCode.UpArrow, () => _onArrow(Vector2Int.up));
            this.SubscribeOnKeyDown(_coroutines, KeyCode.DownArrow, () => _onArrow(Vector2Int.down));
        }
    }
}
