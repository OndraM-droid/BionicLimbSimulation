import { describe, it, expect } from 'vitest';
import { EMGPreprocessor } from '../src/core/EMGPreprocessor.js';

function makeWindow(windowSize: number, value: number): Float32Array[] {
  return Array.from({ length: windowSize }, () => new Float32Array(8).fill(value));
}

describe('EMGPreprocessor', () => {
  it('all output values >= 0', () => {
    const window = makeWindow(40, 0.5);
    const result = EMGPreprocessor.process(window, 200);
    for (let i = 0; i < 8; i++) {
      expect(result[i]).toBeGreaterThanOrEqual(0);
    }
  });

  it('output length is 8', () => {
    const window = makeWindow(40, 0.3);
    const result = EMGPreprocessor.process(window, 200);
    expect(result.length).toBe(8);
  });

  it('all output values <= 1', () => {
    const window = makeWindow(40, 0.9);
    const result = EMGPreprocessor.process(window, 200);
    for (let i = 0; i < 8; i++) {
      expect(result[i]).toBeLessThanOrEqual(1);
    }
  });

  it('zero input window produces zero output', () => {
    const window = makeWindow(40, 0);
    const result = EMGPreprocessor.process(window, 200);
    for (let i = 0; i < 8; i++) {
      expect(result[i]).toBe(0);
    }
  });
});
