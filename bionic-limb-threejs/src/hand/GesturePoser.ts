import type { GestureResult } from '../classification/IGestureClassifier.js';
import type { HandRig } from './HandRig.js';
import type { GesturePoseLibrary } from './GesturePoseLibrary.js';

export class GesturePoser {
  private _handRig: HandRig;
  private _library: GesturePoseLibrary;

  constructor(handRig: HandRig, library: GesturePoseLibrary) {
    this._handRig = handRig;
    this._library = library;
  }

  applyResult(result: GestureResult): void {
    const pose = this._library.getPose(result.gestureId);
    if (pose) {
      this._handRig.setTargetPose(pose);
    }
  }

  update(delta: number): void {
    this._handRig.update(delta);
  }
}
