using System;
using System.Collections.Generic;
using UnityEngine;
using BionicLimb.Hand;
using BionicLimb.Classification;

namespace BionicLimb.Core
{
    /// <summary>
    /// Drives EMG playback: loads a .myo.csv file, classifies each window,
    /// and drives the HandRig through GesturePoser at the original sample rate.
    /// Spec §4, §9.
    /// </summary>
    public class PlaybackController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("References")]
        public GesturePoser gesturePoser;

        [Header("Settings")]
        [Tooltip("Number of samples per classification window.")]
        public int windowSize = 40;

        // ── State machine ─────────────────────────────────────────────────────
        public enum PlaybackState { Idle, Loaded, Playing, Paused, Error }

        public PlaybackState State { get; private set; } = PlaybackState.Idle;
        public string ErrorMessage { get; private set; } = string.Empty;

        // ── Playback data ──────────────────────────────────────────────────────
        public EMGDataBuffer Buffer { get; private set; }
        public int CurrentSampleIndex { get; private set; }
        public string ClassifierMode { get; private set; } = string.Empty;

        // Current gesture output
        public GestureResult CurrentResult { get; private set; }

        // Pre-computed gesture-transition boundaries (sample indices)
        private int[] _stepBoundaries;
        private int _currentStep;

        // Accumulator-based timing
        private float _accumulator;
        private float _sampleInterval;

        // Preprocessor and classifier
        private IGestureClassifier _classifier;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action<GestureResult> OnGestureChanged;
        public event Action<float> OnProgressChanged;   // 0–1
        public event Action<PlaybackState> OnStateChanged;

        // ══════════════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Parse and preprocess a .myo.csv file, ready to Play().</summary>
        public void LoadFile(string filePath)
        {
            try
            {
                Buffer = MyoFileParser.Parse(filePath);
                EMGPreprocessor.ComputeNormalisationMaxima(Buffer);
                _sampleInterval = 1.0f / Buffer.SampleRate;

                _classifier = ClassifierFactory.Create(windowSize, out string mode);
                ClassifierMode = mode;

                _stepBoundaries = ComputeStepBoundaries(Buffer);
                CurrentSampleIndex = 0;
                _currentStep = 0;
                _accumulator = 0f;

                SetState(PlaybackState.Loaded);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                SetState(PlaybackState.Error);
                Debug.LogError($"[PlaybackController] LoadFile failed: {ex}");
            }
        }

        public void Play()
        {
            if (State == PlaybackState.Loaded || State == PlaybackState.Paused)
                SetState(PlaybackState.Playing);
        }

        public void Pause()
        {
            if (State == PlaybackState.Playing)
                SetState(PlaybackState.Paused);
        }

        public void Stop()
        {
            if (State == PlaybackState.Playing || State == PlaybackState.Paused)
            {
                CurrentSampleIndex = 0;
                _accumulator = 0f;
                _currentStep = 0;
                SetState(PlaybackState.Loaded);
                OnProgressChanged?.Invoke(0f);
            }
        }

        public void NextStep()
        {
            if (Buffer == null) return;
            int next = _currentStep + 1;
            if (next < _stepBoundaries.Length)
            {
                _currentStep = next;
                SeekToSample(_stepBoundaries[_currentStep]);
            }
        }

        public void PreviousStep()
        {
            if (Buffer == null) return;
            int prev = _currentStep - 1;
            if (prev >= 0)
            {
                _currentStep = prev;
                SeekToSample(_stepBoundaries[_currentStep]);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Unity lifecycle
        // ══════════════════════════════════════════════════════════════════════

        private void Update()
        {
            if (State != PlaybackState.Playing) return;

            _accumulator += Time.deltaTime;
            while (_accumulator >= _sampleInterval)
            {
                AdvanceSample();
                _accumulator -= _sampleInterval;
            }
        }

        private void OnDestroy()
        {
            _classifier?.Dispose();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Internal helpers
        // ══════════════════════════════════════════════════════════════════════

        private void AdvanceSample()
        {
            if (Buffer == null) return;

            CurrentSampleIndex++;
            if (CurrentSampleIndex >= Buffer.SampleCount)
            {
                CurrentSampleIndex = Buffer.SampleCount - 1;
                SetState(PlaybackState.Paused);
                OnProgressChanged?.Invoke(1f);
                return;
            }

            // Update step pointer
            while (_currentStep + 1 < _stepBoundaries.Length &&
                   CurrentSampleIndex >= _stepBoundaries[_currentStep + 1])
            {
                _currentStep++;
            }

            // Classify at the current window
            float[,] rawWindow = Buffer.GetWindowAt(CurrentSampleIndex, windowSize);
            float[,] processed = EMGPreprocessor.Process(rawWindow, Buffer.SampleRate);
            GestureResult result = _classifier.Classify(processed);

            CurrentResult = result;
            gesturePoser?.SetGestureResult(result);
            OnGestureChanged?.Invoke(result);
            OnProgressChanged?.Invoke((float)CurrentSampleIndex / (Buffer.SampleCount - 1));
        }

        private void SeekToSample(int sampleIndex)
        {
            CurrentSampleIndex = Mathf.Clamp(sampleIndex, 0, Buffer.SampleCount - 1);
            _accumulator = 0f;
            OnProgressChanged?.Invoke((float)CurrentSampleIndex / (Buffer.SampleCount - 1));
        }

        private void SetState(PlaybackState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Pre-computes the sample index of every gesture-label transition.
        /// Index 0 is always sample 0 (start of recording).
        /// </summary>
        private static int[] ComputeStepBoundaries(EMGDataBuffer buffer)
        {
            var boundaries = new List<int> { 0 };
            int lastGesture = buffer.GetSampleAt(0).GestureId;

            for (int i = 1; i < buffer.SampleCount; i++)
            {
                int g = buffer.GetSampleAt(i).GestureId;
                if (g != lastGesture)
                {
                    boundaries.Add(i);
                    lastGesture = g;
                }
            }
            return boundaries.ToArray();
        }
    }
}
