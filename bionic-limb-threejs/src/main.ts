import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';
import { HandRig } from './hand/HandRig.js';
import { GesturePoseLibrary } from './hand/GesturePoseLibrary.js';
import { GesturePoser } from './hand/GesturePoser.js';
import { MLGestureClassifier } from './classification/MLGestureClassifier.js';
import { RuleBasedClassifier } from './classification/RuleBasedClassifier.js';
import { PlaybackController } from './playback/PlaybackController.js';
import { PlaybackUI } from './ui/PlaybackUI.js';
import type { GestureResult } from './classification/IGestureClassifier.js';

const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setPixelRatio(window.devicePixelRatio);
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.2;
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
document.body.appendChild(renderer.domElement);

window.addEventListener('resize', () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

const camera = new THREE.PerspectiveCamera(40, window.innerWidth / window.innerHeight, 0.01, 10);
camera.position.set(0, 0.1, -0.35);
camera.lookAt(0, 0.08, 0);

const scene = new THREE.Scene();
scene.background = new THREE.Color(0x0a0e1a);

// PMREMGenerator for environment map so MeshStandardMaterial reflects correctly
const pmremGenerator = new THREE.PMREMGenerator(renderer);
pmremGenerator.compileEquirectangularShader();
scene.environment = pmremGenerator.fromScene(new RoomEnvironment()).texture;

const controls = new OrbitControls(camera, renderer.domElement);
controls.target.set(0, 0.08, 0);
controls.update();

// Key light – warm
const keyLight = new THREE.DirectionalLight(0xfff4e0, 2.5);
keyLight.position.set(1.5, 2, 1);
keyLight.castShadow = true;
scene.add(keyLight);

// Fill light – cool blue-ish
const fillLight = new THREE.DirectionalLight(0xc0d8ff, 0.8);
fillLight.position.set(-2, 0.5, -1);
scene.add(fillLight);

// Rim light
const rimLight = new THREE.DirectionalLight(0xffffff, 1.0);
rimLight.position.set(0, -1, -2);
scene.add(rimLight);

// Ambient
scene.add(new THREE.AmbientLight(0x404060, 0.5));

const handRig = new HandRig();
scene.add(handRig.mesh);

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
  const result = (e as CustomEvent<{ result: GestureResult }>).detail.result;
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
