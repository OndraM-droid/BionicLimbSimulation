import type { EMGDataBuffer } from './EMGDataBuffer.js';

interface ButterworthCoeffs {
  b: Float64Array;
  a: Float64Array;
}

const coeffsCache = new Map<number, ButterworthCoeffs>();

function convolve(a: number[], b: number[]): number[] {
  const result = new Array(a.length + b.length - 1).fill(0) as number[];
  for (let i = 0; i < a.length; i++) {
    for (let j = 0; j < b.length; j++) {
      result[i + j] += a[i] * b[j];
    }
  }
  return result;
}

function computeButterworthBandpass(lowHz: number, highHz: number, fs: number): ButterworthCoeffs {
  const T = 1 / fs;
  const clippedHigh = Math.min(highHz, (fs / 2) * 0.99);
  const wLow = (2 / T) * Math.tan(Math.PI * lowHz * T);
  const wHigh = (2 / T) * Math.tan(Math.PI * clippedHigh * T);
  const bw = wHigh - wLow;
  const w0 = Math.sqrt(wLow * wHigh);
  const halfBw = bw / 2;

  function biquad(w0_: number, halfBw_: number) {
    const K = 2 / T;
    const A0 = K * K + halfBw_ * K + w0_ * w0_;
    return {
      b: [halfBw_ * K / A0, 0, -(halfBw_ * K) / A0],
      a: [1.0, (2 * (w0_ * w0_ - K * K)) / A0, (K * K - halfBw_ * K + w0_ * w0_) / A0]
    };
  }

  const s1 = biquad(w0, halfBw);
  const s2 = biquad(w0, halfBw);

  const bConv = convolve(s1.b, s2.b);
  const aConv = convolve(s1.a, s2.a);

  const bArr = new Float64Array(5);
  const aArr = new Float64Array(5);
  for (let i = 0; i < Math.min(5, bConv.length); i++) bArr[i] = bConv[i];
  for (let i = 0; i < Math.min(5, aConv.length); i++) aArr[i] = aConv[i];

  return { b: bArr, a: aArr };
}

function getCoeffs(sampleRateHz: number): ButterworthCoeffs {
  const cached = coeffsCache.get(sampleRateHz);
  if (cached) return cached;
  const coeffs = computeButterworthBandpass(20, 450, sampleRateHz);
  coeffsCache.set(sampleRateHz, coeffs);
  return coeffs;
}

function applyIIR(x: Float32Array, b: Float64Array, a: Float64Array): Float32Array {
  const n = x.length;
  const y = new Float32Array(n);
  const w = new Float64Array(5);
  for (let i = 0; i < n; i++) {
    const xi = x[i];
    const yi = b[0] * xi + w[1];
    w[1] = b[1] * xi - a[1] * yi + w[2];
    w[2] = b[2] * xi - a[2] * yi + w[3];
    w[3] = b[3] * xi - a[3] * yi + w[4];
    w[4] = b[4] * xi - a[4] * yi;
    y[i] = yi;
  }
  return y;
}

function rmsEnvelope(x: Float32Array, sampleRateHz: number): Float32Array {
  const n = x.length;
  const y = new Float32Array(n);
  const rmsWin = Math.max(1, Math.round(sampleRateHz / 10));
  for (let i = 0; i < n; i++) {
    const start = Math.max(0, i - Math.floor(rmsWin / 2));
    const end = Math.min(n, i + Math.floor(rmsWin / 2));
    let sum = 0;
    for (let j = start; j < end; j++) {
      sum += x[j] * x[j];
    }
    y[i] = Math.sqrt(sum / (end - start));
  }
  return y;
}

export class EMGPreprocessor {
  private static _normMaxima: Float32Array | null = null;

  static computeNormalisationMaxima(buffer: EMGDataBuffer): void {
    const maxima = new Float32Array(8);
    const coeffs = getCoeffs(buffer.sampleRateHz);
    for (let c = 0; c < 8; c++) {
      const channelData = new Float32Array(buffer.sampleCount);
      for (let i = 0; i < buffer.sampleCount; i++) {
        channelData[i] = buffer.samples[i].channels[c];
      }
      const filtered = applyIIR(channelData, coeffs.b, coeffs.a);
      const rectified = new Float32Array(filtered.length);
      for (let i = 0; i < filtered.length; i++) rectified[i] = Math.abs(filtered[i]);
      const envelope = rmsEnvelope(rectified, buffer.sampleRateHz);
      let max = 0;
      for (let i = 0; i < envelope.length; i++) {
        if (envelope[i] > max) max = envelope[i];
      }
      maxima[c] = max > 0 ? max : 1.0;
    }
    EMGPreprocessor._normMaxima = maxima;
  }

  static process(window: Float32Array[], sampleRateHz: number): Float32Array {
    const windowSize = window.length;
    const result = new Float32Array(8);
    const coeffs = getCoeffs(sampleRateHz);
    const maxima = EMGPreprocessor._normMaxima;

    for (let c = 0; c < 8; c++) {
      const channelData = new Float32Array(windowSize);
      for (let i = 0; i < windowSize; i++) {
        channelData[i] = window[i][c];
      }
      const filtered = applyIIR(channelData, coeffs.b, coeffs.a);
      const rectified = new Float32Array(windowSize);
      for (let i = 0; i < windowSize; i++) rectified[i] = Math.abs(filtered[i]);
      const envelope = rmsEnvelope(rectified, sampleRateHz);
      let sumSq = 0;
      for (let i = 0; i < windowSize; i++) sumSq += envelope[i] * envelope[i];
      let rms = windowSize > 0 ? Math.sqrt(sumSq / windowSize) : 0;
      const max = maxima ? maxima[c] : 1.0;
      rms = rms / (max > 0 ? max : 1.0);
      result[c] = Math.max(0, Math.min(1, rms));
    }
    return result;
  }
}
