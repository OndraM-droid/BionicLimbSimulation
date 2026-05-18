import * as THREE from 'three';
import type { GesturePose } from './GesturePose.js';
import { BoneIndex, BONE_COUNT } from './GesturePose.js';

function eulerToQuat(x: number, y: number, z: number): THREE.Quaternion {
  const e = new THREE.Euler(
    THREE.MathUtils.degToRad(x),
    THREE.MathUtils.degToRad(y),
    THREE.MathUtils.degToRad(z),
    'XYZ'
  );
  return new THREE.Quaternion().setFromEuler(e);
}

function identity(): THREE.Quaternion {
  return new THREE.Quaternion();
}

function makeRotations(defs: { [boneIdx: number]: [number, number, number] }): THREE.Quaternion[] {
  const rots: THREE.Quaternion[] = [];
  for (let i = 0; i < BONE_COUNT; i++) {
    if (defs[i]) {
      rots.push(eulerToQuat(defs[i][0], defs[i][1], defs[i][2]));
    } else {
      rots.push(identity());
    }
  }
  return rots;
}

export class GesturePoseLibrary {
  static readonly gestureNames: readonly string[] = [
    'rest', 'fist', 'open_hand', 'pinch_index', 'pinch_middle',
    'point', 'thumb_up', 'victory'
  ];

  private _poses: GesturePose[];

  constructor() {
    this._poses = [
      {
        name: 'rest',
        rotations: makeRotations({
          [BoneIndex.Thumb_PP]:   [-20, 0, 0],
          [BoneIndex.Thumb_DP]:   [-10, 0, 0],
          [BoneIndex.Index_PP]:   [-20, 0, 0],
          [BoneIndex.Index_MP]:   [-20, 0, 0],
          [BoneIndex.Index_DP]:   [-10, 0, 0],
          [BoneIndex.Middle_PP]:  [-20, 0, 0],
          [BoneIndex.Middle_MP]:  [-20, 0, 0],
          [BoneIndex.Middle_DP]:  [-10, 0, 0],
          [BoneIndex.Ring_PP]:    [-20, 0, 0],
        })
      },
      {
        name: 'fist',
        rotations: makeRotations({
          [BoneIndex.Thumb_PP]:   [-80, 0, 0],
          [BoneIndex.Thumb_DP]:   [-60, 0, 0],
          [BoneIndex.Index_PP]:   [-80, 0, 0],
          [BoneIndex.Index_MP]:   [-80, 0, 0],
          [BoneIndex.Index_DP]:   [-60, 0, 0],
          [BoneIndex.Middle_PP]:  [-80, 0, 0],
          [BoneIndex.Middle_MP]:  [-80, 0, 0],
          [BoneIndex.Middle_DP]:  [-60, 0, 0],
          [BoneIndex.Ring_PP]:    [-80, 0, 0],
        })
      },
      { name: 'open_hand', rotations: makeRotations({}) },
      {
        name: 'pinch_index',
        rotations: makeRotations({
          [BoneIndex.Thumb_PP]:   [-20, 0, 0],
          [BoneIndex.Thumb_DP]:   [-10, 0, 0],
          [BoneIndex.Index_PP]:   [-70, 0, 0],
          [BoneIndex.Index_MP]:   [-60, 0, 0],
          [BoneIndex.Index_DP]:   [-50, 0, 0],
          [BoneIndex.Middle_PP]:  [-20, 0, 0],
          [BoneIndex.Middle_MP]:  [-20, 0, 0],
          [BoneIndex.Middle_DP]:  [-10, 0, 0],
          [BoneIndex.Ring_PP]:    [-20, 0, 0],
        })
      },
      {
        name: 'pinch_middle',
        rotations: makeRotations({
          [BoneIndex.Thumb_PP]:   [-20, 0, 0],
          [BoneIndex.Thumb_DP]:   [-10, 0, 0],
          [BoneIndex.Index_PP]:   [-20, 0, 0],
          [BoneIndex.Index_MP]:   [-20, 0, 0],
          [BoneIndex.Index_DP]:   [-10, 0, 0],
          [BoneIndex.Middle_PP]:  [-70, 0, 0],
          [BoneIndex.Middle_MP]:  [-60, 0, 0],
          [BoneIndex.Middle_DP]:  [-50, 0, 0],
          [BoneIndex.Ring_PP]:    [-20, 0, 0],
        })
      },
      {
        name: 'point',
        rotations: makeRotations({
          [BoneIndex.Thumb_PP]:   [-80, 0, 0],
          [BoneIndex.Thumb_DP]:   [-60, 0, 0],
          [BoneIndex.Middle_PP]:  [-80, 0, 0],
          [BoneIndex.Middle_MP]:  [-80, 0, 0],
          [BoneIndex.Middle_DP]:  [-60, 0, 0],
          [BoneIndex.Ring_PP]:    [-80, 0, 0],
        })
      },
      {
        name: 'thumb_up',
        rotations: makeRotations({
          [BoneIndex.Thumb_MC]:   [0, 0, -20],
          [BoneIndex.Index_PP]:   [-80, 0, 0],
          [BoneIndex.Index_MP]:   [-80, 0, 0],
          [BoneIndex.Index_DP]:   [-60, 0, 0],
          [BoneIndex.Middle_PP]:  [-80, 0, 0],
          [BoneIndex.Middle_MP]:  [-80, 0, 0],
          [BoneIndex.Middle_DP]:  [-60, 0, 0],
          [BoneIndex.Ring_PP]:    [-80, 0, 0],
        })
      },
      {
        name: 'victory',
        rotations: makeRotations({
          [BoneIndex.Thumb_PP]:   [-80, 0, 0],
          [BoneIndex.Thumb_DP]:   [-60, 0, 0],
          [BoneIndex.Ring_PP]:    [-80, 0, 0],
        })
      },
    ];
  }

  getPose(gestureId: number): GesturePose | null {
    if (gestureId < 0 || gestureId >= this._poses.length) return null;
    return this._poses[gestureId] ?? null;
  }
}
