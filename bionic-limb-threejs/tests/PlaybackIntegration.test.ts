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
});
