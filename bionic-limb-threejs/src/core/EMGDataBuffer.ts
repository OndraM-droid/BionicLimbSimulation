import type { MyoSample } from './MyoFileParser.js';

export class EMGDataBuffer {
  readonly sampleCount: number;
  readonly channelCount: number = 8;
  readonly sampleRateHz: number;
  readonly samples: readonly MyoSample[];

  constructor(samples: MyoSample[], sampleRateHz: number) {
    this.samples = samples;
    this.sampleCount = samples.length;
    this.sampleRateHz = sampleRateHz;
  }

  getSampleAt(index: number): Float32Array {
    if (index < 0 || index >= this.sampleCount) {
      return new Float32Array(8);
    }
    return this.samples[index].channels;
  }

  getWindowAt(index: number, windowSize: number): Float32Array[] {
    const window: Float32Array[] = [];
    for (let i = 0; i < windowSize; i++) {
      const sampleIdx = index - windowSize + 1 + i;
      if (sampleIdx < 0) {
        window.push(new Float32Array(8));
      } else {
        window.push(this.getSampleAt(sampleIdx));
      }
    }
    return window;
  }
}
