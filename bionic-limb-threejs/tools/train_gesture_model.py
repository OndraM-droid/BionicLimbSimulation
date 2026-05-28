"""
train_gesture_model.py
======================
Trains a scikit-learn MLPClassifier on synthetic .myo.csv data, then exports:
  • GestureClassifier.onnx             (for onnxruntime-web in the browser)
  • GestureClassifier_scaler.json      (StandardScaler mean + scale arrays)

Both files are written to ../public/data/ so the Vite dev server and the
GitHub Pages deploy serve them at /data/<filename>.

Usage
-----
    pip install scikit-learn skl2onnx onnx numpy
    python train_gesture_model.py
    python train_gesture_model.py --csv ../public/data/synthetic_001.myo.csv

Feature extraction (32 features = 4 features × 8 channels)
-----------------------------------------------------------
For a sliding window of EMG samples, per channel:
  • MAV  — Mean Absolute Value
  • RMS  — Root Mean Square
  • ZCR  — Zero Crossing Rate
  • WL   — Waveform Length

These are the same 32 features extracted by MLGestureClassifier.ts at
inference time, so training and inference are consistent.
"""

import argparse
import json
import os
import sys

import numpy as np

# ---------------------------------------------------------------------------
# Constants must match MLGestureClassifier.ts and PlaybackController.ts
# ---------------------------------------------------------------------------
NUM_CHANNELS = 8
NUM_FEATURES = 32   # 4 features × 8 channels
WINDOW_SIZE = 40    # samples — matches WINDOW_SIZE in src/core/constants.ts
SAMPLE_RATE_HZ = 200
GESTURE_NAMES = ["Rest", "Fist", "Open", "PinchIndex", "PinchMiddle",
                 "Point", "ThumbUp", "Victory"]


# ---------------------------------------------------------------------------
# Feature extraction
# ---------------------------------------------------------------------------

def extract_features(window: np.ndarray) -> np.ndarray:
    """
    window shape: (WINDOW_SIZE, NUM_CHANNELS)
    Returns: float32 array of shape (NUM_FEATURES,)
    """
    feats = np.zeros(NUM_FEATURES, dtype=np.float32)
    for c in range(NUM_CHANNELS):
        col = window[:, c].astype(np.float64)
        n = len(col)

        mav = np.mean(np.abs(col))
        rms = np.sqrt(np.mean(col ** 2))

        signs = np.sign(col)
        zc = np.sum(signs[1:] != signs[:-1]) / max(n - 1, 1)

        wl = np.sum(np.abs(np.diff(col)))

        feats[c * 4 + 0] = float(mav)
        feats[c * 4 + 1] = float(rms)
        feats[c * 4 + 2] = float(zc)
        feats[c * 4 + 3] = float(wl)

    return feats


# ---------------------------------------------------------------------------
# CSV loading
# ---------------------------------------------------------------------------

def load_csv(path: str) -> tuple[np.ndarray, np.ndarray]:
    """
    Returns (emg, gesture_ids):
      emg          — float32 (n_samples, 8)
      gesture_ids  — int32   (n_samples,)
    """
    emg_rows: list[list[float]] = []
    gids: list[int] = []

    with open(path) as fh:
        header_found = False
        col_ch: list[int] = []
        col_gid = -1

        for line in fh:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            parts = [p.strip() for p in line.split(",")]
            if not header_found:
                # First non-comment, non-blank line is the header
                col_ch = [parts.index(f"ch{i}") for i in range(NUM_CHANNELS)]
                col_gid = parts.index("gesture_id")
                header_found = True
                continue
            try:
                emg_rows.append([float(parts[c]) for c in col_ch])
                gids.append(int(parts[col_gid]))
            except (ValueError, IndexError):
                continue  # skip malformed rows

    emg = np.array(emg_rows, dtype=np.float32)
    gesture_ids = np.array(gids, dtype=np.int32)
    print(f"Loaded {len(emg)} samples from {path}")
    return emg, gesture_ids


# ---------------------------------------------------------------------------
# Dataset construction — sliding window with stride = WINDOW_SIZE
# ---------------------------------------------------------------------------

def build_dataset(
    emg: np.ndarray,
    gesture_ids: np.ndarray,
) -> tuple[np.ndarray, np.ndarray]:
    """
    Extract non-overlapping windows of size WINDOW_SIZE.
    Label each window with the gesture_id at the window's last sample
    (matches PlaybackController._processSample behaviour).
    """
    X_list: list[np.ndarray] = []
    y_list: list[int] = []

    n = len(emg)
    for start in range(0, n - WINDOW_SIZE + 1, WINDOW_SIZE):
        window = emg[start: start + WINDOW_SIZE]
        label = int(gesture_ids[start + WINDOW_SIZE - 1])
        feats = extract_features(window)
        X_list.append(feats)
        y_list.append(label)

    X = np.array(X_list, dtype=np.float32)
    y = np.array(y_list, dtype=np.int32)
    print(f"  → {len(X)} windows extracted (stride={WINDOW_SIZE})")
    return X, y


