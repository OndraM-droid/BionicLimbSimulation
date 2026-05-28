"""
GenerateSyntheticMyo.py
=======================
Generates a synthetic .myo.csv EMG recording suitable for use with the
Bionic Limb Simulator browser app.

Output: ../public/data/synthetic_001.myo.csv

Usage
-----
    python GenerateSyntheticMyo.py
    python GenerateSyntheticMyo.py --duration 30 --seed 42

Format produced
---------------
    # FORMAT_VERSION,1.0
    # SAMPLE_RATE_HZ,200
    # NUM_CHANNELS,8
    # SUBJECT_ID,synthetic_001
    # GESTURE_LABELS,Rest,Fist,Open,PinchIndex,PinchMiddle,Point,ThumbUp,Victory
    # DURATION_S,<duration>
    timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_label,gesture_id
    0.0,<ch0>,...,<ch7>,<gesture_label>,<gesture_id>
    ...
"""

import argparse
import os
import random

import numpy as np

# ---------------------------------------------------------------------------
# Gesture definitions
# Each gesture is characterised by a per-channel activation weight in [0, 1].
# During synthesis, active channels are driven with a burst signal; inactive
# channels receive only low-amplitude noise.
# ---------------------------------------------------------------------------
GESTURES = [
    # (id, label, activation weights per channel [ch0..ch7])
    (0, "Rest",        [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]),
    (1, "Fist",        [0.9, 0.8, 0.9, 0.8, 0.7, 0.7, 0.8, 0.7]),
    (2, "Open",        [0.8, 0.7, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]),
    (3, "PinchIndex",  [0.0, 0.0, 0.7, 0.7, 0.0, 0.0, 0.6, 0.0]),
    (4, "PinchMiddle", [0.0, 0.0, 0.0, 0.0, 0.7, 0.7, 0.6, 0.0]),
    (5, "Point",       [0.6, 0.0, 0.7, 0.7, 0.0, 0.0, 0.0, 0.0]),
    (6, "ThumbUp",     [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.8, 0.7]),
    (7, "Victory",     [0.0, 0.0, 0.7, 0.7, 0.7, 0.7, 0.0, 0.0]),
]

GESTURE_NAMES = [g[1] for g in GESTURES]
NUM_CHANNELS = 8
SAMPLE_RATE_HZ = 200


def butter_bandpass(lowcut: float, highcut: float, fs: float, order: int = 4):
    """Return (b, a) IIR Butterworth bandpass coefficients."""
    from scipy.signal import butter  # type: ignore[import-untyped]
    nyq = fs / 2.0
    return butter(order, [lowcut / nyq, highcut / nyq], btype="band")


