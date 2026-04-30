using System.IO;
using UnityEngine;


namespace BionicLimb.Classification
{
    /// <summary>
    /// Selects between ML and rule-based classifier at startup.
    /// Spec §7.1
    /// </summary>
    public static class ClassifierFactory
    {
        private const string OnnxAssetPath = "Assets/ML/GestureClassifier.onnx";

        /// <summary>
        /// Returns MLGestureClassifier if a valid ONNX asset exists and loads cleanly;
        /// otherwise returns RuleBasedClassifier.
        /// </summary>
        public static IGestureClassifier Create(int windowSize, out string classifierMode)
        {
            var modelAsset = Resources.Load<Unity.InferenceEngine.ModelAsset>("ML/GestureClassifier");
            if (modelAsset != null)
            {
                try
                {
                    var ml = new MLGestureClassifier(modelAsset);
                    classifierMode = "ML Model";
                    Debug.Log("ClassifierFactory: Using ML gesture classifier (Sentis).");
                    return ml;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"ClassifierFactory: ML model load failed ({ex.Message}). Falling back to rule-based.");
                }
            }
            else
            {
                Debug.LogWarning("ClassifierFactory: GestureClassifier.onnx not found in Resources/ML/. Using rule-based fallback.");
            }

            classifierMode = "Rule-Based (fallback)";
            return new RuleBasedClassifier();
        }
    }
}
