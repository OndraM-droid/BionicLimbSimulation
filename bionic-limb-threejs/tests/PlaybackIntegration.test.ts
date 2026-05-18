// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { PlaybackController } from '../src/playback/PlaybackController.js';

const VALID_CSV = `# FORMAT_VERSION,1.0
# SAMPLE_RATE_HZ,200
# NUM_CHANNELS,8
# SUBJECT_ID,TEST
# GESTURE_LABELS,rest
# DURATION_S,1.0
timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_label,gesture_id
0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,rest,0
5.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,rest,0
10.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,rest,0
`;

describe('PlaybackController integration', () => {
  let controller: PlaybackController;

  beforeEach(() => {
    controller = new PlaybackController();
  });

  it('loadFile -> state Loaded', () => {
    controller.loadFile(VALID_CSV);
    expect(controller.state).toBe('Loaded');
  });

  it('play + update advances sample index', () => {
    controller.loadFile(VALID_CSV);
    controller.play();
    controller.update(1 / 200);
    expect(controller.currentSampleIndex).toBeGreaterThan(0);
  });

  it('pause stops advancement', () => {
    controller.loadFile(VALID_CSV);
    controller.play();
    controller.pause();
    const idx = controller.currentSampleIndex;
    controller.update(1 / 200);
    expect(controller.currentSampleIndex).toBe(idx);
  });

  it('default classifierMode is RuleBased', () => {
    expect(controller.classifierMode).toBe('RuleBased');
  });

  it('play + update sets currentResult', () => {
    controller.loadFile(VALID_CSV);
    controller.play();
    controller.update(1 / 200);
    expect(controller.currentResult).not.toBeNull();
  });

  it('gestureChanged event has detail.result with GestureResult shape', () => {
    controller.loadFile(VALID_CSV);
    controller.play();
    let capturedResult: unknown = null;
    controller.addEventListener('gestureChanged', (e: Event) => {
      capturedResult = (e as CustomEvent).detail.result;
    });
    controller.update(1 / 200);
    expect(capturedResult).not.toBeNull();
    const result = capturedResult as Record<string, unknown>;
    expect(typeof result.gestureId).toBe('number');
    expect(typeof result.gestureName).toBe('string');
    expect(typeof result.confidence).toBe('number');
    expect((result.confidence as number)).toBeGreaterThanOrEqual(0);
    expect((result.confidence as number)).toBeLessThanOrEqual(1);
    expect(result.allProbabilities).toBeInstanceOf(Float32Array);
  });

  it('progressChanged event has detail.progress in [0, 1]', () => {
    controller.loadFile(VALID_CSV);
    controller.play();
    let capturedProgress: number | null = null;
    controller.addEventListener('progressChanged', (e: Event) => {
      capturedProgress = (e as CustomEvent<{ progress: number }>).detail.progress;
    });
    controller.update(1 / 200);
    expect(capturedProgress).not.toBeNull();
    expect(capturedProgress as unknown as number).toBeGreaterThanOrEqual(0);
    expect(capturedProgress as unknown as number).toBeLessThanOrEqual(1);
  });

  it('stateChanged event has detail.state as PlaybackState string', () => {
    const states: string[] = [];
    controller.addEventListener('stateChanged', (e: Event) => {
      states.push((e as CustomEvent<{ state: string }>).detail.state);
    });
    controller.loadFile(VALID_CSV);
    controller.play();
    expect(states).toContain('Loaded');
    expect(states).toContain('Playing');
    for (const s of states) {
      expect(['Idle', 'Loaded', 'Playing', 'Paused', 'Error']).toContain(s);
    }
  });
});
