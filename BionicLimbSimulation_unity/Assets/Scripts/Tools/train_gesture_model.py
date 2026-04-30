"""
train_gesture_model.py
Trains an MLP gesture classifier on synthetic .myo.csv data and exports to ONNX.
Spec §7.3.1

Requirements: numpy, scikit-learn, skl2onnx, onnx
Usage: python train_gesture_model.py [input.myo.csv] [output.onnx]
"""

import sys
import os
import csv
import numpy as np

def load_myo_csv(path: str):
    """Load a .myo.csv file, return (channels [N,8], gesture_ids [N])."""
    channels = []
    gesture_ids = []
    sample_rate = 200
    with open(path, "r", encoding="utf-8") as f:
        reader = csv.reader(f)
        header_done = False
        col_indices = None
        for row in reader:
            if not row: continue
            if row[0].startswith("#"):
                key = row[0][1:].strip()
                if key == "SAMPLE_RATE_HZ" and len(row) > 1:
                    sample_rate = int(row[1].strip())
                continue
            if not header_done:
                col_indices = {v.strip(): i for i, v in enumerate(row)}
                header_done = True
                continue
            ch = [float(row[col_indices[f"ch{c}"]]) for c in range(8)]
            gid = int(row[col_indices["gesture_id"]])
            channels.append(ch)
            gesture_ids.append(gid)
    return np.array(channels, dtype=np.float32), np.array(gesture_ids, dtype=np.int32), sample_rate


def extract_features(channels: np.ndarray, gesture_ids: np.ndarray, sample_rate: int, window_size: int = 40):
    """
    Slide a window across the signal and extract 4 time-domain features per channel.
    Features per channel: MAV, RMS, ZCR, WL  →  4 × 8 = 32 features per window.
    """
    n = len(channels)
    half = window_size // 2
    X, y = [], []

    for center in range(half, n - half, window_size // 2):
        win = channels[center - half:center + half]  # [window_size, 8]
        label = gesture_ids[center]
        feats = []
        for c in range(8):
            sig = win[:, c]
            mav = np.mean(np.abs(sig))
            rms = np.sqrt(np.mean(sig ** 2))
            zcr = np.sum(np.diff(np.sign(sig)) != 0) / len(sig)
            wl  = np.sum(np.abs(np.diff(sig)))
            feats.extend([mav, rms, zcr, wl])
        X.append(feats)
        y.append(label)

    return np.array(X, dtype=np.float32), np.array(y, dtype=np.int32)


def train_and_export(csv_path: str, onnx_path: str):
    from sklearn.neural_network import MLPClassifier
    from sklearn.model_selection import train_test_split
    from sklearn.preprocessing import StandardScaler
    from sklearn.pipeline import Pipeline
    from skl2onnx import convert_sklearn
    from skl2onnx.common.data_types import FloatTensorType

    print(f"Loading {csv_path}...")
    channels, gesture_ids, sample_rate = load_myo_csv(csv_path)
    print(f"  {len(channels)} samples, sample_rate={sample_rate}")

    print("Extracting features...")
    X, y = extract_features(channels, gesture_ids, sample_rate)
    print(f"  Feature matrix: {X.shape}, classes: {np.unique(y)}")

    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42, stratify=y)

    pipeline = Pipeline([
        ("scaler", StandardScaler()),
        ("clf", MLPClassifier(hidden_layer_sizes=(128, 64), activation="relu", max_iter=500, random_state=42))
    ])

    print("Training...")
    pipeline.fit(X_train, y_train)
    acc = pipeline.score(X_test, y_test)
    print(f"  Test accuracy: {acc:.3f}")

    print(f"Exporting to {onnx_path}...")
    n_features = X.shape[1]
    initial_type = [("float_input", FloatTensorType([None, n_features]))]
    onnx_model = convert_sklearn(pipeline, initial_types=initial_type, target_opset=17)

    os.makedirs(os.path.dirname(onnx_path) or ".", exist_ok=True)
    with open(onnx_path, "wb") as f:
        f.write(onnx_model.SerializeToString())
    print(f"  Saved: {onnx_path}")


if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    default_csv  = os.path.join(script_dir, "..", "..", "StreamingAssets", "SampleData", "synthetic_001.myo.csv")
    default_onnx = os.path.join(script_dir, "..", "..", "ML", "GestureClassifier.onnx")

    csv_path  = sys.argv[1] if len(sys.argv) > 1 else default_csv
    onnx_path = sys.argv[2] if len(sys.argv) > 2 else default_onnx

    train_and_export(csv_path, onnx_path)
