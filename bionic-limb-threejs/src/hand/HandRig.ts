import * as THREE from 'three';
import type { GesturePose } from './GesturePose.js';

// Dark charcoal gunmetal shell – main finger/palm segments
const SHELL_MATERIAL = new THREE.MeshStandardMaterial({
  color: 0x1c1c22,        // dark charcoal, almost black
  metalness: 0.85,
  roughness: 0.25,        // satin rather than mirror
  envMapIntensity: 1.5,
});

// Darker antique gold/bronze accent pieces – knuckle strips, wrist band
const ACCENT_MATERIAL = new THREE.MeshStandardMaterial({
  color: 0x8a7340,        // darker antique gold/bronze
  metalness: 0.9,
  roughness: 0.3,
  envMapIntensity: 1.0,
});

// Very dark near-black for joint gap areas
const JOINT_MATERIAL = new THREE.MeshStandardMaterial({
  color: 0x111116,
  metalness: 0.6,
  roughness: 0.5,
});

// [width, height, length] per bone index for BoxGeometry segments (bones along Z axis)
const BONE_DIMS: Array<[number, number, number]> = [
  [0,      0,      0     ],  // 0: Hand_Root — handled by palm
  [0.018,  0.012,  0.030 ],  // 1: Thumb_MC
  [0.016,  0.011,  0.030 ],  // 2: Thumb_PP
  [0.013,  0.009,  0.025 ],  // 3: Thumb_DP
  [0.016,  0.013,  0.040 ],  // 4: Index_MC
  [0.015,  0.012,  0.025 ],  // 5: Index_PP
  [0.014,  0.011,  0.020 ],  // 6: Index_MP
  [0.012,  0.009,  0.018 ],  // 7: Index_DP
  [0.017,  0.014,  0.040 ],  // 8: Middle_MC
  [0.016,  0.013,  0.030 ],  // 9: Middle_PP
  [0.015,  0.011,  0.020 ],  // 10: Middle_MP
  [0.013,  0.009,  0.018 ],  // 11: Middle_DP
  [0.016,  0.012,  0.040 ],  // 12: Ring_MC
  [0.015,  0.011,  0.035 ],  // 13: Ring_PP
];

/**
 * Returns a group of meshes representing a single cyberpunk finger segment:
 * a faceted box phalange body, a dorsal bevel strip, a gold accent strip at the
 * proximal joint, and a dark gap ring simulating the socket between segments.
 */
function createBionicSegment(length: number, w: number, h: number): THREE.Group {
  const group = new THREE.Group();

  // Main phalange body — box running along +Z, width=X height=Y depth=Z
  const bodyGeo = new THREE.BoxGeometry(w, h, length * 0.88);
  const body = new THREE.Mesh(bodyGeo, SHELL_MATERIAL);
  body.position.set(0, 0, length * 0.5);
  group.add(body);

  // Dorsal bevel strip — slightly raised panel on top face (+Y)
  const bevelGeo = new THREE.BoxGeometry(w * 0.7, h * 0.08, length * 0.75);
  const bevel = new THREE.Mesh(bevelGeo, SHELL_MATERIAL);
  bevel.position.set(0, h * 0.54, length * 0.5);
  group.add(bevel);

  // Gold accent strip at the proximal joint (base of this segment)
  const accentGeo = new THREE.BoxGeometry(w * 1.05, h * 0.18, 0.0015);
  const accent = new THREE.Mesh(accentGeo, ACCENT_MATERIAL);
  accent.position.set(0, 0, 0.001);
  group.add(accent);

  // Dark joint gap ring at proximal end — simulates socket/gap between segments
  const gapGeo = new THREE.BoxGeometry(w * 1.02, h * 1.02, 0.002);
  const gap = new THREE.Mesh(gapGeo, JOINT_MATERIAL);
  gap.position.set(0, 0, -0.001);
  group.add(gap);

  return group;
}

/**
 * Returns the palm and wrist visuals attached to the root bone:
 * a solid thick palm block, a raised dorsal centre panel, a gold knuckle seam,
 * a thin gold wrist band, and a dark spacer behind it.
 */
function createPalmVisual(): THREE.Group {
  const group = new THREE.Group();

  // Main palm body — solid thick block extending in +Z
  const palmGeo = new THREE.BoxGeometry(0.072, 0.014, 0.058);
  const palm = new THREE.Mesh(palmGeo, SHELL_MATERIAL);
  palm.position.set(-0.002, 0, 0.030);
  group.add(palm);

  // Dorsal raised centre panel
  const dorsalGeo = new THREE.BoxGeometry(0.050, 0.005, 0.044);
  const dorsal = new THREE.Mesh(dorsalGeo, SHELL_MATERIAL);
  dorsal.position.set(-0.002, 0.0095, 0.032);
  group.add(dorsal);

  // Gold accent seam across the knuckle line (distal edge of palm)
  const knuckleSeamGeo = new THREE.BoxGeometry(0.070, 0.003, 0.0015);
  const knuckleSeam = new THREE.Mesh(knuckleSeamGeo, ACCENT_MATERIAL);
  knuckleSeam.position.set(-0.002, 0.008, 0.057);
  group.add(knuckleSeam);

  // Thin gold wrist band (flat box, not a large cylinder)
  const wristBandGeo = new THREE.BoxGeometry(0.046, 0.025, 0.006);
  const wristBand = new THREE.Mesh(wristBandGeo, ACCENT_MATERIAL);
  wristBand.position.set(0, 0, -0.003);
  group.add(wristBand);

  // Dark spacer between palm block and wrist band
  const spacerGeo = new THREE.BoxGeometry(0.040, 0.020, 0.005);
  const spacer = new THREE.Mesh(spacerGeo, JOINT_MATERIAL);
  spacer.position.set(0, 0, -0.008);
  group.add(spacer);

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
        const [bw, bh] = BONE_DIMS[spec.boneIdx];
        bone.add(createBionicSegment(length, bw, bh));
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
