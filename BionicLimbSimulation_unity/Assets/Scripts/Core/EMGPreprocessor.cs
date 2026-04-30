using System;
using UnityEngine;

namespace BionicLimb.Core
{
    /// <summary>
    /// Static EMG signal preprocessing pipeline.
    /// Order: Band-pass → Full-wave rectification → RMS envelope → Normalisation.
    /// Spec §6.3
    /// </summary>
    public static class EMGPreprocessor
    {
        // Cached IIR coefficients keyed by sample rate
        private static int _cachedSampleRate = -1;
        private static double[] _b, _a;   // numerator / denominator (5 coefficients each for 4th order)

        // Per-channel max computed from the full buffer at load time
        private static float[] _channelMaxima;

        /// <summary>
        /// Compute normalisation maxima from the full buffer. Call once after file load.
        /// </summary>
        public static void ComputeNormalisationMaxima(EMGDataBuffer buffer)
        {
            _channelMaxima = new float[8];
            for (int c = 0; c < 8; c++) _channelMaxima[c] = 1e-6f; // avoid divide-by-zero
            for (int s = 0; s < buffer.SampleCount; s++)
                for (int c = 0; c < 8; c++)
                {
                    float v = Math.Abs(buffer.Channels[s, c]);
                    if (v > _channelMaxima[c]) _channelMaxima[c] = v;
                }
        }

        /// <summary>
        /// Apply full preprocessing pipeline to a raw [windowSize, 8] window.
        /// Returns processed [windowSize, 8].
        /// </summary>
        public static float[,] Process(float[,] rawWindow, int sampleRate)
        {
            EnsureCoefficients(sampleRate);
            int n = rawWindow.GetLength(0);
            int channels = rawWindow.GetLength(1);
            var result = new float[n, channels];

            for (int c = 0; c < channels; c++)
            {
                // Extract single-channel array
                double[] sig = new double[n];
                for (int i = 0; i < n; i++) sig[i] = rawWindow[i, c];

                // 1. Band-pass filter (4th-order Butterworth 20-450 Hz)
                sig = ApplyIIR(sig, _b, _a);

                // 2. Full-wave rectification
                for (int i = 0; i < n; i++) sig[i] = Math.Abs(sig[i]);

                // 3. RMS envelope (window = sampleRate / 10)
                int rmsWin = Math.Max(1, sampleRate / 10);
                sig = RmsEnvelope(sig, rmsWin);

                // 4. Normalisation
                float maxVal = (_channelMaxima != null && c < _channelMaxima.Length)
                    ? _channelMaxima[c] : 1f;

                for (int i = 0; i < n; i++)
                    result[i, c] = (float)(sig[i] / maxVal);
            }
            return result;
        }

        // -------------------------------------------------------------------
        // IIR filter: Direct-Form II Transposed, 4th-order (two biquad stages)
        // -------------------------------------------------------------------
        private static double[] ApplyIIR(double[] x, double[] b, double[] a)
        {
            // b and a each have 5 coefficients (order 4)
            // a[0] is assumed 1 (normalised)
            int n = x.Length;
            double[] y = new double[n];
            double[] w = new double[5];

            for (int i = 0; i < n; i++)
            {
                double xi = x[i];
                double yi = b[0] * xi + w[1];
                w[1] = b[1] * xi - a[1] * yi + w[2];
                w[2] = b[2] * xi - a[2] * yi + w[3];
                w[3] = b[3] * xi - a[3] * yi + w[4];
                w[4] = b[4] * xi - a[4] * yi;
                y[i] = yi;
            }
            return y;
        }

        private static double[] RmsEnvelope(double[] x, int windowLen)
        {
            int n = x.Length;
            double[] y = new double[n];
            for (int i = 0; i < n; i++)
            {
                int start = Math.Max(0, i - windowLen / 2);
                int end = Math.Min(n, i + windowLen / 2);
                double sum = 0;
                for (int j = start; j < end; j++) sum += x[j] * x[j];
                y[i] = Math.Sqrt(sum / (end - start));
            }
            return y;
        }

        // -------------------------------------------------------------------
        // 4th-order Butterworth bandpass 20–450 Hz
        // Coefficients computed via bilinear transform.
        // -------------------------------------------------------------------
        private static void EnsureCoefficients(int sampleRate)
        {
            if (sampleRate == _cachedSampleRate) return;
            _cachedSampleRate = sampleRate;
            ComputeButterworthBandpass(20.0, 450.0, sampleRate, out _b, out _a);
        }

        /// <summary>
        /// Analytic 4th-order Butterworth bandpass via bilinear transform.
        /// Produces 5-tap IIR coefficients (b, a) suitable for the DF-II transposed filter above.
        /// Reference: https://en.wikipedia.org/wiki/Butterworth_filter
        /// </summary>
        private static void ComputeButterworthBandpass(double lowHz, double highHz, int fs, out double[] b, out double[] a)
        {
            // Pre-warp frequencies
            double T = 1.0 / fs;
            double wLow  = 2.0 / T * Math.Tan(Math.PI * lowHz  * T);
            double wHigh = 2.0 / T * Math.Tan(Math.PI * highHz * T);
            double bw = wHigh - wLow;
            double w0 = Math.Sqrt(wLow * wHigh);

            // 2nd-order analogue bandpass prototype → bilinear → 4th-order digital
            // Implemented as cascade of two 2nd-order sections combined into one 4th-order.
            // For simplicity and correctness, compute coefficients for each biquad then convolve.
            double[] b1, a1, b2, a2;
            BiquadBandpass(w0, bw / 2.0, T, out b1, out a1);
            BiquadBandpass(w0, bw / 2.0, T, out b2, out a2);
            b = PolyConvolve(b1, b2);
            a = PolyConvolve(a1, a2);
        }

        private static void BiquadBandpass(double w0, double halfBw, double T, out double[] b, out double[] a)
        {
            // Analogue 1st-order LP → BP transform → bilinear
            // s-domain: H(s) = (halfBw * s) / (s^2 + halfBw*s + w0^2)
            // Bilinear: s = 2/T * (z-1)/(z+1)
            double K = 2.0 / T;
            double K2 = K * K;
            double A0 = K2 + halfBw * K + w0 * w0;

            // Numerator: halfBw * K * (z - z^{-1}) → [halfBw*K, 0, -halfBw*K]
            double nb0 =  halfBw * K / A0;
            double nb1 =  0.0;
            double nb2 = -halfBw * K / A0;

            // Denominator coefficients (a0 normalised to 1)
            double da1 = (2.0 * (w0 * w0 - K2)) / A0;
            double da2 = (K2 - halfBw * K + w0 * w0) / A0;

            b = new[] { nb0, nb1, nb2 };
            a = new[] { 1.0, da1, da2 };
        }

        private static double[] PolyConvolve(double[] p, double[] q)
        {
            int n = p.Length + q.Length - 1;
            double[] r = new double[n];
            for (int i = 0; i < p.Length; i++)
                for (int j = 0; j < q.Length; j++)
                    r[i + j] += p[i] * q[j];
            return r;
        }
    }
}
