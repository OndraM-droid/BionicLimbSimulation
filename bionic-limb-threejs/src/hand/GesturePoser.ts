import type { GestureResult } from '../classification/IGestureClassifier.js';
import type { IHandRig } from './IHandRig.js';
import type { GesturePoseLibrary } from './GesturePoseLibrary.js';

export class GesturePoser {
  private _handRig: IHandRig;
  private _library: GesturePoseLibrary;

  constructor(handRig: IHandRig, library: GesturePoseLibrary) {
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
