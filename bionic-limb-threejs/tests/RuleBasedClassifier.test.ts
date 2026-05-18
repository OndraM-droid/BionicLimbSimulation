import { describe, it, expect } from 'vitest';
import { RuleBasedClassifier } from '../src/classification/RuleBasedClassifier.js';
import { GesturePoseLibrary } from '../src/hand/GesturePoseLibrary.js';

describe('RuleBasedClassifier', () => {
  const clf = new RuleBasedClassifier();

  it('all zeros -> rest (id 0)', () => {
    const result = clf.classify(new Float32Array(8));
    expect(result.gestureId).toBe(0);
  });

  it('5 channels active -> fist (id 1)', () => {
    const features = new Float32Array([0.8, 0.8, 0.8, 0.8, 0.8, 0.0, 0.0, 0.0]);
    const result = clf.classify(features);
    expect(result.gestureId).toBe(1);
  });

  it('confidence in [0, 1]', () => {
    const result = clf.classify(new Float32Array([0.5, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));
    expect(result.confidence).toBeGreaterThanOrEqual(0);
    expect(result.confidence).toBeLessThanOrEqual(1);
  });

  it('allProbabilities sums to ~1.0', () => {
    const result = clf.classify(new Float32Array([0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));
    const sum = Array.from(result.allProbabilities).reduce((a, b) => a + b, 0);
    expect(sum).toBeCloseTo(1.0, 3);
  });

  it('allProbabilities.length === 8', () => {
    const result = clf.classify(new Float32Array(8));
    expect(result.allProbabilities.length).toBe(8);
  });

  it('gestureName matches library', () => {
    const result = clf.classify(new Float32Array(8));
    expect(result.gestureName).toBe(GesturePoseLibrary.gestureNames[result.gestureId]);
  });

  it('[0.5, 0, ...] -> point (id 5)', () => {
    const features = new Float32Array(8);
    features[0] = 0.5;
    const result = clf.classify(features);
    expect(result.gestureId).toBe(5);
  });
});
