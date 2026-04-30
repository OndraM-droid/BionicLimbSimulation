using System;

namespace BionicLimb.Core
{
    /// <summary>
    /// Plain C# data container for a loaded .myo.csv recording.
    /// Spec §6.2
    /// </summary>
    public class EMGDataBuffer
    {
        public double[] Timestamps { get; }       // millisecond offsets
        public float[,] Channels { get; }          // [sampleCount, 8]
        public string[] GestureLabels { get; }     // per-sample gesture name
        public int[] GestureIds { get; }            // per-sample gesture id
        public int SampleRate { get; }
        public int SampleCount { get; }

        public EMGDataBuffer(
            double[] timestamps,
            float[,] channels,
            string[] gestureLabels,
            int[] gestureIds,
            int sampleRate)
        {
            Timestamps = timestamps;
            Channels = channels;
            GestureLabels = gestureLabels;
            GestureIds = gestureIds;
            SampleRate = sampleRate;
            SampleCount = timestamps.Length;
        }

        /// <summary>Returns all fields for a single sample index.</summary>
        public EMGSample GetSampleAt(int index)
        {
            if (index < 0 || index >= SampleCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            float[] ch = new float[8];
            for (int i = 0; i < 8; i++) ch[i] = Channels[index, i];

            return new EMGSample
            {
                Index = index,
                TimestampMs = Timestamps[index],
                Channels = ch,
                GestureLabel = GestureLabels[index],
                GestureId = GestureIds[index]
            };
        }

        /// <summary>
        /// Returns a [windowSize, 8] slice centred on centerIndex.
        /// Pads with zeros if window extends past buffer edges.
        /// Used by classifiers (§6.2).
        /// </summary>
        public float[,] GetWindowAt(int centerIndex, int windowSize)
        {
            var window = new float[windowSize, 8];
            int half = windowSize / 2;
            for (int w = 0; w < windowSize; w++)
            {
                int src = centerIndex - half + w;
                if (src < 0 || src >= SampleCount) continue;
                for (int c = 0; c < 8; c++)
                    window[w, c] = Channels[src, c];
            }
            return window;
        }
    }

    public struct EMGSample
    {
        public int Index;
        public double TimestampMs;
        public float[] Channels;
        public string GestureLabel;
        public int GestureId;
    }
}
