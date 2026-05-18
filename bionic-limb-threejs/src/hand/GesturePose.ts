import * as THREE from 'three';

export interface GesturePose {
  name: string;
  rotations: THREE.Quaternion[];
}

export const BoneIndex = {
  Hand_Root:  0,
  Thumb_MC:   1, Thumb_PP:   2, Thumb_DP:   3,
  Index_MC:   4, Index_PP:   5, Index_MP:   6, Index_DP:   7,
  Middle_MC:  8, Middle_PP:  9, Middle_MP:  10, Middle_DP:  11,
  Ring_MC:   12, Ring_PP:   13,
} as const;

export const BONE_COUNT = 14;
