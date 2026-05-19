import * as THREE from 'three';
import type { GesturePose } from './GesturePose.js';

// Dark gunmetal metallic shell – main finger/palm segments
const SHELL_MATERIAL = new THREE.MeshStandardMaterial({
  color: 0x1a1f2e,
  metalness: 0.9,
  roughness: 0.15,
  envMapIntensity: 1.0,
});

// Gold/brass accent pieces – knuckle guards, wrist ring
const ACCENT_MATERIAL = new THREE.MeshStandardMaterial({
  color: 0xb8960c,
  metalness: 0.95,
  roughness: 0.2,
  envMapIntensity: 1.2,
});

// Dark inner mechanical parts exposed at segment ends
const INNER_MATERIAL = new THREE.MeshStandardMaterial({
  color: 0x0d1117,
  metalness: 0.7,
  roughness: 0.4,
});

// Radius per bone index: MC=0.0065, PP=0.006, MP=0.0055, DP=0.005
const BONE_RADII = [
  0,       // 0: Hand_Root (palm handled separately)
  0.0065,  // 1: Thumb_MC
  0.006,   // 2: Thumb_PP
  0.005,   // 3: Thumb_DP
  0.0065,  // 4: Index_MC
  0.006,   // 5: Index_PP
  0.0055,  // 6: Index_MP
  0.005,   // 7: Index_DP
  0.0065,  // 8: Middle_MC
  0.006,   // 9: Middle_PP
  0.0055,  // 10: Middle_MP
  0.005,   // 11: Middle_DP
  0.0065,  // 12: Ring_MC
  0.006,   // 13: Ring_PP
];

/**
 * Returns a group of meshes representing a single cyberpunk finger segment:
 * a tapered cylinder shell, a gold knuckle guard at the proximal end,
 * a flat dorsal panel, and a dark inner ring peeking out at the distal end.
 */
function createBionicSegment(length: number, radius: number): THREE.Group {
  const group = new THREE.Group();

  // Main tapered shell body aligned along +Z
  const bodyGeo = new THREE.CylinderGeometry(radius * 0.8, radius * 0.9, length, 8);
  const body = new THREE.Mesh(bodyGeo, SHELL_MATERIAL);
  body.rotation.x = -Math.PI / 2;
  body.position.set(0, 0, length / 2);
  group.add(body);

  // Gold knuckle guard at the proximal (joint) end
  const knuckleGeo = new THREE.BoxGeometry(radius * 1.8, radius * 0.6, radius * 1.0);
  const knuckle = new THREE.Mesh(knuckleGeo, ACCENT_MATERIAL);
  knuckle.position.set(0, 0, 0);
  group.add(knuckle);

  // Flat dorsal panel on the back-of-hand side (+Y)
  const dorsalGeo = new THREE.BoxGeometry(radius * 1.4, radius * 0.15, length * 0.7);
  const dorsal = new THREE.Mesh(dorsalGeo, SHELL_MATERIAL);
  dorsal.position.set(0, radius * 0.975, length / 2);
  group.add(dorsal);

  // Dark inner ring exposed at the distal end – suggests internal mechanics
  const innerGeo = new THREE.CylinderGeometry(radius * 0.55, radius * 0.55, radius * 0.25, 6);
  const inner = new THREE.Mesh(innerGeo, INNER_MATERIAL);
  inner.rotation.x = -Math.PI / 2;
  inner.position.set(0, 0, length);
  group.add(inner);

  return group;
}

/**
 * Returns the palm and wrist visuals attached to the root bone:
 * a dorsal palm plate, two gold accent lines simulating panel seams,
 * and a gold wrist cuff ring.
 */
function createPalmVisual(): THREE.Group {
  const group = new THREE.Group();

  // Main dorsal palm plate
  const palmGeo = new THREE.BoxGeometry(0.06, 0.008, 0.055);
  const palm = new THREE.Mesh(palmGeo, SHELL_MATERIAL);
  palm.position.set(-0.005, 0.004, 0.03);
  group.add(palm);

  // Gold accent seam – proximal row
  const line1Geo = new THREE.BoxGeometry(0.058, 0.003, 0.002);
  const line1 = new THREE.Mesh(line1Geo, ACCENT_MATERIAL);
  line1.position.set(-0.005, 0.009, 0.02);
  group.add(line1);

  // Gold accent seam – distal row
  const line2Geo = new THREE.BoxGeometry(0.058, 0.003, 0.002);
  const line2 = new THREE.Mesh(line2Geo, ACCENT_MATERIAL);
  line2.position.set(-0.005, 0.009, 0.045);
  group.add(line2);

  // Gold wrist cuff ring at the base of the hand
  const cuffGeo = new THREE.CylinderGeometry(0.022, 0.024, 0.018, 16);
  const cuff = new THREE.Mesh(cuffGeo, ACCENT_MATERIAL);
  cuff.rotation.x = -Math.PI / 2;
  cuff.position.set(0, 0, -0.005);
  group.add(cuff);

  return group;
}

export class HandRig {
  readonly bones: THREE.Bone[] = [];
  readonly rootBone: THREE.Bone;
  readonly group: THREE.Group;

  private _targetRotations: THREE.Quaternion[] = [];
  private _currentRotations: THREE.Quaternion[] = [];
  private readonly _scratchEuler = new THREE.Euler();

  constructor() {
    this.group = new THREE.Group();

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

      if (spec.boneIdx === 0) {
        bone.add(createPalmVisual());
      } else {
        bone.add(createBionicSegment(length, BONE_RADII[spec.boneIdx]));
      }

      if (spec.parentIdx !== -1) {
        bones[spec.parentIdx].add(bone);
      }
    }

    this.group.add(bones[0]);

    this.bones = bones;
    this.rootBone = bones[0];

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
