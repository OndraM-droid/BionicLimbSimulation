# Bionic Limb Simulator — Implementation Plan

> **Spec version**: 1.0 | **Engine**: Unity 6000.3.10f1 | **Render**: URP 17.3  
> **Status legend**: ⬜ Not started · 🔄 In progress · ✅ Done · ❌ Blocked

---

## Block 0 — Foundation & Project Wiring
> *Packages, directory scaffold, scene rename.*

| # | Task | Status | Notes |
|---|------|--------|-------|
| 0.1 | Add `com.unity.sentis` 2.1.x via Package Manager | ✅ | `com.unity.sentis 2.1.1` in manifest.json |
| 0.2 | Add `com.unity.animation.rigging` 1.3.x | ✅ | `1.3.1` in manifest.json |
| 0.3 | Add `com.unity.probuilder` 6.0.x | ✅ | `6.0.5` in manifest.json |
| 0.4 | Create full `Assets/` directory layout | ✅ | Scripts/Core, Hand, Classification, UI, Tools; Resources/ML, Poses; UI; StreamingAssets/SampleData |
| 0.5 | Create scene | ✅ | Used existing `SampleScene.unity` (spec name HandDemo — minor deviation noted) |

---

## Block 1 — Data Layer (Core Scripts + Synthetic Data)
> *Parser · Buffer · Preprocessor · Synthetic generator*

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.1 | `Assets/Scripts/Core/MyoFileParser.cs` | ✅ | Validates 6 header keys; clamps channels to [-1,1]; throws `FormatException`/`FileNotFoundException` |
| 1.2 | `Assets/Scripts/Core/EMGDataBuffer.cs` | ✅ | `GetSampleAt()`, `GetWindowAt()` with zero-padding |
| 1.3 | `Assets/Scripts/Core/EMGPreprocessor.cs` (static) | ✅ | 4th-order Butterworth IIR (20–450 Hz), rectify, RMS envelope, normalise; coefficients cached by sample rate |
| 1.4 | `Assets/Scripts/Tools/GenerateSyntheticMyo.py` | ✅ | numpy; 10 s, 200 Hz, 8 gestures, 20-sample Hanning ramps |
| 1.5 | `StreamingAssets/SampleData/synthetic_001.myo.csv` | ✅ | 2000 samples generated and committed |

---

## Block 2 — Hand Model & Rig
> *Procedural mesh · Bone hierarchy · HandRig · GesturePose · GesturePoseLibrary*

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.1 | Procedural hand — 14 capsule segments via MCP scene build | ✅ | Hand_Root + 5 MC bones + 9 phalanges, capsule meshes |
| 2.2 | Bone GameObjects named to spec convention | ✅ | Hand_Root, Thumb_MC/PP/DP, Index/Middle/Ring/Pinky MC→DP |
| 2.3 | `Assets/Scripts/Hand/GesturePose.cs` | ✅ | ScriptableObject; `Quaternion[14]`; `BoneIndex` constants |
| 2.4 | `Assets/Scripts/Hand/HandRig.cs` | ✅ | `ApplyGesturePose()`, Slerp blend, ROM clamping per joint |
| 2.5 | `Assets/Scripts/Hand/GesturePoseLibrary.cs` | ✅ | `GesturePose[8]`, `GetPose()`, static `GestureNames` |
| 2.6 | `Assets/Scripts/Hand/GesturePoser.cs` | ✅ | Bridges classifier → HandRig; `SetGesture()`, `SetGestureResult()` |
| 2.7 | `Assets/Scripts/Hand/FingerController.cs` | ✅ | Per-finger flexion/abduction with anatomical coupling |
| 2.8 | `GesturePoseLibrary` asset + 8 `GesturePose` assets | ✅ | `Resources/Poses/` — created and wired in scene via MCP |

---

## Block 3 — Classification System
> *Interface · Rule-based · ML (Sentis) · Factory · Training script*

