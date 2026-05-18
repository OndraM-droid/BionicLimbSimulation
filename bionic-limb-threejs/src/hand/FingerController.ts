import * as THREE from 'three';

export class FingerController {
  private _proximal: THREE.Bone;
  private _middle: THREE.Bone | null;
  private _distal: THREE.Bone;

  constructor(proximal: THREE.Bone, middle: THREE.Bone | null, distal: THREE.Bone) {
    this._proximal = proximal;
    this._middle = middle;
    this._distal = distal;
  }

  setFlexion(degrees: number): void {
    const rad = THREE.MathUtils.degToRad(degrees);
    const euler = new THREE.Euler(rad, 0, 0, 'XYZ');
    this._proximal.quaternion.setFromEuler(euler);
    if (this._middle) {
      this._middle.quaternion.setFromEuler(euler);
    }
    this._distal.quaternion.setFromEuler(euler);
  }

  setAbduction(degrees: number): void {
    const rad = THREE.MathUtils.degToRad(degrees);
    const euler = new THREE.Euler(0, rad, 0, 'XYZ');
    this._proximal.quaternion.setFromEuler(euler);
  }
}
