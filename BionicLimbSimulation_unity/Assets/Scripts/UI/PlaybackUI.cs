using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using BionicLimb.Core;

namespace BionicLimb.UI
{
    /// <summary>
    /// Binds PlaybackPanel.uxml controls to PlaybackController.
    /// Spec §10.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlaybackUI : MonoBehaviour
    {
        [Header("References")]
        public PlaybackController playbackController;

        // ── UXML element references ───────────────────────────────────────────
        private Label         _gestureLabel;
        private Label         _classifierModeLabel;
        private ProgressBar   _progressBar;
        private Button        _playPauseButton;
        private Button        _prevStepButton;
        private Button        _nextStepButton;
        private Button        _importButton;
        private Label         _errorLabel;

        // ══════════════════════════════════════════════════════════════════════
        // Unity lifecycle
        // ══════════════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            _gestureLabel        = root.Q<Label>("GestureLabel");
            _classifierModeLabel = root.Q<Label>("ClassifierModeLabel");
            _progressBar         = root.Q<ProgressBar>("ProgressBar");
            _playPauseButton     = root.Q<Button>("PlayPauseButton");
            _prevStepButton      = root.Q<Button>("PrevStepButton");
            _nextStepButton      = root.Q<Button>("NextStepButton");
            _importButton        = root.Q<Button>("ImportButton");
            _errorLabel          = root.Q<Label>("ErrorLabel");

            _playPauseButton.clicked += OnPlayPauseClicked;
            _prevStepButton.clicked  += OnPrevStepClicked;
            _nextStepButton.clicked  += OnNextStepClicked;
            _importButton.clicked    += OnImportClicked;

            if (playbackController != null)
            {
                playbackController.OnGestureChanged  += OnGestureChanged;
                playbackController.OnProgressChanged += OnProgressChanged;
                playbackController.OnStateChanged    += OnStateChanged;
            }

            RefreshControls(PlaybackController.PlaybackState.Idle);
        }

        private void OnDisable()
        {
            if (playbackController != null)
            {
                playbackController.OnGestureChanged  -= OnGestureChanged;
                playbackController.OnProgressChanged -= OnProgressChanged;
                playbackController.OnStateChanged    -= OnStateChanged;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Button handlers
        // ══════════════════════════════════════════════════════════════════════

        private void OnPlayPauseClicked()
        {
            if (playbackController == null) return;
            if (playbackController.State == PlaybackController.PlaybackState.Playing)
                playbackController.Pause();
            else
                playbackController.Play();
        }

        private void OnPrevStepClicked()  => playbackController?.PreviousStep();
        private void OnNextStepClicked()  => playbackController?.NextStep();

        private void OnImportClicked()
        {
            string path = PickFile();
            if (!string.IsNullOrEmpty(path))
                playbackController?.LoadFile(path);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Event handlers
        // ══════════════════════════════════════════════════════════════════════

        private void OnGestureChanged(Classification.GestureResult result)
        {
            _gestureLabel.text = $"Gesture: {result.GestureName}  ({result.Confidence * 100f:F0}%)";
        }

        private void OnProgressChanged(float progress)
        {
            _progressBar.value = progress;
        }

        private void OnStateChanged(PlaybackController.PlaybackState state)
        {
            RefreshControls(state);
        }

        // ══════════════════════════════════════════════════════════════════════
        // UI helpers
        // ══════════════════════════════════════════════════════════════════════

        private void RefreshControls(PlaybackController.PlaybackState state)
        {
            bool hasFile = state != PlaybackController.PlaybackState.Idle &&
                           state != PlaybackController.PlaybackState.Error;
            bool isPlaying = state == PlaybackController.PlaybackState.Playing;

            _playPauseButton.SetEnabled(hasFile);
            _prevStepButton.SetEnabled(hasFile);
            _nextStepButton.SetEnabled(hasFile);
            _playPauseButton.text = isPlaying ? "⏸" : "▶";

            if (state == PlaybackController.PlaybackState.Error && playbackController != null)
            {
                _errorLabel.text = playbackController.ErrorMessage;
                _errorLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                _errorLabel.style.display = DisplayStyle.None;
            }

            if (playbackController != null && !string.IsNullOrEmpty(playbackController.ClassifierMode))
                _classifierModeLabel.text = $"Classifier: {playbackController.ClassifierMode}";
        }

        /// <summary>
        /// Returns a file path to load.
        /// In Editor → native OS file picker.
        /// In build → first .myo.csv file found in StreamingAssets/SampleData.
        /// </summary>
        private static string PickFile()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.OpenFilePanel(
                "Open EMG Recording", Application.streamingAssetsPath, "csv");
#else
            string dir = Path.Combine(Application.streamingAssetsPath, "SampleData");
            if (!Directory.Exists(dir)) return string.Empty;
            string[] files = Directory.GetFiles(dir, "*.myo.csv");
            return files.Length > 0 ? files[0] : string.Empty;
#endif
        }
    }
}
