using System;
using UnityEngine;
using Unity.Sentis;

namespace BionicLimb.Classification
{
    /// <summary>
    /// ML gesture classifier using Unity Sentis + ONNX model.
    /// Spec §7.3
    /// Falls back gracefully — CallerFactory handles selection.
    /// </summary>
    public class MLGestureClassifier : IGestureClassifier, IDisposable
    {
        private static readonly string[] Names =
            { "rest", "fist", "open_hand", "pinch", "point", "thumbs_up", "peace", "ok" };

        public string[] GestureNames => Names;

        private readonly Worker _worker;
        private readonly int _windowSize;
        private bool _disposed;

        public MLGestureClassifier(ModelAsset modelAsset, int windowSize)
        {
            _windowSize = windowSize;
            var model = ModelLoader.Load(modelAsset);
            var backend = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute : BackendType.CPU;
            _worker = new Worker(model, backend);
        }

        public GestureResult Classify(float[,] window)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MLGestureClassifier));

            // Flatten [windowSize, 8] → [1, windowSize * 8] feature vector
            int featureLen = _windowSize * 8;
            float[] flat = new float[featureLen];
            for (int i = 0; i < _windowSize; i++)
                for (int c = 0; c < 8; c++)
                    flat[i * 8 + c] = window[i, c];

            TensorFloat inputTensor = null;
            TensorFloat outputTensor = null;
            try
            {
                inputTensor = new TensorFloat(new TensorShape(1, featureLen), flat);
                _worker.Schedule(inputTensor);
                outputTensor = _worker.PeekOutput() as TensorFloat;
                outputTensor.MakeReadable();

                int numClasses = outputTensor.shape[1];
                float[] probs = Softmax(outputTensor, numClasses);

                int bestId = 0;
                for (int i = 1; i < numClasses; i++)
                    if (probs[i] > probs[bestId]) bestId = i;

                return new GestureResult
                {
                    GestureId = bestId,
                    GestureName = bestId < Names.Length ? Names[bestId] : bestId.ToString(),
                    Confidence = probs[bestId],
                    AllProbabilities = probs
                };
            }
            finally
            {
                inputTensor?.Dispose();
                // outputTensor is owned by worker (PeekOutput), do not dispose separately
            }
        }

        private static float[] Softmax(TensorFloat t, int n)
        {
            float[] raw = new float[n];
            float max = float.MinValue;
            for (int i = 0; i < n; i++) { raw[i] = t[0, i]; if (raw[i] > max) max = raw[i]; }
            float sum = 0f;
            for (int i = 0; i < n; i++) { raw[i] = Mathf.Exp(raw[i] - max); sum += raw[i]; }
            for (int i = 0; i < n; i++) raw[i] /= sum;
            return raw;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _worker?.Dispose();
        }
    }
}
