# Bionic Limb Simulation

A Unity 6 (URP) real-time EMG playback and gesture classification simulator for a bionic hand prosthetic. Load a `.myo.csv` EMG recording, watch it classified gesture-by-gesture, and see the procedural hand model mirror each predicted pose.

---

## Requirements

| Dependency | Version |
|---|---|
| Unity | 6000.3.10f1 |
| Universal RP | 17.3 |
| Unity Sentis (InferenceEngine) | 2.1.1 |
| Animation Rigging | 1.3.1 |
| ProBuilder | 6.0.5 |
| Python | 3.9+ |
| scikit-learn | ≥ 1.4 |
| skl2onnx | ≥ 1.16 |
| onnx | ≥ 1.14 |

---

## Quick Start

### 1. Open the project
Open `BionicLimbSimulation_unity/` in Unity Hub with Unity 6000.3.10f1.  
Unity will import packages from `Packages/manifest.json` automatically.

### 2. Open the scene
`Assets/Scenes/SampleScene.unity`  
Hit **Play** — the hand model and UI panel will appear.

### 3. Load a recording
Click **Import .myo.csv** in the bottom-left panel.  
A sample recording is already included at `Assets/StreamingAssets/SampleData/synthetic_001.myo.csv`.

### 4. Play / navigate
- **▶ / ⏸** — play or pause playback at the original sample rate
- **◀ / ▶▶** — jump to previous / next gesture transition
- The progress bar and gesture label update in real-time

---

## Generating Synthetic Data

```bash
cd BionicLimbSimulation_unity/Assets/Scripts/Tools
python GenerateSyntheticMyo.py
# Output: Assets/StreamingAssets/SampleData/synthetic_001.myo.csv
```

The script produces a 10-second, 200 Hz, 8-channel recording cycling through all 8 gestures.

---

## Training the Gesture Model

```bash
# Install Python dependencies (one-time)
pip install scikit-learn skl2onnx onnx numpy

# Train and export
cd BionicLimbSimulation_unity/Assets/Scripts/Tools
python train_gesture_model.py
```

Outputs to `Assets/Resources/ML/`:
- `GestureClassifier.onnx` — MLP classifier (128 × 64, ReLU)
- `GestureClassifier_scaler.json` — z-score normalisation parameters

The classifier extracts 32 time-domain features per window (MAV, RMS, ZCR, WL × 8 channels).  
If the ONNX is not found at runtime, the system falls back to a rule-based classifier automatically.

---

## Running Tests

### In the Unity Editor
Open **Window → General → Test Runner**.

- **EditMode** — fast, no scene required:
  - `MyoFileParserTests` — header parsing, clamping, error paths
  - `EMGPreprocessorTests` — pipeline output bounds and shape
  - `RuleBasedClassifierTests` — gesture rule coverage, probability sums
  - `GesturePoseLibraryTests` — ScriptableObject integrity
- **PlayMode** — full pipeline over real frames:
  - `PlaybackIntegrationTest` — load → play → pause → gesture output

### From command line
```bash
"<path-to-Unity>" -runTests -testPlatform EditMode -projectPath BionicLimbSimulation_unity -testResults TestResults/editmode.xml -batchmode -nographics
```

---

## Project Structure

```
BionicLimbSimulation_unity/
├── Assets/
│   ├── ML/                        # Raw ONNX output (pre-Resources copy)
│   ├── Resources/
│   │   ├── ML/                    # GestureClassifier.onnx + _scaler.json (runtime)
│   │   └── Poses/                 # GesturePoseLibrary + 8 GesturePose assets
│   ├── Scenes/
│   │   └── SampleScene.unity      # Main demo scene
│   ├── Scripts/
│   │   ├── Core/                  # MyoFileParser, EMGDataBuffer, EMGPreprocessor,
│   │   │                          #   PlaybackController
│   │   ├── Hand/                  # GesturePose, GesturePoseLibrary, HandRig,
│   │   │                          #   GesturePoser, FingerController
│   │   ├── Classification/        # IGestureClassifier, RuleBasedClassifier,
│   │   │                          #   MLGestureClassifier, ClassifierFactory
│   │   ├── UI/                    # PlaybackUI.cs
│   │   └── Tools/                 # GenerateSyntheticMyo.py, train_gesture_model.py
│   ├── StreamingAssets/
│   │   └── SampleData/            # synthetic_001.myo.csv
│   ├── Tests/
│   │   ├── EditMode/              # Unit tests
│   │   └── PlayMode/              # Integration tests
│   └── UI/
│       └── PlaybackPanel.uxml     # UI Toolkit layout
└── Packages/
    └── manifest.json
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
- **Classification**: `ClassifierFactory` selects ML (ONNX via Sentis) if available, else rule-based fallback
- **Playback timing**: accumulator-based (`accumulator += Time.deltaTime; while acc ≥ interval { advance }`) for sample-accurate replay
- **Hand rig**: 14-bone hierarchy, Slerp blending at `poseBlendSpeed = 8`, ROM clamped per joint
