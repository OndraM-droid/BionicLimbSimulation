import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { HandRig } from './hand/HandRig.js';
import { GesturePoseLibrary } from './hand/GesturePoseLibrary.js';
import { GesturePoser } from './hand/GesturePoser.js';
import { MLGestureClassifier } from './classification/MLGestureClassifier.js';
import { RuleBasedClassifier } from './classification/RuleBasedClassifier.js';
import { PlaybackController } from './playback/PlaybackController.js';
import { PlaybackUI } from './playback/PlaybackUI.js';
import type { GestureResult } from './classification/IGestureClassifier.js';

const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setPixelRatio(window.devicePixelRatio);
renderer.setSize(window.innerWidth, window.innerHeight);
document.body.appendChild(renderer.domElement);

window.addEventListener('resize', () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

const camera = new THREE.PerspectiveCamera(40, window.innerWidth / window.innerHeight, 0.001, 100);
camera.position.set(0, 0.1, 0.35);
camera.lookAt(0, 0.08, 0);

const scene = new THREE.Scene();
scene.background = new THREE.Color(0x111111);

const controls = new OrbitControls(camera, renderer.domElement);
controls.target.set(0, 0.08, 0);
controls.update();

const dirLight = new THREE.DirectionalLight(0xffffff, 1.2);
dirLight.position.set(0.64, 0.77, 0.26);
scene.add(dirLight);
scene.add(new THREE.AmbientLight(0xffffff, 0.3));

const handRig = new HandRig();
scene.add(handRig.group);

const library = new GesturePoseLibrary();
const poser = new GesturePoser(handRig, library);

const restPose = library.getPose(0);
if (restPose) handRig.setTargetPose(restPose);

const controller = new PlaybackController();

(async () => {
  try {
    const mlClassifier = await MLGestureClassifier.create(
      '/data/GestureClassifier.onnx',
      '/data/GestureClassifier_scaler.json'
    );
    controller.setClassifier(mlClassifier, 'ML');
  } catch (_e) {
    controller.setClassifier(new RuleBasedClassifier(), 'RuleBased');
  }
})();

new PlaybackUI(controller);

controller.addEventListener('gestureChanged', (e: Event) => {
  const result = (e as CustomEvent<GestureResult>).detail;
  poser.applyResult(result);
});

const clock = new THREE.Clock();
renderer.setAnimationLoop(() => {
  const delta = clock.getDelta();
  controller.update(delta);
  poser.update(delta);
  controls.update();
  renderer.render(scene, camera);
});
