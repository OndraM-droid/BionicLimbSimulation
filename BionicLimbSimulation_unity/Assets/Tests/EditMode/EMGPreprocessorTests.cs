using NUnit.Framework;
using BionicLimb.Core;

namespace BionicLimb.Tests
{
    /// <summary>
    /// EditMode tests for EMGPreprocessor.
    /// Verifies band-pass, rectification, envelope, normalisation pipeline.
    /// </summary>
    public class EMGPreprocessorTests
    {
        private const int SampleRate = 200;
        private const int WindowSize = 40;
        private const int NumChannels = 8;

        private static float[,] MakeWindow(float value = 0.5f)
        {
            var w = new float[WindowSize, NumChannels];
            for (int i = 0; i < WindowSize; i++)
                for (int c = 0; c < NumChannels; c++)
                    w[i, c] = value;
            return w;
        }

        private static EMGDataBuffer MakeBuffer(float value = 0.5f, int samples = 100)
        {
            float[,] channels = new float[samples, NumChannels];
            int[] gestureIds = new int[samples];
            double[] timestamps = new double[samples];
            string[] labels = { "rest" };
            for (int i = 0; i < samples; i++)
            {
                timestamps[i] = i * 5.0;
                for (int c = 0; c < NumChannels; c++)
                    channels[i, c] = value;
            }
            return new EMGDataBuffer(timestamps, channels, labels, gestureIds, SampleRate);
        }

        [Test]
        public void Process_ConstantSignal_ReturnsNonNegative()
        {
            var buf = MakeBuffer(0.5f);
            EMGPreprocessor.ComputeNormalisationMaxima(buf);
            var window = MakeWindow(0.5f);
            var result = EMGPreprocessor.Process(window, SampleRate);

            for (int i = 0; i < WindowSize; i++)
                for (int c = 0; c < NumChannels; c++)
                    Assert.GreaterOrEqual(result[i, c], 0f, $"Negative value at [{i},{c}]");
        }

        [Test]
        public void Process_OutputShape_MatchesInput()
        {
            var buf = MakeBuffer();
            EMGPreprocessor.ComputeNormalisationMaxima(buf);
            var window = MakeWindow();
            var result = EMGPreprocessor.Process(window, SampleRate);

            Assert.AreEqual(WindowSize, result.GetLength(0));
            Assert.AreEqual(NumChannels, result.GetLength(1));
        }

        [Test]
        public void Process_NormalisedOutput_BoundedAboveByOne()
        {
            var buf = MakeBuffer(0.8f);
            EMGPreprocessor.ComputeNormalisationMaxima(buf);
            var window = MakeWindow(0.8f);
            var result = EMGPreprocessor.Process(window, SampleRate);

            for (int i = 0; i < WindowSize; i++)
                for (int c = 0; c < NumChannels; c++)
                    Assert.LessOrEqual(result[i, c], 1.01f, $"Value > 1 at [{i},{c}]");
        }

        [Test]
        public void Process_ZeroInput_ReturnsZero()
        {
            var buf = MakeBuffer(0f);
            EMGPreprocessor.ComputeNormalisationMaxima(buf);
            var window = MakeWindow(0f);
            var result = EMGPreprocessor.Process(window, SampleRate);

            for (int i = 0; i < WindowSize; i++)
                for (int c = 0; c < NumChannels; c++)
                    Assert.AreEqual(0f, result[i, c], 1e-5f);
        }
    }
}
