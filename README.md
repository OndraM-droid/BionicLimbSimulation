# Bionic Limb Simulation

A real-time EMG playback and gesture classification simulator for a bionic hand prosthetic,
running entirely in the browser using Three.js + WebGL.

---

## 🌐 Browser App (Three.js)

**Live demo → [OndraM-droid.github.io/BionicLimbSimulation](https://OndraM-droid.github.io/BionicLimbSimulation/)**

No installation required — runs entirely in the browser using WebGL + WebAssembly.

### Quick Start (local dev)

```bash
cd bionic-limb-threejs
npm install
npm run dev         # http://localhost:5173
```

### Run tests & build

```bash
npm test            # Vitest unit tests (27 specs)
npm run build       # Production build → dist/
npm run lint        # ESLint (TypeScript)
```

### Three.js app structure

```
bionic-limb-threejs/
├── src/
│   ├── core/           # MyoFileParser, EMGDataBuffer, EMGPreprocessor
│   ├── hand/           # HandRig, GesturePoser, FingerController, GesturePose, GesturePoseLibrary
│   ├── classification/ # IGestureClassifier, RuleBasedClassifier, MLGestureClassifier
│   ├── playback/       # PlaybackController, PlaybackUI
│   └── main.ts         # Bootstrap: Three.js scene, animation loop, auto-load CSV
├── public/data/        # GestureClassifier_scaler.json  (place .myo.csv and .onnx here for ML)
├── styles/             # CSS
└── tests/              # 5 Vitest test files (27 specs)
```

> Full specification: [`docs/THREEJS_SPEC.md`](docs/THREEJS_SPEC.md)

---

## Running Tests

```bash
cd bionic-limb-threejs && npm test
```

| Test file | What it covers |
|---|---|
| `MyoFileParser.test.ts` | Header parsing, clamping, error paths |
| `EMGPreprocessor.test.ts` | Pipeline output bounds and shape |
| `RuleBasedClassifier.test.ts` | Gesture rule coverage, probability sums |
| `GesturePoseLibrary.test.ts` | Pose library integrity |
| `PlaybackIntegration.test.ts` | Load → play → pause → gesture output |

---

## Project Structure

```
BionicLimbSimulation/
├── bionic-limb-threejs/   # Three.js browser app (this repo)
└── docs/                  # Specification documents
```

---

## Gesture Classes

| ID | Name | Primary EMG pattern |
|---|---|---|
| 0 | rest | All channels quiet |
| 1 | fist | All 8 channels active |
| 2 | open_hand | Channels 0–1 (extensors) high |
| 3 | pinch | Channels 2–3 + 6 (index + thumb) high |
| 4 | point | Channels 2–3 (index) high, others low |
| 5 | thumbs_up | Channel 6–7 (thumb) high |
| 6 | peace | Channels 2–3 (index) + 4–5 (middle) high |
| 7 | ok | Channels 2–3 + 6 (index + thumb tip) high |

---

## .myo.csv Format

```
#FORMAT_VERSION,1
#SAMPLE_RATE_HZ,200
#NUM_CHANNELS,8
#SUBJECT_ID,subject_01
#GESTURE_LABELS,rest,fist,open_hand,pinch,point,thumbs_up,peace,ok
#DURATION_S,10.0
timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_id,gesture_label
0,-0.012,0.034,...,0,rest
5,-0.011,0.031,...,0,rest
...
```

Channel values are floats in `[-1, 1]`. Out-of-range values are clamped on import with a warning.

---

## Architecture Notes

- **EMG pipeline**: band-pass (20–450 Hz, 4th-order Butterworth) → rectify → RMS envelope → z-score normalise
- **Classification**: `main.ts` attempts to load `MLGestureClassifier` (ONNX via `onnxruntime-web`); falls back to `RuleBasedClassifier` automatically
- **Playback timing**: accumulator-based (`accumulator += delta; while acc ≥ sampleInterval { advance }`) for sample-accurate replay
- **Hand rig**: 14-bone `THREE.Bone` hierarchy, lerp blending per finger joint, ROM clamped per joint
