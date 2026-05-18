import { describe, it, expect } from 'vitest';
import { GesturePoseLibrary } from '../src/hand/GesturePoseLibrary.js';
import { BONE_COUNT } from '../src/hand/GesturePose.js';

describe('GesturePoseLibrary', () => {
  const lib = new GesturePoseLibrary();

  it('getPose 0-7 all non-null', () => {
    for (let i = 0; i < 8; i++) {
      expect(lib.getPose(i)).not.toBeNull();
    }
  });

  it('getPose out of range returns null', () => {
    expect(lib.getPose(-1)).toBeNull();
    expect(lib.getPose(8)).toBeNull();
  });

  it('gestureNames.length === 8', () => {
    expect(GesturePoseLibrary.gestureNames.length).toBe(8);
  });

  it('gestureNames match spec exactly', () => {
    expect(Array.from(GesturePoseLibrary.gestureNames)).toEqual([
      'Rest', 'Fist', 'Open', 'PinchIndex', 'PinchMiddle', 'Point', 'ThumbUp', 'Victory',
    ]);
  });

  it('gestureNames[0] === "Rest" (contract used by MLGestureClassifier initial state)', () => {
    expect(GesturePoseLibrary.gestureNames[0]).toBe('Rest');
  });

  it('BONE_COUNT === 14', () => {
    expect(BONE_COUNT).toBe(14);
  });
});