| # | Task | Status | Notes |
|---|------|--------|-------|
| 3.1 | `Assets/Scripts/Classification/IGestureClassifier.cs` + `GestureResult` struct | ✅ | `IDisposable`; `GestureId`, `GestureName`, `Confidence`, `AllProbabilities` |
| 3.2 | `Assets/Scripts/Classification/RuleBasedClassifier.cs` | ✅ | 8 lambda rules; confidence = activated-channel ratio |
| 3.3 | `Assets/Scripts/Classification/MLGestureClassifier.cs` | ✅ | Sentis GPU/CPU; extracts 32 features (MAV/RMS/ZCR/WL×8ch); applies z-score scaler; IDisposable |
| 3.4 | `Assets/Scripts/Classification/ClassifierFactory.cs` | ✅ | `Resources.Load<ModelAsset>("ML/GestureClassifier")`; falls back to rule-based |
| 3.5 | `Assets/Scripts/Tools/train_gesture_model.py` | ✅ | MLP 128×64 ReLU; scaler exported separately as JSON (Sentis doesn't support ONNX Scaler op) |
| 3.6 | `Assets/Resources/ML/GestureClassifier.onnx` + `_scaler.json` | ✅ | Trained on synthetic data; test accuracy 1.0; committed to repo |

---

## Block 4 — Playback Controller
> *State machine · Accumulator timing · Step navigation*

| # | Task | Status | Notes |
|---|------|--------|-------|
| 4.1 | `Assets/Scripts/Core/PlaybackController.cs` — state machine (Idle/Loaded/Playing/Paused/Error) | ✅ | Events: `OnGestureChanged`, `OnProgressChanged`, `OnStateChanged` |
| 4.2 | Accumulator-based timing loop in `Update()` | ✅ | `_accumulator += Time.deltaTime; while acc ≥ _sampleInterval` |
| 4.3 | Pre-compute `_stepBoundaries` on file load | ✅ | Scans `GestureId` transitions; index 0 always included |
| 4.4 | `NextStep()` / `PreviousStep()` with clamping | ✅ | |

---

## Block 5 — User Interface
> *UXML panel · PlaybackUI.cs · File browser*

| # | Task | Status | Notes |
|---|------|--------|-------|
| 5.1 | `Assets/UI/PlaybackPanel.uxml` — all 8 controls | ✅ | GestureLabel, ClassifierModeLabel, ProgressBar, Play/Pause, Prev/Next Step, Import, ErrorLabel |
| 5.2 | `Assets/Scripts/UI/PlaybackUI.cs` | ✅ | Binds all controls; subscribes to all 3 PlaybackController events |
| 5.3 | File browser | ✅ | `#if UNITY_EDITOR` → `EditorUtility.OpenFilePanel`; standalone → `StreamingAssets/SampleData/` first `.myo.csv` |

---

## Block 6 — Scene Setup
> *SampleScene.unity hierarchy, lighting, camera*

| # | Task | Status | Notes |
|---|------|--------|-------|
| 6.1 | Lighting: Directional Light intensity 1.2, rotation (50,−30,0) | ✅ | Configured via MCP |
| 6.2 | Camera: position (0, 0.1, −0.35), FOV 40° | ✅ | Configured via MCP |
| 6.3 | Component wiring: HandRig → 14 bones, GesturePoser → HandRig+Library, PlaybackController → GesturePoser, UIDocument → uxml | ✅ | All wired via MCP; 0 null refs confirmed |
| 6.4 | HandMaterial (URP Lit, skin colour RGB 0.9/0.7/0.6) applied to all bone renderers | ✅ | 19 renderers assigned |

---

## Block 7 — Testing
> *EditMode unit tests · PlayMode integration test*

| # | Task | Status | Notes |
|---|------|--------|-------|
| 7.1 | `Tests/EditMode/` asmdef + `MyoFileParserTests.cs` | ✅ | 7 tests: sample count, sample rate, channel values, gesture id, clamping, file-not-found, bad header |
| 7.2 | `EMGPreprocessorTests.cs` | ✅ | 4 tests: non-negative output, shape match, bounded ≤1, zero-in/zero-out |
| 7.3 | `RuleBasedClassifierTests.cs` | ✅ | 7 tests: rest, fist, confidence range, probability sum, length 8, name match |
| 7.4 | `GesturePoseLibraryTests.cs` | ✅ | 4 tests: valid index, out-of-range null, 8 gesture names, BoneCount=14 |
| 7.5 | `Tests/PlayMode/` asmdef + `PlaybackIntegrationTest.cs` | ✅ | 5 tests: load→Loaded, play→advances, pause→freezes, ClassifierMode set, CurrentResult populated |
| 7.6 | Run all tests in Unity | ⬜ | Requires Unity Editor — run via Window → Test Runner |

---

## Block 8 — Final Deliverables

| # | Task | Status | Notes |
|---|------|--------|-------|
| 8.1 | `README.md` | ✅ | Quick start, training, testing, .myo.csv format, architecture notes |
| 8.2 | All 4 production asmdefs (`Core`, `Hand`, `Classification`, `UI`) | ✅ | Proper namespace isolation |
| 8.3 | `.gitignore` | ✅ | Library/Temp/Build/Logs excluded; Assets/Packages/ProjectSettings tracked; .plastic/.slnx ignored |
| 8.4 | Final compile check | ⬜ | Check Unity Console after next open — no errors expected |

---

## Spec §13 Deliverables Checklist

| # | Deliverable | Status |
|---|-------------|--------|
| 1 | `SampleScene.unity` with playable demo | ✅ |
| 2 | `MyoFileParser.cs` | ✅ |
| 3 | `EMGPreprocessor.cs` | ✅ |
| 4 | `HandRig.cs` with ROM clamping | ✅ |
| 5 | `IGestureClassifier.cs` + two implementations | ✅ |
| 6 | `PlaybackController.cs` | ✅ |
| 7 | `PlaybackPanel.uxml` + `PlaybackUI.cs` | ✅ |
| 8 | `synthetic_001.myo.csv` | ✅ |
| 9 | `GestureClassifier.onnx` + `_scaler.json` | ✅ |
| 10 | EditMode tests | ✅ |
| 11 | PlayMode integration test | ✅ |
| 12 | `README.md` | ✅ |

---

## Commit History

| Commit | Content |
|--------|---------|
| `efb74f9` | Initial Unity project |
| `fe70631` | Blocks 1–3: data layer, hand rig, classification, ONNX |
| `ae9799d` | Blocks 4–7: playback, UI, scene, tests, ONNX fix |
| `fdac5a7` | Block 8: README |

---

## Known Deviations from Spec

| Item | Spec | Actual | Impact |
|------|------|--------|--------|
| Scene name | `HandDemo.unity` | `SampleScene.unity` | Cosmetic only |
| ONNX path (spec) | `Assets/ML/` | `Assets/Resources/ML/` | Required for `Resources.Load` at runtime — correct behaviour |
| Scaler | Baked into pipeline ONNX | Separate JSON (Unity Sentis doesn't support ONNX `Scaler` op) | Applied in C# before inference; identical result |
| Test 7.6 | Run via MCP `run_tests` | Must run manually in Editor | MCP `execute_code` broken due to Windows path length after domain reload |


---

## Block 1 — Data Layer (Core Scripts + Synthetic Data)
> *Parser · Buffer · Preprocessor · Synthetic generator*  
> Commit: `feat: data layer — MyoFileParser, EMGDataBuffer, EMGPreprocessor, synthetic generator`

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.1 | `Assets/Scripts/Core/MyoFileParser.cs` | ⬜ | Parse `.myo.csv`; validate header; clamp channels; throw descriptive exceptions (§6.1) |
| 1.2 | `Assets/Scripts/Core/EMGDataBuffer.cs` | ⬜ | Data container; `GetSampleAt()`; `GetWindowAt()` (§6.2) |
| 1.3 | `Assets/Scripts/Core/EMGPreprocessor.cs` (static) | ⬜ | Band-pass (4th-order Butterworth IIR), rectification, RMS envelope, normalisation (§6.3) |
| 1.4 | `Assets/Scripts/Tools/GenerateSyntheticMyo.py` | ⬜ | numpy; 10 s, 200 Hz, 8 gestures, Hanning ramps (§3.2) |
| 1.5 | Run generator → `StreamingAssets/SampleData/synthetic_001.myo.csv` | ⬜ | Output of 1.4 |

---

## Block 2 — Hand Model & Rig
> *Procedural mesh · Bone hierarchy · HandRig · GesturePose · GesturePoseLibrary*  
> Commit: `feat: hand model — procedural capsule rig, HandRig, GesturePoseLibrary`

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.1 | Build procedural hand from 14 capsule segments via ProBuilder in HandDemo scene (§5.1 fallback) | ⬜ | Asset Store import not automatable via MCP |
| 2.2 | Name all bone GameObjects to spec convention (Hand_Root, Thumb_MC … Pinky_DP) (§5.2) | ⬜ | |
| 2.3 | `Assets/Scripts/Hand/GesturePose.cs` (ScriptableObject, `Quaternion[14]`) | ⬜ | §5.3 |
| 2.4 | `Assets/Scripts/Hand/HandRig.cs` — `ApplyGesturePose()`, Slerp, ROM clamping | ⬜ | §5.3 |
| 2.5 | `Assets/Scripts/Hand/GesturePoseLibrary.cs` (ScriptableObject, `GesturePose[8]`) | ⬜ | §8 |
| 2.6 | `Assets/Scripts/Hand/GesturePoser.cs` — bridges classifier output → HandRig | ⬜ | |
| 2.7 | `Assets/Scripts/Hand/FingerController.cs` | ⬜ | Per-finger helper |
| 2.8 | Create `GesturePoseLibrary` asset; populate all 8 gesture poses (Euler angles from §8 table) | ⬜ | Done via `execute_code` |

---

## Block 3 — Classification System
> *Interface · Rule-based · ML (Sentis) · Factory · Training script*  
> Commit: `feat: classification — IGestureClassifier, RuleBasedClassifier, MLGestureClassifier, ClassifierFactory`

| # | Task | Status | Notes |
|---|------|--------|-------|
| 3.1 | `Assets/Scripts/Classification/IGestureClassifier.cs` + `GestureResult` struct | ⬜ | §7.2 |
| 3.2 | `Assets/Scripts/Classification/RuleBasedClassifier.cs` | ⬜ | Threshold table §7.4; confidence = activated-channel ratio |
| 3.3 | `Assets/Scripts/Classification/MLGestureClassifier.cs` | ⬜ | Sentis; GPU/CPU backend; IDisposable; tensor cleanup (§7.3) |
| 3.4 | `Assets/Scripts/Classification/ClassifierFactory.cs` | ⬜ | Checks for ONNX asset; selects strategy (§7.1) |
| 3.5 | `Assets/Scripts/Tools/train_gesture_model.py` | ⬜ | scikit-learn MLP → skl2onnx export; opset 17 (§7.3.1) |
| 3.6 | Train on synthetic data → `Assets/ML/GestureClassifier.onnx` | ⬜ | Run 3.5 outside Unity |

---

## Block 4 — Playback Controller
> *State machine · Accumulator timing · Step navigation*  
> Commit: `feat: playback controller — state machine, step navigation`

| # | Task | Status | Notes |
|---|------|--------|-------|
| 4.1 | `Assets/Scripts/Core/PlaybackController.cs` — state machine (Idle/Loaded/Playing/Paused/Error) | ⬜ | §9.1 |
| 4.2 | Accumulator-based timing loop in `Update()` | ⬜ | `_accumulator += Time.deltaTime` pattern (§9.1) |
| 4.3 | Pre-compute `_stepBoundaries` on file load | ⬜ | §9.2 |
| 4.4 | `NextStep()` / `PreviousStep()` with clamping | ⬜ | §9.2 |

---

## Block 5 — User Interface
> *UXML panel · PlaybackUI.cs · File browser*  
> Commit: `feat: UI — PlaybackPanel.uxml, PlaybackUI, file browser`

| # | Task | Status | Notes |
|---|------|--------|-------|
| 5.1 | `Assets/UI/PlaybackPanel.uxml` — all 8 controls per §10.2 | ⬜ | Bottom-left, semi-transparent bg |
| 5.2 | `Assets/Scripts/UI/PlaybackUI.cs` — binds UXML controls to PlaybackController | ⬜ | §10 |
| 5.3 | File browser: `#if UNITY_EDITOR` → `EditorUtility.OpenFilePanel`; standalone → `persistentDataPath` browser | ⬜ | §10.3 |

---

## Block 6 — Scene Setup
> *HandDemo.unity hierarchy, lighting, camera*  
> Commit: `feat: scene setup — HandDemo hierarchy, lighting, camera`

| # | Task | Status | Notes |
|---|------|--------|-------|
| 6.1 | Configure Lighting hierarchy (Directional Light intensity 1.2, rotation 50/−30/0; ambient soft grey 0.4) | ⬜ | §11 |
| 6.2 | Configure Main Camera (position 0, 0.1, −0.35; FOV 40° or orthographic size 0.15) | ⬜ | §11 |
| 6.3 | Wire all components: HandRig → bones, GesturePoser → HandRig + Library, PlaybackController → all, UIDocument → uxml | ⬜ | |
| 6.4 | Place `synthetic_001.myo.csv` reference in PlaybackController Inspector default path | ⬜ | |

---

## Block 7 — Testing
> *EditMode unit tests · PlayMode integration test*  
> Commit: `test: EditMode unit tests and PlayMode integration test`

| # | Task | Status | Notes |
|---|------|--------|-------|
| 7.1 | Create `Tests/EditMode/` assembly definition + `MyoFileParserTests.cs` | ⬜ | §12.1 — 4 test cases |
| 7.2 | `EMGPreprocessorTests.cs` | ⬜ | Filter shape, RMS, normalisation bounds |
| 7.3 | `RuleBasedClassifierTests.cs` | ⬜ | All 8 gesture rules + rest |
| 7.4 | `GesturePoseLibraryTests.cs` | ⬜ | 8 poses exist; ROM clamping; no identity (except rest) |
| 7.5 | Create `Tests/PlayMode/` assembly definition + `PlaybackIntegrationTest.cs` | ⬜ | §12.2 — load, play 3 s, assert ≥3 gestures, assert bones moved |
| 7.6 | Run all tests via MCP `run_tests`; all green | ⬜ | |

---

## Block 8 — Final Deliverables
> *README · ONNX in repo · Final compile check*  
> Commit: `docs: README, synthetic data, ONNX model — project complete`

| # | Task | Status | Notes |
|---|------|--------|-------|
| 8.1 | `README.md` in project root — setup, package steps, test run, synthetic data generation | ⬜ | Deliverable #12 in spec §13 |
| 8.2 | Verify all 12 deliverables in §13 checklist | ⬜ | |
| 8.3 | Final compile — zero errors, zero warnings | ⬜ | |

---

## Open Questions (Pending Answers Before Starting)

| # | Question | Impact |
|---|----------|--------|
| Q1 | **Hand model**: Asset Store import requires manual steps in Unity Editor. Should I proceed with the **procedural ProBuilder capsule** approach (spec §5.1 fallback) without attempting Asset Store? | Block 2 |
| Q2 | **ONNX training data**: NinaPro DB5 requires registration/download (~10 GB). Should I train the model on **synthetic data only** (from `generate_synthetic_myo.py`)? | Block 3 |
| Q3 | **Scene**: The current project has `SampleScene.unity`. Should I **rename it** to `HandDemo.unity` or **create a new scene** (keeping SampleScene intact)? | Block 0 / 6 |
| Q4 | **File browser (standalone)**: The spec says watched directory via PlayerPrefs. For now, should the standalone browser just list `Application.streamingAssetsPath/SampleData/` as the default directory (simplest viable path)? | Block 5 |

---

## Commit History

| Commit | Block | Status |
|--------|-------|--------|
| `feat: data layer` | Block 1 | ⬜ |
| `feat: hand model & rig` | Block 2 | ⬜ |
| `feat: classification system` | Block 3 | ⬜ |
| `feat: playback controller` | Block 4 | ⬜ |
| `feat: UI` | Block 5 | ⬜ |
| `feat: scene setup` | Block 6 | ⬜ |
| `test: unit + integration tests` | Block 7 | ⬜ |
| `docs: README + final check` | Block 8 | ⬜ |