def generate_emg_segment(
    n_samples: int,
    weights: list[float],
    fs: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """
    Generate an (n_samples, 8) EMG array for one gesture window.

    Each channel is: band-limited noise * weight + low-level noise.
    Output is clipped to [-1, 1].
    """
    try:
        from scipy.signal import lfilter  # type: ignore[import-untyped]
        b, a = butter_bandpass(20.0, min(450.0, fs * 0.49), fs)
        use_filter = True
    except ImportError:
        use_filter = False

    data = np.zeros((n_samples, NUM_CHANNELS), dtype=np.float32)
    for c in range(NUM_CHANNELS):
        noise = rng.standard_normal(n_samples).astype(np.float32)
        if use_filter:
            noise = lfilter(b, a, noise).astype(np.float32)
        # Amplitude envelope: raised cosine so the gesture ramps up/down
        t = np.linspace(0, np.pi, n_samples, dtype=np.float32)
        envelope = (1 - np.cos(t)) / 2  # 0→1→0

        amplitude = weights[c] * 0.8 + 0.05  # baseline noise floor
        data[:, c] = noise * amplitude * envelope

    # Global normalise to [-0.95, 0.95] then clip
    peak = np.abs(data).max()
    if peak > 0:
        data = data * (0.95 / peak)
    return np.clip(data, -1.0, 1.0)


def generate_recording(
    duration_s: float = 20.0,
    seed: int = 0,
) -> tuple[np.ndarray, list[int]]:
    """
    Returns (emg_array, gesture_ids_per_sample).

    emg_array shape: (n_total_samples, 8), dtype float32, values in [-1, 1].
    """
    rng = np.random.default_rng(seed)
    n_total = int(duration_s * SAMPLE_RATE_HZ)

    # Sequence of gesture segments (roughly 1–2 s each)
    gesture_sequence: list[int] = []
    min_seg = int(1.0 * SAMPLE_RATE_HZ)
    max_seg = int(2.0 * SAMPLE_RATE_HZ)

    filled = 0
    while filled < n_total:
        gid = int(rng.integers(0, len(GESTURES)))
        seg_len = int(rng.integers(min_seg, max_seg + 1))
        seg_len = min(seg_len, n_total - filled)
        gesture_sequence.extend([gid] * seg_len)
        filled += seg_len

    gesture_ids = gesture_sequence[:n_total]
    emg = np.zeros((n_total, NUM_CHANNELS), dtype=np.float32)

    # Render each contiguous segment independently
    idx = 0
    while idx < n_total:
        gid = gesture_ids[idx]
        end = idx
        while end < n_total and gesture_ids[end] == gid:
            end += 1
        seg_len = end - idx
        weights = GESTURES[gid][2]
        emg[idx:end] = generate_emg_segment(seg_len, weights, SAMPLE_RATE_HZ, rng)
        idx = end

    return emg, gesture_ids


def write_myo_csv(
    path: str,
    emg: np.ndarray,
    gesture_ids: list[int],
    subject_id: str = "synthetic_001",
) -> None:
    """Write the .myo.csv file at *path*."""
    n_samples = emg.shape[0]
    duration_s = n_samples / SAMPLE_RATE_HZ

    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)

    with open(path, "w", newline="") as fh:
        # Metadata header
        fh.write("# FORMAT_VERSION,1.0\n")
        fh.write(f"# SAMPLE_RATE_HZ,{SAMPLE_RATE_HZ}\n")
        fh.write(f"# NUM_CHANNELS,{NUM_CHANNELS}\n")
        fh.write(f"# SUBJECT_ID,{subject_id}\n")
        fh.write(f"# GESTURE_LABELS,{','.join(GESTURE_NAMES)}\n")
        fh.write(f"# DURATION_S,{duration_s:.1f}\n")

        # Column header
        ch_cols = ",".join(f"ch{i}" for i in range(NUM_CHANNELS))
        fh.write(f"timestamp_ms,{ch_cols},gesture_label,gesture_id\n")

        # Data rows
        for i in range(n_samples):
            ts = i * (1000.0 / SAMPLE_RATE_HZ)
            ch_vals = ",".join(f"{emg[i, c]:.6f}" for c in range(NUM_CHANNELS))
            gid = gesture_ids[i]
            fh.write(f"{ts:.3f},{ch_vals},{GESTURE_NAMES[gid]},{gid}\n")

    print(f"Wrote {n_samples} samples ({duration_s:.1f} s) to: {path}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate synthetic .myo.csv EMG data")
    parser.add_argument("--duration", type=float, default=20.0,
                        help="Recording duration in seconds (default: 20)")
    parser.add_argument("--seed", type=int, default=42,
                        help="Random seed for reproducibility (default: 42)")
    parser.add_argument("--output", type=str,
                        default=os.path.join(os.path.dirname(__file__),
                                             "..", "public", "data",
                                             "synthetic_001.myo.csv"),
                        help="Output path (default: ../public/data/synthetic_001.myo.csv)")
    args = parser.parse_args()

    print(f"Generating {args.duration}s synthetic EMG (seed={args.seed})…")
    emg, gesture_ids = generate_recording(duration_s=args.duration, seed=args.seed)
    write_myo_csv(args.output, emg, gesture_ids)


if __name__ == "__main__":
    main()
