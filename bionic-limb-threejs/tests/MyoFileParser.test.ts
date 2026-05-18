import { describe, it, expect } from 'vitest';
import { MyoFileParser } from '../src/core/MyoFileParser.js';

const VALID_CSV = `# FORMAT_VERSION,1.0
# SAMPLE_RATE_HZ,200
# NUM_CHANNELS,8
# SUBJECT_ID,TEST
# GESTURE_LABELS,rest,fist
# DURATION_S,1.0
timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_label,gesture_id
0.0,0.1,0.2,0.3,0.4,0.5,0.6,0.7,0.8,rest,0
5.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,rest,0
10.0,0.1,0.2,0.3,0.4,0.5,0.6,0.7,0.8,fist,1
`;

describe('MyoFileParser', () => {
  it('parses correct number of samples', () => {
    expect(MyoFileParser.parse(VALID_CSV).samples.length).toBe(3);
  });

  it('reads sample rate from metadata', () => {
    const result = MyoFileParser.parse(VALID_CSV);
    expect(result.sampleRateHz).toBe(200);
  });

  it('parses channel values correctly and clamps', () => {
    const { samples } = MyoFileParser.parse(VALID_CSV);
    expect(samples[0].channels[0]).toBeCloseTo(0.1, 5);
    for (let c = 0; c < 8; c++) {
      expect(samples[0].channels[c]).toBeGreaterThanOrEqual(-1);
      expect(samples[0].channels[c]).toBeLessThanOrEqual(1);
    }
  });

  it('parses gestureId correctly', () => {
    const { samples } = MyoFileParser.parse(VALID_CSV);
    expect(samples[2].gestureId).toBe(1);
  });

  it('throws on missing required column', () => {
    const badCsv = `# header
timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_label
0.0,0,0,0,0,0,0,0,0,rest
`;
    expect(() => MyoFileParser.parse(badCsv)).toThrow('Missing header key: gesture_id');
  });

  it('clamps values above 1 to 1', () => {
    const csv = `# SAMPLE_RATE_HZ,200
timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_label,gesture_id
0.0,2.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,rest,0
`;
    const { samples } = MyoFileParser.parse(csv);
    expect(samples[0].channels[0]).toBe(1.0);
  });

  it('skips blank lines', () => {
    const csvWithBlanks = `# FORMAT_VERSION,1.0
# SAMPLE_RATE_HZ,200
timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_label,gesture_id

0.0,0.1,0.2,0.3,0.4,0.5,0.6,0.7,0.8,rest,0

5.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,rest,0

`;
    const { samples } = MyoFileParser.parse(csvWithBlanks);
    expect(samples.length).toBe(2);
  });
});
