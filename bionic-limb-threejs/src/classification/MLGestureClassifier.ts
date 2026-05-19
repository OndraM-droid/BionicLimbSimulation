import * as ort from 'onnxruntime-web';
import type { IGestureClassifier, GestureResult } from './IGestureClassifier.js';
import { GesturePoseLibrary } from '../hand/GesturePoseLibrary.js';

export class MLGestureClassifier implements IGestureClassifier {
  private _session: ort.InferenceSession;
  private _scalerMean: Float32Array;
  private _scalerScale: Float32Array;
  private _lastResult: GestureResult;

  private constructor(session: ort.InferenceSession, mean: Float32Array, scale: Float32Array) {
    this._session = session;
    this._scalerMean = mean;
    this._scalerScale = scale;
    const allProbs = new Float32Array(8);
    allProbs[0] = 1;
    this._lastResult = {
      gestureId: 0,
      gestureName: GesturePoseLibrary.gestureNames[0],
      confidence: 1,
      allProbabilities: allProbs
    };
  }

  static async create(modelUrl: string, scalerUrl: string): Promise<MLGestureClassifier> {
    const [session, scalerResponse] = await Promise.all([
      ort.InferenceSession.create(modelUrl, { executionProviders: ['webgl', 'wasm'] }),
      fetch(scalerUrl)
    ]);
    const scalerJson = await scalerResponse.json() as { mean: number[]; scale: number[] };
    const mean = new Float32Array(scalerJson.mean);
    const scale = new Float32Array(scalerJson.scale);
    return new MLGestureClassifier(session, mean, scale);
  }

  private _extractFeatures(windowFlat: Float32Array): Float32Array {
    const windowSize = Math.floor(windowFlat.length / 8);
    const feats = new Float32Array(32);
    for (let c = 0; c < 8; c++) {
      let sumAbs = 0, sumSq = 0, zc = 0, wl = 0;
      let prev = windowFlat[0 * 8 + c];
      for (let i = 0; i < windowSize; i++) {
        const v = windowFlat[i * 8 + c];
        sumAbs += Math.abs(v);
        sumSq += v * v;
        if (i > 0) {
          if (Math.sign(v) !== Math.sign(prev)) zc++;
          wl += Math.abs(v - prev);
          prev = v;
        }
      }
      feats[c * 4 + 0] = sumAbs / windowSize;
      feats[c * 4 + 1] = Math.sqrt(sumSq / windowSize);
      feats[c * 4 + 2] = windowSize > 1 ? zc / (windowSize - 1) : 0;
      feats[c * 4 + 3] = wl;
    }
    for (let i = 0; i < 32; i++) {
      feats[i] = (feats[i] - this._scalerMean[i]) / Math.max(this._scalerScale[i], 1e-8);
    }
    return feats;
  }

  private async _runAsync(windowFlat: Float32Array): Promise<void> {
    try {
      const feats = this._extractFeatures(windowFlat);
      const tensor = new ort.Tensor('float32', feats, [1, 32]);
      const inputName = this._session.inputNames[0];
      const feeds: Record<string, ort.Tensor> = { [inputName]: tensor };
      const results = await this._session.run(feeds);
      const outputName = this._session.outputNames[0];
      const output = results[outputName];
      const data = output.data as Float32Array;
      const maxVal = Math.max(...Array.from(data));
      const exps = Array.from(data).map((v: number) => Math.exp(v - maxVal));
      const sum = exps.reduce((a: number, b: number) => a + b, 0);
      const probs = new Float32Array(exps.map((e: number) => e / sum));
      let maxProb = -Infinity;
      let bestId = 0;
      for (let i = 0; i < probs.length; i++) {
        if (probs[i] > maxProb) { maxProb = probs[i]; bestId = i; }
      }
      this._lastResult = {
        gestureId: bestId,
        gestureName: GesturePoseLibrary.gestureNames[bestId] ?? 'unknown',
        confidence: maxProb,
        allProbabilities: probs
      };
    } catch (e) {
      console.warn('ONNX inference error:', e);
      // Keep last result on error
    }
  }

  classify(windowFlat: Float32Array): GestureResult {
    void this._runAsync(windowFlat);
    return this._lastResult;
  }

  dispose(): void {
    void this._session.release();
  }
}
