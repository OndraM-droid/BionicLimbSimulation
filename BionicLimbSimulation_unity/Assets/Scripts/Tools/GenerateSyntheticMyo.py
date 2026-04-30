"""
generate_synthetic_myo.py
Generates a synthetic .myo.csv file for testing the Bionic Limb Simulator.
Spec §3.2

Requirements: numpy
Usage: python generate_synthetic_myo.py [output_path]
"""

import sys
import numpy as np

# ── Config ──────────────────────────────────────────────────────────────────
SAMPLE_RATE = 200       # Hz
DURATION    = 10.0      # seconds
NUM_SAMPLES = int(DURATION * SAMPLE_RATE)   # 2000
SUBJECT_ID  = "SYNTH_001"
RAMP_LEN    = 20        # Hanning ramp samples (0.1 s)

GESTURES = ["rest", "fist", "open_hand", "pinch", "point", "thumbs_up", "peace", "ok"]

# Per-gesture per-channel mean activation (8 channels, based on §2.3 muscle map)
# Channels: [FDS, FCR, EDC, ECR, FPL, EPL, PT, Sup]
GESTURE_MEANS = {
    "rest":      [0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00],
    "fist":      [0.55, 0.45, 0.05, 0.05, 0.10, 0.05, 0.05, 0.05],
    "open_hand": [0.05, 0.05, 0.55, 0.45, 0.05, 0.40, 0.05, 0.05],
    "pinch":     [0.35, 0.10, 0.10, 0.05, 0.55, 0.10, 0.05, 0.05],
    "point":     [0.35, 0.10, 0.45, 0.30, 0.05, 0.05, 0.05, 0.05],
    "thumbs_up": [0.05, 0.05, 0.05, 0.05, 0.55, 0.40, 0.05, 0.05],
    "peace":     [0.30, 0.05, 0.55, 0.10, 0.05, 0.05, 0.05, 0.05],
    "ok":        [0.35, 0.10, 0.25, 0.05, 0.50, 0.05, 0.05, 0.05],
}
SIGMA = 0.08
REST_SIGMA = 0.02

# Sequence: each gesture held 1 s, separated by 0.1 s rest
# 8 gestures × (1.0 + 0.1) s = 8.8 s, rest for remainder
SEQUENCE = []
t = 0.0
for g in GESTURES[1:]:   # skip leading 'rest' in loop
    SEQUENCE.append(("rest", t, t + 0.1))
    t += 0.1
    SEQUENCE.append((g, t, t + 1.0))
    t += 1.0
# fill remainder with rest
if t < DURATION:
    SEQUENCE.append(("rest", t, DURATION))


def hanning_ramp(sig: np.ndarray, ramp_len: int) -> np.ndarray:
    """Apply Hanning ramp in/out at start and end of segment."""
    n = len(sig)
    rl = min(ramp_len, n // 2)
    ramp = np.hanning(rl * 2)
    sig[:rl] *= ramp[:rl]
    sig[-rl:] *= ramp[rl:]
    return sig


def build_signal() -> tuple[np.ndarray, np.ndarray, list[str], list[int]]:
    rng = np.random.default_rng(42)
    timestamps = np.arange(NUM_SAMPLES) * (1000.0 / SAMPLE_RATE)  # ms
    channels = np.zeros((NUM_SAMPLES, 8), dtype=np.float32)
    labels = ["rest"] * NUM_SAMPLES
    ids = [0] * NUM_SAMPLES

    for gesture, t_start, t_end in SEQUENCE:
        i_start = int(t_start * SAMPLE_RATE)
        i_end   = min(int(t_end * SAMPLE_RATE), NUM_SAMPLES)
        seg_len = i_end - i_start
        if seg_len <= 0:
            continue

        gid = GESTURES.index(gesture)
        means = GESTURE_MEANS[gesture]

        if gesture == "rest":
            seg = rng.normal(0, REST_SIGMA, (seg_len, 8)).astype(np.float32)
        else:
            seg = np.zeros((seg_len, 8), dtype=np.float32)
            for c in range(8):
                seg[:, c] = rng.normal(means[c], SIGMA, seg_len)

        # Hanning ramp per channel
        for c in range(8):
            seg[:, c] = hanning_ramp(seg[:, c], RAMP_LEN)

        # Clamp to [-1, 1]
        seg = np.clip(seg, -1.0, 1.0)
        channels[i_start:i_end] = seg

        for i in range(i_start, i_end):
            labels[i] = gesture
            ids[i] = gid

    return timestamps, channels, labels, ids


def write_csv(path: str):
    timestamps, channels, labels, ids = build_signal()
    gesture_labels_str = ",".join(GESTURES)

    with open(path, "w", encoding="utf-8", newline="") as f:
        # Metadata header
        f.write(f"# FORMAT_VERSION,1.0\n")
        f.write(f"# SAMPLE_RATE_HZ,{SAMPLE_RATE}\n")
        f.write(f"# NUM_CHANNELS,8\n")
        f.write(f"# SUBJECT_ID,{SUBJECT_ID}\n")
        f.write(f"# GESTURE_LABELS,{gesture_labels_str}\n")
        f.write(f"# DURATION_S,{DURATION}\n")
        f.write(f"# NOTES,Synthetic data generated for unit testing\n")
        # Column header
        f.write("timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_label,gesture_id\n")
        # Data rows
        for i in range(NUM_SAMPLES):
            ch = ",".join(f"{channels[i, c]:.6f}" for c in range(8))
            f.write(f"{timestamps[i]:.1f},{ch},{labels[i]},{ids[i]}\n")

    print(f"Wrote {NUM_SAMPLES} samples to: {path}")


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "synthetic_001.myo.csv"
    write_csv(out)
