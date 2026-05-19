import { MyoFileParser } from '../core/MyoFileParser.js';
import { EMGDataBuffer } from '../core/EMGDataBuffer.js';
import { EMGPreprocessor } from '../core/EMGPreprocessor.js';
import type { IGestureClassifier, GestureResult } from '../classification/IGestureClassifier.js';
import { RuleBasedClassifier } from '../classification/RuleBasedClassifier.js';
import { WINDOW_SIZE } from '../core/constants.js';

export type PlaybackState = 'Idle' | 'Loaded' | 'Playing' | 'Paused' | 'Error';

export class PlaybackController extends EventTarget {
  state: PlaybackState = 'Idle';
  currentSampleIndex: number = 0;
  currentResult: GestureResult | null = null;
  classifierMode: 'ML' | 'RuleBased' = 'RuleBased';

  private _buffer: EMGDataBuffer | null = null;
  private _classifier: IGestureClassifier;
  private _accumulator: number = 0;
  private _sampleInterval: number = 1 / 200;
  private _stepBoundaries: number[] = [];
  private _currentStep: number = 0;

  constructor() {
    super();
    this._classifier = new RuleBasedClassifier();
  }

  loadFile(csvText: string): void {
    try {
      const parsed = MyoFileParser.parse(csvText);
      this._buffer = new EMGDataBuffer(parsed.samples, parsed.sampleRateHz);
      EMGPreprocessor.computeNormalisationMaxima(this._buffer);
      this._sampleInterval = 1 / parsed.sampleRateHz;
      this._stepBoundaries = [0];
      let lastGestureId = parsed.samples[0]?.gestureId ?? 0;
      for (let i = 1; i < parsed.samples.length; i++) {
        if (parsed.samples[i].gestureId !== lastGestureId) {
          this._stepBoundaries.push(i);
          lastGestureId = parsed.samples[i].gestureId;
        }
      }
      this.currentSampleIndex = 0;
      this._accumulator = 0;
      this._currentStep = 0;
      this.state = 'Loaded';
      this.dispatchEvent(new CustomEvent('stateChanged', { detail: { state: this.state } }));
    } catch (e) {
      this.state = 'Error';
      this.dispatchEvent(new CustomEvent('stateChanged', { detail: { state: this.state } }));
      throw e;
    }
  }

  play(): void {
    if (this.state === 'Loaded' || this.state === 'Paused') {
      this.state = 'Playing';
      this.dispatchEvent(new CustomEvent('stateChanged', { detail: { state: this.state } }));
    }
  }

  pause(): void {
    if (this.state === 'Playing') {
      this.state = 'Paused';
      this.dispatchEvent(new CustomEvent('stateChanged', { detail: { state: this.state } }));
    }
  }

  nextStep(): void {
    if (!this._buffer) return;
    const nextStep = this._currentStep + 1;
    if (nextStep < this._stepBoundaries.length) {
      this._currentStep = nextStep;
      this.currentSampleIndex = this._stepBoundaries[nextStep];
      this._processSample(this.currentSampleIndex);
    }
  }

  previousStep(): void {
    if (!this._buffer) return;
    const prevStep = this._currentStep - 1;
    if (prevStep >= 0) {
      this._currentStep = prevStep;
      this.currentSampleIndex = this._stepBoundaries[prevStep];
      this._processSample(this.currentSampleIndex);
    }
  }

  update(delta: number): void {
    if (this.state !== 'Playing' || !this._buffer) return;
    this._accumulator += delta;
    while (this._accumulator >= this._sampleInterval) {
      this._processSample(this.currentSampleIndex);
      this.currentSampleIndex++;
      this._accumulator -= this._sampleInterval;
      if (this.currentSampleIndex >= this._buffer.sampleCount) {
        this.state = 'Paused';
        this.dispatchEvent(new CustomEvent('stateChanged', { detail: { state: this.state } }));
        break;
      }
    }
  }

  setClassifier(classifier: IGestureClassifier, mode: 'ML' | 'RuleBased'): void {
    this._classifier.dispose();
    this._classifier = classifier;
    this.classifierMode = mode;
    this.dispatchEvent(new CustomEvent('stateChanged', { detail: { state: this.state } }));
  }

  private _processSample(index: number): void {
    if (!this._buffer) return;
    const window = this._buffer.getWindowAt(index, WINDOW_SIZE);
    const windowFlat = new Float32Array(WINDOW_SIZE * 8);
    for (let i = 0; i < WINDOW_SIZE; i++) {
      for (let c = 0; c < 8; c++) {
        windowFlat[i * 8 + c] = window[i][c];
      }
    }
    const features = EMGPreprocessor.process(window, this._buffer.sampleRateHz);
    const classifyInput = this.classifierMode === 'ML' ? windowFlat : features;
    const result = this._classifier.classify(classifyInput);
    this.currentResult = result;
    this._currentStep = this._findCurrentStep(index);
    const progress = this._buffer.sampleCount > 1 ? index / (this._buffer.sampleCount - 1) : 0;
    this.dispatchEvent(new CustomEvent('gestureChanged', { detail: { result } }));
    this.dispatchEvent(new CustomEvent('progressChanged', { detail: { progress } }));
  }

  private _findCurrentStep(index: number): number {
    let lo = 0, hi = this._stepBoundaries.length - 1;
    while (lo < hi) {
      const mid = (lo + hi + 1) >> 1;
      if (this._stepBoundaries[mid] <= index) lo = mid;
      else hi = mid - 1;
    }
    return lo;
  }
}
