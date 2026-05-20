import type * as THREE from 'three';
import type { GesturePose } from './GesturePose.js';

/**
 * Minimal interface that both HandRig (procedural) and GltfHandRig (GLB-based)
 * must satisfy.  GesturePoser and main.ts depend only on this contract.
 */
export interface IHandRig {
  /** The Three.js group to add to the scene. */
  readonly group: THREE.Group;
  /** Queue a target pose; the rig will interpolate toward it on update(). */
  setTargetPose(pose: GesturePose): void;
  /** Advance the pose interpolation by `delta` seconds. */
  update(delta: number, blendSpeed?: number): void;
}
