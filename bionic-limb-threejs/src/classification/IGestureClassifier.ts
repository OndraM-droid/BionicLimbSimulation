export interface GestureResult {
  gestureId: number;
  gestureName: string;
  confidence: number;
  allProbabilities: Float32Array;
}

export interface IGestureClassifier {
  classify(features: Float32Array): GestureResult;
  dispose(): void;
}
