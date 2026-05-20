/**
 * GltfHandRig — Strategy C+D integration
 *
 * Loads robotic_hand_white.glb as the primary visual representation of the hand
 * while keeping the internal procedural HandRig invisible but fully active for:
 *   - Pose interpolation (smooth slerp between gesture targets)
 *   - ROM clamping (anatomically correct rotation limits)
 *   - Interface compatibility with GesturePoser
 *
 * Why not Strategy A/B?
 *   Inspection of both small GLB files reveals NO skeleton (skins: undefined).
 *   robotic_hand_white.glb has 36 flat static mesh groups under Root, all with
 *   baked geometry and generic Blender names (Sphere.008, Cylinder.018..Cylinder,
 *   Cube.037..Cube.014) — no finger hierarchy to drive with bone rotations.
 *   human_hand_model_open_with_fingers_spread.glb is a single monolithic mesh.
 *
 * Fallback behaviour:
 *   If the GLB fails to load (network error, missing file, etc.) the procedural
 *   HandRig meshes stay visible — the simulation continues to work unchanged.
 */

import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import type { GesturePose } from './GesturePose.js';
import type { IHandRig } from './IHandRig.js';
import { HandRig } from './HandRig.js';

// ── Materials ──────────────────────────────────────────────────────────────

/** Dark gunmetal clearcoat shell applied to all unnamed GLB surfaces. */
function makeShellMat(): THREE.MeshPhysicalMaterial {
  return new THREE.MeshPhysicalMaterial({
    color: 0x1c1c22,
    metalness: 0.9,
    roughness: 0.15,
    clearcoat: 1.0,
    clearcoatRoughness: 0.05,
    envMapIntensity: 1.5,
  });
}

/** Antique gold/brass accent applied to any surface with 'gold'/'accent'/'brass' in name. */
function makeAccentMat(): THREE.MeshPhysicalMaterial {
  return new THREE.MeshPhysicalMaterial({
    color: 0x8a7340,
    metalness: 0.9,
    roughness: 0.25,
    clearcoat: 0.3,
    clearcoatRoughness: 0.2,
    envMapIntensity: 1.0,
  });
}

// ── Class ──────────────────────────────────────────────────────────────────

export class GltfHandRig implements IHandRig {
  readonly group: THREE.Group;

  /** Inner procedural rig — drives pose maths, kept hidden after GLB loads. */
  private readonly _handRig: HandRig;
  private _loaded = false;

  constructor() {
    this._handRig = new HandRig();
    this.group = new THREE.Group();
    // The procedural rig is a child of our outer group so it animates and
    // provides an immediate fallback visual before the GLB arrives.
    this.group.add(this._handRig.group);
  }

  // ── Public API (IHandRig) ────────────────────────────────────────────────

  setTargetPose(pose: GesturePose): void {
    this._handRig.setTargetPose(pose);
  }

  update(delta: number, blendSpeed?: number): void {
    this._handRig.update(delta, blendSpeed);
  }

  /** True once the GLB model has been loaded and is being displayed. */
  get isLoaded(): boolean {
    return this._loaded;
  }

  // ── GLB loading ──────────────────────────────────────────────────────────

  /**
   * Asynchronously load a GLB file and overlay it on this rig.
   *
   * On success the procedural BoxGeometry segments are hidden and the GLB
   * scene is added to `this.group`.  On failure the procedural rig stays
   * visible so the simulation continues without interruption.
   *
   * Scale / orientation notes for robotic_hand_white.glb:
   *   • The file has 36 separate mesh groups all baked at their world
   *     positions; no individual node transforms are present.
   *   • Default scale is 1.0 (Blender meters export).  If the model appears
   *     far too large or small, try scale 0.01 (cm) or 0.001 (mm).
   *   • If the model faces the wrong direction, uncomment the rotation lines.
   */
  async load(url: string): Promise<void> {
    const loader = new GLTFLoader();
    try {
      const gltf = await loader.loadAsync(url);

      const shellMat = makeShellMat();
      const accentMat = makeAccentMat();

      gltf.scene.traverse((obj) => {
        if (obj instanceof THREE.Mesh) {
          const n = obj.name.toLowerCase();
          const isAccent =
            n.includes('gold') || n.includes('accent') || n.includes('brass');
          obj.material = isAccent ? accentMat : shellMat;
          obj.castShadow = true;
          obj.receiveShadow = true;
        }
      });

      // ── Scale / orientation ─────────────────────────────────────────────
      // robotic_hand_white.glb is a Sketchfab model exported at Blender-meter
      // scale.  Start with 1.0 and tune if the model looks wrong.
      // Uncomment one of the alternatives below if needed:
      //   gltf.scene.scale.setScalar(0.01);   // if exported in centimetres
      //   gltf.scene.scale.setScalar(0.001);  // if exported in millimetres
      gltf.scene.scale.setScalar(1.0);

      // Flip 180° around Y if the palm faces away from the camera.
      // gltf.scene.rotation.y = Math.PI;

      // ── Add to scene ────────────────────────────────────────────────────
      this.group.add(gltf.scene);

      // ── Hide procedural meshes now that the real model is visible ───────
      // Bones remain intact so pose maths still runs every frame.
      this._handRig.group.traverse((obj) => {
        if (obj instanceof THREE.Mesh) {
          obj.visible = false;
        }
      });

      this._loaded = true;
      console.info(`[GltfHandRig] ${url} loaded — procedural fallback hidden.`);
    } catch (err) {
      console.warn(
        '[GltfHandRig] GLB load failed — keeping procedural rig visible.',
        err,
      );
    }
  }
}
