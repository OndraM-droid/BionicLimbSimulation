# Bionic Limb Simulator — Three.js Specification

> **Spec version**: 2.0 | **Platform**: Web (Browser) | **Renderer**: Three.js r184 (WebGLRenderer / WebGPURenderer)  
> **Language**: TypeScript 5.x | **Bundler**: Vite 5.x | **Inference**: onnxruntime-web 1.19.x  
> **Status legend**: ⬜ Not started · 🔄 In progress · ✅ Done · ❌ Blocked

---

## 1. Overview

A browser-based real-time EMG playback and gesture-classification simulator for a bionic hand prosthetic. The user loads a `.myo.csv` EMG recording, watches it classified gesture-by-gesture, and sees a procedurally built 3D hand model animate each predicted pose — all running inside a web page with no engine license or native install required.

### 1.1 Why Three.js Instead of Unity

| Concern | Unity 6 (current) | Three.js r184 (new) |
|---|---|---|
| Distribution | Desktop executable / WebGL build | URL — instant load in any browser |
| License | Unity Personal/Pro required | MIT open source |
| ML inference | Unity Sentis (ONNX, C#) | `onnxruntime-web` (ONNX, TypeScript) |
| Hand rig | `Bone` + `Animation Rigging` package | `THREE.Bone` + `THREE.SkinnedMesh` (built-in) |
| UI | UI Toolkit (UXML/USS) | HTML5 + CSS overlay |
| Testing | NUnit via Unity Test Runner | Vitest (fast, no headless browser needed for unit tests) |
| Build tooling | Unity Editor (GUI) | Vite + TypeScript compiler (CLI / CI-friendly) |
| Install size | ~500 MB Unity project | ~10 MB `node_modules` for three + ort |

---

## 2. Technology Stack

| Layer | Technology | Version |
|---|---|---|
| 3D rendering | three.js | r184 (`^0.184.0`) |
| Language | TypeScript | 5.5+ |
| Bundler / dev server | Vite | 5.4+ |
| ML inference (browser) | onnxruntime-web | 1.19.x |
| Unit testing | Vitest | 2.x |
| Linting | ESLint + `@typescript-eslint` | 9.x / 8.x |
| Python tooling (offline) | scikit-learn, skl2onnx, onnx, numpy | unchanged from current project |

---

## 3. Project Structure

```
bionic-limb-threejs/
├── index.html                        # single-page app entry point
├── vite.config.ts
├── tsconfig.json
├── package.json
├── .eslintrc.json
├── public/
│   └── data/
│       ├── synthetic_001.myo.csv     # re-used from current project
│       ├── GestureClassifier.onnx    # re-used from current project
│       └── GestureClassifier_scaler.json
├── src/
│   ├── main.ts                       # bootstrap: renderer, scene, loop, wiring
│   ├── core/
│   │   ├── MyoFileParser.ts          # §5.1 — CSV text → MyoSample[]
│   │   ├── EMGDataBuffer.ts          # §5.2 — typed-array data container
│   │   └── EMGPreprocessor.ts        # §5.3 — Butterworth IIR, rectify, RMS, normalise
│   ├── hand/
│   │   ├── HandRig.ts                # §5.4 — THREE.Bone hierarchy + SkinnedMesh
│   │   ├── GesturePose.ts            # §5.5 — Quaternion[14] pose record
│   │   ├── GesturePoseLibrary.ts     # §5.6 — 8 named gesture poses
│   │   ├── GesturePoser.ts           #        bridge: classifier result → HandRig
│   │   └── FingerController.ts       #        per-finger flexion / abduction helper
│   ├── classification/
│   │   ├── IGestureClassifier.ts     # §5.7 — interface + GestureResult
│   │   ├── RuleBasedClassifier.ts    # §5.8 — 8 channel-activation rules
│   │   └── MLGestureClassifier.ts    # §5.9 — onnxruntime-web InferenceSession
│   ├── playback/
│   │   └── PlaybackController.ts     # §5.10 — state machine + accumulator loop
│   └── ui/
│       └── PlaybackUI.ts             # §5.11 — DOM panel wired to PlaybackController
├── styles/
│   └── panel.css                     # HUD overlay styling
├── tools/
│   ├── GenerateSyntheticMyo.py       # unchanged — generates .myo.csv
│   └── train_gesture_model.py        # unchanged — exports ONNX + scaler JSON
└── tests/
    ├── MyoFileParser.test.ts
    ├── EMGPreprocessor.test.ts
    ├── RuleBasedClassifier.test.ts
    ├── GesturePoseLibrary.test.ts
    └── PlaybackIntegration.test.ts
```

---

## 4. Three.js Scene Architecture

### 4.1 Renderer and Camera Setup (`src/main.ts`)

Three.js constructs the entire scene programmatically on page load — no editor required.

```typescript
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

// Renderer
const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setPixelRatio(window.devicePixelRatio);
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
document.body.appendChild(renderer.domElement);

// Camera — matches Unity HandDemo scene
const camera = new THREE.PerspectiveCamera(40, window.innerWidth / window.innerHeight, 0.01, 10);
camera.position.set(0, 0.1, -0.35);

// Orbit controls for interactive inspection
const controls = new OrbitControls(camera, renderer.domElement);
controls.target.set(0, 0.08, 0);

// Lighting — matches Unity: Directional intensity 1.2, rotation (50, -30, 0)
const dirLight = new THREE.DirectionalLight(0xffffff, 1.2);
dirLight.position.set(0.64, 0.77, -0.26); // from euler (50°, -30°, 0°)
dirLight.castShadow = true;
scene.add(dirLight);
scene.add(new THREE.AmbientLight(0xffffff, 0.3));
```

### 4.2 Bone Hierarchy

The `THREE.Bone` tree mirrors the Unity GameObject hierarchy exactly. Bones are assembled in `HandRig.ts` and added to a `THREE.SkinnedMesh`.

```
Hand_Root (root bone)
├── Thumb_MC  → Thumb_PP → Thumb_DP
├── Index_MC  → Index_PP → Index_MP → Index_DP
├── Middle_MC → Middle_PP → Middle_MP → Middle_DP
├── Ring_MC   → Ring_PP  → Ring_MP  → Ring_DP
└── Pinky_MC  → Pinky_PP → Pinky_MP → Pinky_DP
```

**Total bone count**: 14 — matches `GesturePose.BoneCount` from the Unity spec.

Each `THREE.Bone` is a parent of the next joint, with `bone.position` set to the anatomical segment length. `CapsuleGeometry` segments are used for mesh skinning, one per phalanx.

### 4.3 Material

```typescript
// Matches Unity HandMaterial: URP Lit, skin colour RGB(0.9, 0.7, 0.6)
const handMaterial = new THREE.MeshStandardMaterial({
  color: new THREE.Color(0.9, 0.7, 0.6),
  roughness: 0.8,
  metalness: 0.0,
});
```

### 4.4 SkinnedMesh Assembly

```typescript
// One CapsuleGeometry per phalanx, merged via BufferGeometryUtils.mergeGeometries()
// or individual Mesh objects parented to the bone — implementation choice in HandRig.ts
const skeleton = new THREE.Skeleton(bones);
const skinnedMesh = new THREE.SkinnedMesh(combinedGeometry, handMaterial);
skinnedMesh.add(rootBone);
skinnedMesh.bind(skeleton);
scene.add(skinnedMesh);
```

---

## 5. Module Specifications

### 5.1 `MyoFileParser.ts`

**Unity equivalent**: `Assets/Scripts/Core/MyoFileParser.cs`

```typescript
export interface MyoSample {
  timestamp: number;
  channels: Float32Array;  // length 8, clamped to [-1, 1]
  gestureId: number;
  gestureName: string;
}

export class MyoFileParser {
  /** Parse CSV text. Throws Error with descriptive message on bad input. */
  static parse(csvText: string): MyoSample[];
}
```

**Required header keys**: `timestamp_ms`, `ch0`, `ch1`, `ch2`, `ch3`, `ch4`, `ch5`, `ch6`, `ch7`, `gesture_label`, `gesture_id`

> **Note**: The column naming scheme was updated from the Unity convention (`timestamp`, `ch1–ch8`, `gesture_name`) to match the actual `.myo.csv` files produced by `GenerateSyntheticMyo.py` and `train_gesture_model.py`. The implementation, tests, and data files all use this naming.

**Behaviour**:
- Throws `Error('Missing header key: <key>')` for any missing column
- Clamps channel values to `[-1, 1]` (identical to Unity `Mathf.Clamp`)
- Skips blank lines; trims whitespace from tokens

---

### 5.2 `EMGDataBuffer.ts`

**Unity equivalent**: `Assets/Scripts/Core/EMGDataBuffer.cs`

```typescript
export class EMGDataBuffer {
  readonly sampleCount: number;
  readonly channelCount: number;  // always 8

  /** sampleRateHz is read by PlaybackController to compute the sample interval. */
  constructor(samples: MyoSample[], sampleRateHz: number);

  /** Returns a copy of the channel values at the given index. */
  getSampleAt(index: number): Float32Array;

  /**
   * Returns a sliding window of windowSize samples ending at index.
   * Pads with zeros if index < windowSize (same as Unity zero-padding).
   */
  getWindowAt(index: number, windowSize: number): Float32Array[];
}
```

---

### 5.3 `EMGPreprocessor.ts`

**Unity equivalent**: `Assets/Scripts/Core/EMGPreprocessor.cs`

```typescript
export class EMGPreprocessor {
  /**
   * Processes an 8-channel window through:
   *   1. 4th-order Butterworth IIR band-pass (20–450 Hz)
   *   2. Full-wave rectification
   *   3. RMS envelope
   *   4. Per-channel normalisation → [0, 1]
   *
   * IIR coefficients are cached by sampleRateHz.
   * Returns Float32Array of length 8.
   */
  static process(window: Float32Array[], sampleRateHz: number): Float32Array;
}
```

The Butterworth IIR implementation is a direct TypeScript port of the Unity C# version — same filter order, same coefficient derivation, same caching strategy.

---

### 5.4 `HandRig.ts`

**Unity equivalent**: `Assets/Scripts/Hand/HandRig.cs`

```typescript
export class HandRig {
  readonly bones: THREE.Bone[];        // length 14, indexed by BoneIndex
  readonly skeleton: THREE.Skeleton;
  readonly mesh: THREE.SkinnedMesh;

  constructor();

  /**
   * Initiates a smooth slerp blend toward the given pose.
   * Stores target quaternions; actual rotation applied in update().
   */
  setTargetPose(pose: GesturePose): void;

  /**
   * Called every animation frame. Slerps each bone toward its target.
   * Applies ROM clamping per joint after slerp.
   * @param delta seconds since last frame (from THREE.Clock.getDelta())
   * @param blendSpeed normalised blend units per second
   */
  update(delta: number, blendSpeed?: number): void;
}
```

**ROM clamping**: Applied per joint as Euler angle constraints after `Quaternion.slerp()`, matching the Unity `HandRig.cs` implementation.

**Three.js slerp**: `bone.quaternion.slerp(targetQuat, t)` where `t = Math.min(blendSpeed * delta, 1)`.

---

### 5.5 `GesturePose.ts`

**Unity equivalent**: `Assets/Scripts/Hand/GesturePose.cs`

```typescript
export interface GesturePose {
  name: string;
  rotations: THREE.Quaternion[];  // length 14
}

/** Bone index constants — same ordering as Unity BoneIndex enum */
export const BoneIndex = {
  Hand_Root:  0,
  Thumb_MC:   1, Thumb_PP:   2, Thumb_DP:   3,
  Index_MC:   4, Index_PP:   5, Index_MP:   6,  Index_DP:   7,
  Middle_MC:  8, Middle_PP:  9, Middle_MP:  10, Middle_DP:  11,
  Ring_MC:   12, Ring_PP:   13,
} as const;

export const BONE_COUNT = 14;
```

---

### 5.6 `GesturePoseLibrary.ts`

**Unity equivalent**: `Assets/Scripts/Hand/GesturePoseLibrary.cs`

```typescript
export class GesturePoseLibrary {
  static readonly gestureNames: readonly string[] = [
    'Rest', 'Fist', 'Open', 'PinchIndex', 'PinchMiddle',
    'Point', 'ThumbUp', 'Victory',
  ];

  /** Returns null for out-of-range gestureId (mirrors Unity behaviour). */
  getPose(gestureId: number): GesturePose | null;
}
```

Pose data is defined as plain TypeScript constant objects (Euler angles from the Unity spec §8 converted via `new THREE.Quaternion().setFromEuler(euler)` at module load time). No `.asset` file required.

---

### 5.7 `IGestureClassifier.ts`

**Unity equivalent**: `Assets/Scripts/Classification/IGestureClassifier.cs`

```typescript
export interface GestureResult {
  gestureId: number;
  gestureName: string;
  confidence: number;
  allProbabilities: Float32Array;  // length 8, sums to 1.0
}

export interface IGestureClassifier {
  classify(features: Float32Array): GestureResult;
  dispose(): void;
}
```

---

### 5.8 `RuleBasedClassifier.ts`

**Unity equivalent**: `Assets/Scripts/Classification/RuleBasedClassifier.cs`

- 8 rule functions mapping channel activation patterns → gesture ID
- `confidence = activatedChannels / totalChannels` (same formula as Unity)
- `allProbabilities` sums to 1.0: dominant class gets `confidence`, remainder split equally

---

### 5.9 `MLGestureClassifier.ts`

**Unity equivalent**: `Assets/Scripts/Classification/MLGestureClassifier.cs`

```typescript
import * as ort from 'onnxruntime-web';

export class MLGestureClassifier implements IGestureClassifier {
  private session: ort.InferenceSession;
  private scalerMean: Float32Array;
  private scalerStd: Float32Array;

  /**
   * Async factory — loads ONNX model and scaler JSON via fetch().
   * Falls back: caller should catch and construct RuleBasedClassifier.
   */
  static async create(modelUrl: string, scalerUrl: string): Promise<MLGestureClassifier>;

  /** Extracts 32 features (MAV, RMS, ZCR, WL × 8 ch), z-scores, runs inference. */
  classify(features: Float32Array): GestureResult;

  dispose(): void;
}
```

**Feature extraction** (same 32 features as Unity `MLGestureClassifier.cs`):
- MAV — Mean Absolute Value per channel
- RMS — Root Mean Square per channel
- ZCR — Zero Crossing Rate per channel
- WL  — Waveform Length per channel

**onnxruntime-web execution providers**: `['webgl', 'wasm']` in that priority order.

---

### 5.10 `PlaybackController.ts`

**Unity equivalent**: `Assets/Scripts/Core/PlaybackController.cs`

```typescript
export type PlaybackState = 'Idle' | 'Loaded' | 'Playing' | 'Paused' | 'Error';

export class PlaybackController extends EventTarget {
  state: PlaybackState;
  currentSampleIndex: number;
  currentResult: GestureResult | null;
  classifierMode: 'ML' | 'RuleBased';

  // Events dispatched via EventTarget API:
  //   'gestureChanged'  → CustomEvent<{ result: GestureResult }>
  //   'progressChanged' → CustomEvent<{ progress: number }>   // 0–1
  //   'stateChanged'    → CustomEvent<{ state: PlaybackState }>

  loadFile(csvText: string): void;
  play(): void;
  pause(): void;
  nextStep(): void;
  previousStep(): void;

  /** Called each animation frame from the render loop. */
  update(delta: number): void;
}
```

**Accumulator timing** (identical to Unity):
```
_accumulator += delta
while (_accumulator >= _sampleInterval) {
  processSample(currentSampleIndex++)
  _accumulator -= _sampleInterval
}
```

**Step boundaries**: Pre-computed on `loadFile()` by scanning `gestureId` transitions; index 0 is always included. `nextStep()` and `previousStep()` jump to boundaries with clamping.

---

### 5.11 `PlaybackUI.ts`

**Unity equivalent**: `Assets/Scripts/UI/PlaybackUI.cs`

HTML element IDs map 1-to-1 with Unity UXML element names:

| HTML `id` | Unity UXML name | Purpose |
|---|---|---|
| `gesture-label` | `GestureLabel` | Current gesture name |
| `classifier-mode-label` | `ClassifierModeLabel` | `ML` or `Rule-Based` |
| `progress-bar` | `ProgressBar` | `<input type="range">` 0–100 |
| `btn-play-pause` | Play/Pause button | `▶` / `⏸` |
| `btn-prev-step` | Prev Step button | `◀` |
| `btn-next-step` | Next Step button | `▶▶` |
| `btn-import` | Import button | Opens `<input type="file">` |
| `error-label` | `ErrorLabel` | Hidden unless state === `Error` |

**File import** (replaces `EditorUtility.OpenFilePanel`):
```typescript
const fileInput = document.createElement('input');
fileInput.type = 'file';
fileInput.accept = '.csv';
fileInput.onchange = () => {
  const reader = new FileReader();
  reader.onload = e => controller.loadFile(e.target!.result as string);
  reader.readAsText(fileInput.files![0]);
};
fileInput.click();
```

`PlaybackUI` subscribes to all three `PlaybackController` events and updates DOM accordingly.

---

## 6. Animation Loop

```typescript
const clock = new THREE.Clock();

renderer.setAnimationLoop(() => {
  const delta = clock.getDelta();     // seconds since last frame

  playbackController.update(delta);   // advance EMG sample index, classify
  gesturePoser.update(delta);         // apply GestureResult → HandRig target
  handRig.update(delta);              // slerp bones, apply ROM clamping
  controls.update();                  // OrbitControls damping

  renderer.render(scene, camera);
});
```

This replaces Unity's `MonoBehaviour.Update()` lifecycle.

---

## 7. Gesture Pose Data

The same 8 gestures from the Unity spec §8. Euler angles are stored as TypeScript constants and converted to `THREE.Quaternion` at module-init via `quaternion.setFromEuler(euler)`.

| ID | Name | Key joint description |
|----|------|----------------------|
| 0 | Rest | All fingers neutral, slight natural curl |
| 1 | Fist | All MCP/PIP/DIP fully flexed |
| 2 | Open | All fingers fully extended, slightly abducted |
| 3 | PinchIndex | Index tip toward thumb, others neutral |
| 4 | PinchMiddle | Middle tip toward thumb, others neutral |
| 5 | Point | Index extended, others fisted |
| 6 | ThumbUp | Thumb extended upward, fingers fisted |
| 7 | Victory | Index + Middle extended, others fisted |

---

## 8. Data Files — Re-used from Current Project

All Python tooling and generated artifacts are **unchanged**. Only the runtime consumer moves from C# / Unity Sentis to TypeScript / `onnxruntime-web`.

| File | Current path | New path |
|---|---|---|
| `synthetic_001.myo.csv` | `BionicLimbSimulation_unity/Assets/StreamingAssets/SampleData/` | `public/data/` |
| `GestureClassifier.onnx` | `BionicLimbSimulation_unity/Assets/Resources/ML/` | `public/data/` |
| `GestureClassifier_scaler.json` | `BionicLimbSimulation_unity/Assets/Resources/ML/` | `public/data/` |
| `GenerateSyntheticMyo.py` | `BionicLimbSimulation_unity/Assets/Scripts/Tools/` | `tools/` |
| `train_gesture_model.py` | `BionicLimbSimulation_unity/Assets/Scripts/Tools/` | `tools/` |

---

## 9. `.myo.csv` Format

```
# FORMAT_VERSION,1.0
# SAMPLE_RATE_HZ,200
# NUM_CHANNELS,8
timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_label,gesture_id
0.000,0.12,-0.34,0.05,0.78,-0.22,0.11,-0.45,0.33,Fist,1
5.000,0.11,-0.33,0.06,0.77,-0.21,0.12,-0.44,0.34,Fist,1
...
```

- `timestamp_ms`: milliseconds from recording start
- `ch0`–`ch7`: EMG channel amplitudes (8 channels, zero-indexed), expected range `[-1, 1]`, clamped on parse
- `gesture_label`: human-readable string label (matches `GesturePoseLibrary.gestureNames`)
- `gesture_id`: integer 0–7

> **Note**: Comment lines beginning with `#` are metadata and are skipped by the parser.

---

## 10. Testing Strategy

### 10.1 Unit Tests (Vitest)

No headless browser, no Unity Editor. Vitest runs in Node with JSDOM for DOM-touching tests.

| Test file | Coverage | Count |
|---|---|---|
| `MyoFileParser.test.ts` | Header validation, clamping, `Error` on bad input, sample count, sample rate, channel values, gesture id | 7 |
| `EMGPreprocessor.test.ts` | Non-negative output, shape match, values ≤ 1, zero-in/zero-out | 4 |
| `RuleBasedClassifier.test.ts` | Rest, Fist, confidence in range, probabilities sum to 1, array length 8, name match | 7 |
| `GesturePoseLibrary.test.ts` | Valid index returns pose, OOB returns null, 8 gesture names, `BONE_COUNT === 14` | 4 |

Run:
```bash
npx vitest run
# or watch mode:
npx vitest
```

### 10.2 Integration Test

`PlaybackIntegration.test.ts` — JSDOM environment, tests:

1. `loadFile(csvText)` → state transitions to `'Loaded'`
2. `play()` followed by `update(sampleInterval)` → `currentSampleIndex` advances
3. `pause()` followed by `update(sampleInterval)` → `currentSampleIndex` frozen
4. `classifierMode` is set to `'ML'` when ONNX loads; `'RuleBased'` on fallback
5. `currentResult` is non-null after first sample processed

---

## 11. Quick Start

### 11.1 Prerequisites

```
Node.js 20+
npm 10+
(Python 3.9+ only needed for data generation / retraining)
```

### 11.2 Install and run

```bash
npm install
npm run dev       # Vite dev server → http://localhost:5173
```

### 11.3 Build for production

```bash
npm run build     # outputs to dist/
npm run preview   # locally serve dist/
```

### 11.4 Run tests

```bash
npm test          # vitest run (CI mode)
npm run test:ui   # vitest UI (browser-based test explorer)
```

### 11.5 Regenerate sample data (optional)

```bash
cd tools
python GenerateSyntheticMyo.py
# → ../public/data/synthetic_001.myo.csv
```

### 11.6 Retrain classifier (optional)

```bash
pip install scikit-learn skl2onnx onnx numpy
cd tools
python train_gesture_model.py
# → ../public/data/GestureClassifier.onnx
# → ../public/data/GestureClassifier_scaler.json
```

---

## 12. `package.json` Key Dependencies

```json
{
  "dependencies": {
    "three": "^0.184.0",
    "onnxruntime-web": "^1.19.0"
  },
  "devDependencies": {
    "typescript": "^5.5.0",
    "vite": "^5.4.0",
    "vitest": "^2.0.0",
    "jsdom": "^24.0.0",
    "@types/three": "^0.184.0",
    "eslint": "^9.0.0",
    "@typescript-eslint/eslint-plugin": "^8.0.0",
    "@typescript-eslint/parser": "^8.0.0"
  },
  "scripts": {
    "dev": "vite",
    "build": "tsc && vite build",
    "preview": "vite preview",
    "test": "vitest run",
    "test:ui": "vitest --ui",
    "lint": "eslint src tests"
  }
}
```

---

## 13. Deliverables Checklist

| # | Deliverable | Three.js equivalent | Status |
|---|---|---|---|
| 1 | Playable demo (single URL) | `index.html` via Vite | ⬜ |
| 2 | `MyoFileParser.ts` | §5.1 | ⬜ |
| 3 | `EMGPreprocessor.ts` | §5.3 | ⬜ |
| 4 | `HandRig.ts` with ROM clamping | §5.4 | ⬜ |
| 5 | `IGestureClassifier.ts` + 2 implementations | §5.7–5.9 | ⬜ |
| 6 | `PlaybackController.ts` | §5.10 | ⬜ |
| 7 | `index.html` panel + `PlaybackUI.ts` | §5.11 | ⬜ |
| 8 | `synthetic_001.myo.csv` | `public/data/` (re-used) | ⬜ |
| 9 | `GestureClassifier.onnx` + `_scaler.json` | `public/data/` (re-used) | ⬜ |
| 10 | Unit tests (Vitest) | §10.1 | ⬜ |
| 11 | Integration test | §10.2 | ⬜ |
| 12 | `README.md` | Updated for web stack | ⬜ |

---

## 14. Mapping: Unity → Three.js API Reference

| Unity concept | Three.js equivalent |
|---|---|
| `Transform.localRotation` | `THREE.Bone.quaternion` |
| `Quaternion.Slerp(a, b, t)` | `a.clone().slerp(b, t)` or `a.slerp(b, t)` |
| `new Quaternion.Euler(x,y,z)` | `new THREE.Quaternion().setFromEuler(new THREE.Euler(x, y, z))` |
| `CapsuleGeometry` primitive | `new THREE.CapsuleGeometry(radius, height, capSegments, radialSegments)` |
| `GameObject` with children | `new THREE.Object3D()` with `.add()` |
| `Bone` + `Skeleton` | `THREE.Bone` + `THREE.Skeleton` |
| `SkinnedMesh` | `THREE.SkinnedMesh` |
| `MeshRenderer` with URP Lit | `THREE.MeshStandardMaterial` or `THREE.MeshPhysicalMaterial` |
| `MonoBehaviour.Update()` | Callback inside `renderer.setAnimationLoop()` |
| `Time.deltaTime` | `clock.getDelta()` (`THREE.Clock`) |
| `ScriptableObject` asset | Plain TypeScript `const` / JSON file loaded via `fetch()` |
| `Resources.Load<T>()` | `fetch('/data/filename')` |
| `EditorUtility.OpenFilePanel` | `<input type="file">` |
| `StreamingAssets/` folder | `public/` folder (served at root by Vite) |
| `Assembly Definition` (asmdef) | ES module with TypeScript path aliases |
| `UnityEvent` / C# event | `EventTarget` + `CustomEvent` |
| Unity Sentis `InferenceSession` | `onnxruntime-web` `InferenceSession` |
| UI Toolkit UXML/USS | HTML5 + CSS |
| Unity NUnit (EditMode tests) | Vitest unit tests |
| Unity PlayMode integration test | Vitest integration test with JSDOM |
| `SkeletonHelper` (debug) | `new THREE.SkeletonHelper(skinnedMesh)` |
| `OrbitControls` (not in Unity) | `OrbitControls` from `three/addons` |

---

## 15. Known Differences from Unity Implementation

| Item | Unity behaviour | Three.js behaviour |
|---|---|---|
| Slerp source of truth | `Time.deltaTime` accumulated in `Update()` | `THREE.Clock.getDelta()` in animation loop callback |
| ONNX execution | Sentis GPU backend (Metal/DX12/Vulkan) | `onnxruntime-web` WebGL EP or WASM fallback |
| File dialog | Blocks thread; returns path; `File.ReadAllText()` | Non-blocking; `FileReader` async API |
| Scaler loading | Parsed once in `Awake()` | `await fetch()` in `MLGestureClassifier.create()` |
| State machine dispatch | C# `event Action<T>` delegates | `EventTarget.dispatchEvent(new CustomEvent(...))` |
| Pose assets | Unity `.asset` ScriptableObject files | TypeScript `const` objects (no serialisation format) |
| Test isolation | Domain reload between EditMode test runs | Vitest module isolation per test file |
| Scene coordinate system | Left-handed Y-up (Unity) | Right-handed Y-up (Three.js) — bone positions must flip Z |
| Shadow mapping | URP shadow cascades | `THREE.PCFSoftShadowMap` (simpler, sufficient for a hand) |
| Render pipeline | URP 17.3 | Three.js WebGLRenderer (no separate pipeline config needed) |
