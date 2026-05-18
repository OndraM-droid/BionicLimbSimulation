export interface MyoSample {
  timestamp: number;
  channels: Float32Array;
  gestureId: number;
  gestureName: string;
}

export interface MyoParseResult {
  samples: MyoSample[];
  sampleRateHz: number;
}

export class MyoFileParser {
  static readonly REQUIRED_COLUMNS: readonly string[] = [
    'timestamp_ms', 'ch0', 'ch1', 'ch2', 'ch3', 'ch4', 'ch5', 'ch6', 'ch7',
    'gesture_label', 'gesture_id'
  ];

  static parse(csvText: string): MyoParseResult {
    const lines = csvText.split('\n');
    let sampleRateHz: number | null = null;
    const dataLines: string[] = [];
    let headerLine: string | null = null;

    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed) continue;
      if (trimmed.startsWith('#')) {
        const metaMatch = trimmed.match(/^#\s*SAMPLE_RATE_HZ\s*,\s*(\d+(?:\.\d+)?)/);
        if (metaMatch) {
          sampleRateHz = parseFloat(metaMatch[1]);
        }
        continue;
      }
      if (headerLine === null) {
        headerLine = trimmed;
        continue;
      }
      dataLines.push(trimmed);
    }

    if (!headerLine) {
      throw new Error('Missing header line');
    }

    const headers = headerLine.split(',').map(h => h.trim());
    for (const col of MyoFileParser.REQUIRED_COLUMNS) {
      if (!headers.includes(col)) {
        throw new Error(`Missing header key: ${col}`);
      }
    }

    const colIndex = (name: string) => headers.indexOf(name);

    const samples: MyoSample[] = [];
    for (const line of dataLines) {
      if (!line.trim()) continue;
      const parts = line.split(',');
      const timestamp = parseFloat(parts[colIndex('timestamp_ms')]);
      const channels = new Float32Array(8);
      for (let c = 0; c < 8; c++) {
        const val = parseFloat(parts[colIndex(`ch${c}`)]);
        channels[c] = Math.max(-1, Math.min(1, val));
      }
      const gestureId = parseInt(parts[colIndex('gesture_id')], 10);
      const gestureName = parts[colIndex('gesture_label')].trim();
      samples.push({ timestamp, channels, gestureId, gestureName });
    }

    if (sampleRateHz === null) {
      if (samples.length >= 2) {
        let totalInterval = 0;
        for (let i = 1; i < samples.length; i++) {
          totalInterval += samples[i].timestamp - samples[i - 1].timestamp;
        }
        const avgIntervalMs = totalInterval / (samples.length - 1);
        sampleRateHz = 1000 / avgIntervalMs;
      } else {
        sampleRateHz = 200;
      }
    }

    return { samples, sampleRateHz };
  }
}
