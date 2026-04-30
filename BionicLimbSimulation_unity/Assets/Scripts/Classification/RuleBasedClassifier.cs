using System;
using System.Collections.Generic;
using UnityEngine;

namespace BionicLimb.Classification
{
    /// <summary>
    /// Rule-based fallback classifier using channel RMS thresholds.
    /// Spec §7.4
    /// </summary>
    public class RuleBasedClassifier : IGestureClassifier
    {
        private static readonly string[] Names =
            { "rest", "fist", "open_hand", "pinch", "point", "thumbs_up", "peace", "ok" };

        public string[] GestureNames => Names;

        // Threshold rules: (primary condition, secondary condition)
        // Each entry: Func<float[], bool> for primary, Func<float[], bool> for secondary
        private static readonly (Func<float[], bool> primary, Func<float[], bool> secondary)[] Rules =
        {
            // 0 rest
            (ch => ch[0]<0.15f && ch[1]<0.15f && ch[2]<0.15f && ch[3]<0.15f &&
                   ch[4]<0.15f && ch[5]<0.15f && ch[6]<0.15f && ch[7]<0.15f, _ => true),
            // 1 fist
            (ch => ch[0]>0.4f && ch[1]>0.35f, ch => ch[2]<0.2f),
            // 2 open_hand
            (ch => ch[2]>0.4f && ch[3]>0.35f, ch => ch[0]<0.2f),
            // 3 pinch
            (ch => ch[4]>0.4f && ch[0]>0.3f, ch => ch[2]<0.25f),
            // 4 point
            (ch => ch[2]>0.35f && ch[0]>0.3f, ch => ch[4]<0.2f),
            // 5 thumbs_up
            (ch => ch[4]>0.45f && ch[5]>0.3f, ch => ch[0]<0.2f),
            // 6 peace
            (ch => ch[2]>0.4f && ch[0]>0.25f, ch => ch[4]<0.2f),
            // 7 ok
            (ch => ch[4]>0.4f && ch[0]>0.3f, ch => ch[2]>0.2f),
        };

        public GestureResult Classify(float[,] window)
        {
            float[] rms = ComputeWindowRms(window);

            // Find first matching rule (rest checked last after others)
            int winner = 0; // default rest
            for (int g = 1; g < Rules.Length; g++)
            {
                if (Rules[g].primary(rms) && Rules[g].secondary(rms))
                {
                    winner = g;
                    break;
                }
            }
            // Explicitly check rest as override if all channels low
            if (Rules[0].primary(rms)) winner = 0;

            // Confidence = ratio of rule-matching channels
            float confidence = ComputeConfidence(rms, winner);

            float[] probs = new float[Names.Length];
            probs[winner] = confidence;
            float remaining = (1f - confidence) / (Names.Length - 1);
            for (int i = 0; i < Names.Length; i++)
                if (i != winner) probs[i] = remaining;

            return new GestureResult
            {
                GestureId = winner,
                GestureName = Names[winner],
                Confidence = confidence,
                AllProbabilities = probs
            };
        }

        private static float[] ComputeWindowRms(float[,] window)
        {
            int n = window.GetLength(0);
            float[] rms = new float[8];
            for (int c = 0; c < 8; c++)
            {
                float sum = 0f;
                for (int i = 0; i < n; i++) sum += window[i, c] * window[i, c];
                rms[c] = Mathf.Sqrt(sum / n);
            }
            return rms;
        }

        private static float ComputeConfidence(float[] rms, int gestureId)
        {
            if (gestureId == 0) // rest: confidence based on how low all channels are
            {
                float maxVal = 0f;
                foreach (var v in rms) maxVal = Mathf.Max(maxVal, v);
                return Mathf.Clamp01(1f - maxVal / 0.15f);
            }
            // Count channels that are "active" (above 0.1) and compare to expected
            int activeCount = 0;
            for (int c = 0; c < 8; c++) if (rms[c] > 0.1f) activeCount++;
            return Mathf.Clamp01(activeCount / 4f); // normalised to rough expected active count
        }

        public void Dispose() { }
    }
}
