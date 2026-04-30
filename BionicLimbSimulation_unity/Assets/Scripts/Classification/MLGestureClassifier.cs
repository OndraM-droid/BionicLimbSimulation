using System;
using UnityEngine;


namespace BionicLimb.Classification
{
    /// <summary>
    /// ML gesture classifier using Unity Sentis ONNX model.
    /// Input: [windowSize, 8] preprocessed EMG window.
    /// Internally extracts 32 time-domain features (MAV, RMS, ZCR, WL × 8 channels),
    /// applies z-score normalisation using parameters saved alongside the ONNX,
    /// then runs the MLP model.
    /// Spec §7.3
    /// </summary>
    public class MLGestureClassifier : IGestureClassifier, IDisposable
    {
        private const int NumChannels   = 8;
        private const int NumFeatures   = 32; // 4 features × 8 channels

        private static readonly string[] Names =
            { "rest", "fist", "open_hand", "pinch", "point", "thumbs_up", "peace", "ok" };

        public string[] GestureNames => Names;

        private readonly Unity.InferenceEngine.Worker _worker;
        private bool _disposed;

        // Scaler parameters loaded from Resources/ML/GestureClassifier_scaler.json
        private float[] _scalerMean;
        private float[] _scalerScale;

        public MLGestureClassifier(Unity.InferenceEngine.ModelAsset modelAsset)
        {
            var model = Unity.InferenceEngine.ModelLoader.Load(modelAsset);
            var backend = SystemInfo.supportsComputeShaders
                ? Unity.InferenceEngine.BackendType.GPUCompute
                : Unity.InferenceEngine.BackendType.CPU;
            _worker = new Unity.InferenceEngine.Worker(model, backend);

            LoadScaler();
        }

        public GestureResult Classify(float[,] window)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MLGestureClassifier));

            float[] features = ExtractFeatures(window);
            ApplyScaler(features);

            Unity.InferenceEngine.Tensor<float> inputTensor  = null;
            Unity.InferenceEngine.Tensor<float> outputTensor = null;
            try
            {
                inputTensor = new Unity.InferenceEngine.Tensor<float>(
                    new Unity.InferenceEngine.TensorShape(1, NumFeatures), features);
                _worker.SetInput(0, inputTensor);
                _worker.Schedule();
                outputTensor = _worker.PeekOutput() as Unity.InferenceEngine.Tensor<float>;
                outputTensor.CompleteAllPendingOperations();

                int numClasses = outputTensor.shape[1];
                float[] probs = Softmax(outputTensor, numClasses);

                int bestId = 0;
                for (int i = 1; i < numClasses; i++)
                    if (probs[i] > probs[bestId]) bestId = i;

                return new GestureResult
                {
                    GestureId       = bestId,
                    GestureName     = bestId < Names.Length ? Names[bestId] : bestId.ToString(),
                    Confidence      = probs[bestId],
                    AllProbabilities = probs
                };
            }
            finally
            {
                inputTensor?.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _worker?.Dispose();
        }

        // ── Feature extraction ─────────────────────────────────────────────────
        // Mirrors train_gesture_model.py's extract_features: MAV, RMS, ZCR, WL per channel.

        private static float[] ExtractFeatures(float[,] window)
        {
            int n = window.GetLength(0);
            float[] feats = new float[NumFeatures]; // [ch0_MAV, ch0_RMS, ch0_ZCR, ch0_WL, ch1_MAV, ...]

            for (int c = 0; c < NumChannels; c++)
            {
                float sumAbs = 0f, sumSq = 0f, wl = 0f;
                int zc = 0;
                float prev = window[0, c];

                for (int i = 0; i < n; i++)
                {
                    float v = window[i, c];
                    sumAbs += Mathf.Abs(v);
                    sumSq  += v * v;
                    if (i > 0)
                    {
                        if (Mathf.Sign(v) != Mathf.Sign(prev)) zc++;
                        wl += Mathf.Abs(v - prev);
                        prev = v;
                    }
                }

                int idx = c * 4;
                feats[idx + 0] = sumAbs / n;                        // MAV
                feats[idx + 1] = Mathf.Sqrt(sumSq / n);             // RMS
                feats[idx + 2] = n > 1 ? (float)zc / (n - 1) : 0f; // ZCR
                feats[idx + 3] = wl;                                 // WL
            }
            return feats;
        }

        // ── Scaler ────────────────────────────────────────────────────────────

        private void LoadScaler()
        {
            var json = Resources.Load<TextAsset>("ML/GestureClassifier_scaler");
            if (json == null)
            {
                Debug.LogWarning("[MLGestureClassifier] Scaler JSON not found at Resources/ML/GestureClassifier_scaler.json — features will not be normalised.");
                _scalerMean  = new float[NumFeatures];
                _scalerScale = new float[NumFeatures];
                for (int i = 0; i < NumFeatures; i++) _scalerScale[i] = 1f;
                return;
            }

            ScalerData data = JsonUtility.FromJson<ScalerData>(json.text);
            _scalerMean  = data.mean;
            _scalerScale = data.scale;
        }

        private void ApplyScaler(float[] features)
        {
            if (_scalerMean == null) return;
            for (int i = 0; i < features.Length; i++)
                features[i] = (features[i] - _scalerMean[i]) / Mathf.Max(_scalerScale[i], 1e-8f);
        }

        // ── Softmax ───────────────────────────────────────────────────────────

        private static float[] Softmax(Unity.InferenceEngine.Tensor<float> t, int n)
        {
            float[] raw = new float[n];
            float max = float.MinValue;
            for (int i = 0; i < n; i++) { raw[i] = t[0, i]; if (raw[i] > max) max = raw[i]; }
            float sum = 0f;
            for (int i = 0; i < n; i++) { raw[i] = Mathf.Exp(raw[i] - max); sum += raw[i]; }
            for (int i = 0; i < n; i++) raw[i] /= sum;
            return raw;
        }

        [Serializable]
        private class ScalerData
        {
            public float[] mean;
            public float[] scale;
        }
    }
}