# ---------------------------------------------------------------------------
# Training
# ---------------------------------------------------------------------------

def train(X_train: np.ndarray, y_train: np.ndarray):  # type: ignore[return]
    """Train an MLPClassifier and return (model, scaler)."""
    from sklearn.neural_network import MLPClassifier  # type: ignore[import-untyped]
    from sklearn.preprocessing import StandardScaler  # type: ignore[import-untyped]

    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(X_train)

    clf = MLPClassifier(
        hidden_layer_sizes=(128, 64),
        activation="relu",
        max_iter=500,
        random_state=42,
        verbose=False,
    )
    clf.fit(X_scaled, y_train)

    train_acc = clf.score(X_scaled, y_train)
    print(f"  Training accuracy: {train_acc * 100:.1f}%")

    return clf, scaler


# ---------------------------------------------------------------------------
# Export
# ---------------------------------------------------------------------------

def export_onnx(clf, scaler, output_dir: str) -> None:  # type: ignore[type-arg]
    """
    Convert scikit-learn MLPClassifier → ONNX and write to output_dir.
    Writes both GestureClassifier.onnx and GestureClassifier_scaler.json.
    """
    from skl2onnx import convert_sklearn  # type: ignore[import-untyped]
    from skl2onnx.common.data_types import FloatTensorType  # type: ignore[import-untyped]

    os.makedirs(output_dir, exist_ok=True)

    # ── ONNX model ──────────────────────────────────────────────────────────
    initial_type = [("float_input", FloatTensorType([None, NUM_FEATURES]))]
    onnx_model = convert_sklearn(
        clf,
        initial_types=initial_type,
        target_opset=15,
        options={"zipmap": False},
    )

    onnx_path = os.path.join(output_dir, "GestureClassifier.onnx")
    with open(onnx_path, "wb") as fh:
        fh.write(onnx_model.SerializeToString())
    print(f"Wrote ONNX model → {onnx_path}")

    # ── Scaler JSON ─────────────────────────────────────────────────────────
    scaler_path = os.path.join(output_dir, "GestureClassifier_scaler.json")
    scaler_data = {
        "mean": scaler.mean_.tolist(),
        "scale": scaler.scale_.tolist(),
    }
    with open(scaler_path, "w") as fh:
        json.dump(scaler_data, fh, indent=2)
    print(f"Wrote scaler JSON → {scaler_path}")


# ---------------------------------------------------------------------------
# Entrypoint
# ---------------------------------------------------------------------------

def main() -> None:
    default_csv = os.path.join(
        os.path.dirname(__file__), "..", "public", "data", "synthetic_001.myo.csv"
    )
    default_out = os.path.join(
        os.path.dirname(__file__), "..", "public", "data"
    )

    parser = argparse.ArgumentParser(
        description="Train gesture classifier and export ONNX + scaler JSON"
    )
    parser.add_argument(
        "--csv",
        default=default_csv,
        help=f"Input .myo.csv path (default: {default_csv})",
    )
    parser.add_argument(
        "--output-dir",
        default=default_out,
        help=f"Output directory for .onnx and _scaler.json (default: {default_out})",
    )
    args = parser.parse_args()

    if not os.path.isfile(args.csv):
        print(f"ERROR: CSV file not found: {args.csv}", file=sys.stderr)
        print("Run GenerateSyntheticMyo.py first to create the training data.",
              file=sys.stderr)
        sys.exit(1)

    try:
        import sklearn  # noqa: F401  # type: ignore[import-untyped]
        import skl2onnx  # noqa: F401  # type: ignore[import-untyped]
    except ImportError as exc:
        print(f"Missing dependency: {exc}", file=sys.stderr)
        print("Install with: pip install scikit-learn skl2onnx onnx numpy",
              file=sys.stderr)
        sys.exit(1)

    print("Loading CSV…")
    emg, gesture_ids = load_csv(args.csv)

    print("Building feature dataset…")
    X, y = build_dataset(emg, gesture_ids)

    print("Training MLPClassifier…")
    clf, scaler = train(X, y)

    print("Exporting ONNX + scaler JSON…")
    export_onnx(clf, scaler, args.output_dir)

    print("Done.")


if __name__ == "__main__":
    main()
