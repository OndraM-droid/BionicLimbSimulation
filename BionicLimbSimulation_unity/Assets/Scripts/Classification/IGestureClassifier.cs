using System;

namespace BionicLimb.Classification
{
    /// <summary>
    /// Shared classifier interface. Spec §7.2
    /// </summary>
    public interface IGestureClassifier : IDisposable
    {
        /// <summary>Classify a preprocessed EMG window. window: [windowSize, 8]</summary>
        GestureResult Classify(float[,] window);

        string[] GestureNames { get; }
    }

    public struct GestureResult
    {
        public int GestureId;
        public string GestureName;
        public float Confidence;          // 0–1
        public float[] AllProbabilities;  // length = num_gestures
    }
}
