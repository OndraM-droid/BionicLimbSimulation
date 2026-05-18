import * as THREE from 'three';
import type { GesturePose } from './GesturePose.js';

const SKIN_MATERIAL = new THREE.MeshStandardMaterial({
  color: new THREE.Color(0.9, 0.7, 0.6),
  roughness: 0.8,
  metalness: 0.0,
});

export class HandRig {
  readonly bones: THREE.Bone[] = [];
  readonly rootBone: THREE.Bone;
  readonly skeleton: THREE.Skeleton;
  readonly mesh: THREE.SkinnedMesh;

  private _targetRotations: THREE.Quaternion[] = [];
  private _currentRotations: THREE.Quaternion[] = [];
  private readonly _scratchEuler = new THREE.Euler();

  constructor() {
    const createBone = (name: string): THREE.Bone => {
      const bone = new THREE.Bone();
      bone.name = name;
      return bone;
    };

    const bones = [
      createBone('Hand_Root'),
      createBone('Thumb_MC'),
      createBone('Thumb_PP'),
      createBone('Thumb_DP'),
      createBone('Index_MC'),
      createBone('Index_PP'),
      createBone('Index_MP'),
      createBone('Index_DP'),
      createBone('Middle_MC'),
      createBone('Middle_PP'),
      createBone('Middle_MP'),
      createBone('Middle_DP'),
      createBone('Ring_MC'),
      createBone('Ring_PP'),
    ];

    const boneSpecs: { boneIdx: number; parentIdx: number; pos: [number, number, number] }[] = [
      { boneIdx: 0, parentIdx: -1, pos: [0, 0, 0] },
      { boneIdx: 1, parentIdx: 0, pos: [0, 0, 0.02] },
      { boneIdx: 2, parentIdx: 1, pos: [0, 0, 0.03] },
      { boneIdx: 3, parentIdx: 2, pos: [0, 0, 0.03] },
      { boneIdx: 4, parentIdx: 0, pos: [-0.04, 0, 0.05] },
      { boneIdx: 5, parentIdx: 4, pos: [0, 0, 0.04] },
      { boneIdx: 6, parentIdx: 5, pos: [0, 0, 0.025] },
      { boneIdx: 7, parentIdx: 6, pos: [0, 0, 0.02] },
      { boneIdx: 8, parentIdx: 0, pos: [-0.01, 0, 0.055] },
      { boneIdx: 9, parentIdx: 8, pos: [0, 0, 0.04] },
      { boneIdx: 10, parentIdx: 9, pos: [0, 0, 0.03] },
      { boneIdx: 11, parentIdx: 10, pos: [0, 0, 0.02] },
      { boneIdx: 12, parentIdx: 0, pos: [0.02, 0, 0.05] },
      { boneIdx: 13, parentIdx: 12, pos: [0, 0, 0.04] },
    ];

    const boneLengths: number[] = [
      0.02, 0.03, 0.03, 0.025,
      0.04, 0.025, 0.02, 0.018,
      0.04, 0.03, 0.02, 0.018,
      0.04, 0.035,
    ];

    for (const spec of boneSpecs) {
      const bone = bones[spec.boneIdx];
      bone.position.set(spec.pos[0], spec.pos[1], spec.pos[2]);
      const length = boneLengths[spec.boneIdx];
      const capsule = new THREE.Mesh(
        new THREE.CapsuleGeometry(0.007, length, 4, 8),
        SKIN_MATERIAL
      );
      capsule.rotation.x = -Math.PI / 2;
      capsule.position.set(0, 0, length / 2);
      bone.add(capsule);
      if (spec.parentIdx !== -1) {
        bones[spec.parentIdx].add(bone);
      }
    }

    const skeleton = new THREE.Skeleton(bones);
    const skinnedMesh = new THREE.SkinnedMesh(new THREE.BufferGeometry(), SKIN_MATERIAL);
    skinnedMesh.add(bones[0]);
    skinnedMesh.bind(skeleton);

    this.bones = bones;
    this.rootBone = bones[0];
    this.skeleton = skeleton;
    this.mesh = skinnedMesh;

    for (let i = 0; i < 14; i++) {
      this._targetRotations.push(new THREE.Quaternion());
      this._currentRotations.push(new THREE.Quaternion());
    }
  }

  setTargetPose(pose: GesturePose): void {
    for (let i = 0; i < 14 && i < pose.rotations.length; i++) {
      this._targetRotations[i].copy(pose.rotations[i]);
    }
  }

  update(delta: number, blendSpeed: number = 5.0): void {
    const t = Math.min(1, delta * blendSpeed);
    for (let i = 0; i < 14; i++) {
      this._currentRotations[i].slerp(this._targetRotations[i], t);
      this.bones[i].quaternion.copy(this._currentRotations[i]);
    }
    this._applyROMClamping();
  }

  private _applyROMClamping(): void {
    for (const idx of [1, 4, 8, 12]) {
      this._clampBoneRotationX(idx, -10, 10);
    }
    for (const idx of [2, 5, 9, 13]) {
      this._clampBoneRotationX(idx, -90, 0);
    }
    for (const idx of [3, 6, 7, 10, 11]) {
      this._clampBoneRotationX(idx, -80, 0);
    }
  }

  private _clampBoneRotationX(boneIdx: number, minDeg: number, maxDeg: number): void {
    const bone = this.bones[boneIdx];
    this._scratchEuler.setFromQuaternion(bone.quaternion, 'XYZ');
    this._scratchEuler.x = THREE.MathUtils.clamp(
      this._scratchEuler.x,
      THREE.MathUtils.degToRad(minDeg),
      THREE.MathUtils.degToRad(maxDeg)
    );
    bone.quaternion.setFromEuler(this._scratchEuler);
  }
}
