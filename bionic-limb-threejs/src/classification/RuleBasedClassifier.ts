import type { IGestureClassifier, GestureResult } from './IGestureClassifier.js';
import { GesturePoseLibrary } from '../hand/GesturePoseLibrary.js';

const THRESHOLD = 0.3;

export class RuleBasedClassifier implements IGestureClassifier {
  classify(features: Float32Array): GestureResult {
    const active = (ch: number) => features[ch] > THRESHOLD;
    const activeCount = Array.from({ length: 8 }, (_, i) => features[i] > THRESHOLD ? 1 : 0)
      .reduce((a: number, b: number) => a + b, 0);

    let gestureId: number;

    if (activeCount >= 5) {
      gestureId = 1; // Fist
    } else if (active(0) && active(1) && active(4)) {
      gestureId = 7; // Victory
    } else if (active(4) && active(5)) {
      gestureId = 6; // ThumbUp
    } else if (active(0) && !active(4)) {
      gestureId = 5; // Point
    } else if (active(1) && active(2)) {
      gestureId = 4; // PinchMiddle
    } else if (active(0) && active(1)) {
      gestureId = 3; // PinchIndex
    } else if (activeCount > 0 && activeCount <= 1) {
      gestureId = 2; // Open
    } else {
      gestureId = 0; // Rest
    }

    const confidence = activeCount === 0 ? 0 : activeCount / 8;
    const gestureName = GesturePoseLibrary.gestureNames[gestureId];

    const allProbabilities = new Float32Array(8);
    if (activeCount === 0) {
      allProbabilities[0] = 1.0;
    } else {
      allProbabilities[gestureId] = confidence;
      const remaining = (1 - confidence) / 7;
      for (let i = 0; i < 8; i++) {
        if (i !== gestureId) allProbabilities[i] = remaining;
      }
    }

    return { gestureId, gestureName, confidence, allProbabilities };
  }

  dispose(): void {}
}
