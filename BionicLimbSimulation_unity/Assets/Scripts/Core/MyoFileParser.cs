using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BionicLimb.Core
{
    /// <summary>
    /// Parses .myo.csv files into an EMGDataBuffer.
    /// Spec §6.1
    /// </summary>
    public static class MyoFileParser
    {
        private const int RequiredChannels = 8;

        public static EMGDataBuffer Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"MyoFileParser: file not found: {filePath}");

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var timestamps = new List<double>();
            var channels = new List<float[]>();
            var gestureLabels = new List<string>();
            var gestureIds = new List<int>();

            bool headerRowParsed = false;
            int lineNumber = 0;

            using var reader = new StreamReader(filePath);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                line = line.Trim();
                if (line.Length == 0) continue;

                // Metadata lines
                if (line.StartsWith("#"))
                {
                    int comma = line.IndexOf(',');
                    if (comma < 0) continue;
                    string key = line.Substring(1, comma - 1).Trim();
                    string value = line.Substring(comma + 1).Trim();
                    metadata[key] = value;
                    continue;
                }

                // Column header row (first non-# line)
                if (!headerRowParsed)
                {
                    ValidateColumnHeader(line, lineNumber);
                    headerRowParsed = true;
                    continue;
                }

                // Data rows
                ParseDataRow(line, lineNumber, timestamps, channels, gestureLabels, gestureIds);
            }

            ValidateMetadata(metadata);

            if (!int.TryParse(metadata["SAMPLE_RATE_HZ"], out int sampleRate))
                throw new FormatException("MyoFileParser: SAMPLE_RATE_HZ is not a valid integer.");

            return new EMGDataBuffer(
                timestamps.ToArray(),
                ToChannelArray(channels),
                gestureLabels.ToArray(),
                gestureIds.ToArray(),
                sampleRate
            );
        }

        private static void ValidateColumnHeader(string line, int lineNumber)
        {
            var cols = line.Split(',');
            string[] expected = { "timestamp_ms", "ch0", "ch1", "ch2", "ch3", "ch4", "ch5", "ch6", "ch7", "gesture_label", "gesture_id" };
            if (cols.Length < expected.Length)
                throw new FormatException($"MyoFileParser: column header at line {lineNumber} has too few columns. Expected: {string.Join(",", expected)}");
            for (int i = 0; i < expected.Length; i++)
            {
                if (!string.Equals(cols[i].Trim(), expected[i], StringComparison.OrdinalIgnoreCase))
                    throw new FormatException($"MyoFileParser: column '{cols[i].Trim()}' at index {i} (line {lineNumber}). Expected '{expected[i]}'.");
            }
        }

        private static void ParseDataRow(
            string line, int lineNumber,
            List<double> timestamps, List<float[]> channels,
            List<string> gestureLabels, List<int> gestureIds)
        {
            var parts = line.Split(',');
            if (parts.Length < 11)
                throw new FormatException($"MyoFileParser: data row at line {lineNumber} has {parts.Length} columns, expected at least 11.");

            if (!double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double ts))
                throw new FormatException($"MyoFileParser: invalid timestamp_ms at line {lineNumber}: '{parts[0]}'.");
            timestamps.Add(ts);

            float[] ch = new float[RequiredChannels];
            for (int i = 0; i < RequiredChannels; i++)
            {
                if (!float.TryParse(parts[1 + i].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float val))
                    throw new FormatException($"MyoFileParser: invalid channel value ch{i} at line {lineNumber}.");
                if (val < -1f || val > 1f)
                    Debug.LogWarning($"MyoFileParser: ch{i} value {val} at line {lineNumber} is out of [-1,1]. Clamping.");
                ch[i] = Mathf.Clamp(val, -1f, 1f);
            }
            channels.Add(ch);

            gestureLabels.Add(parts[9].Trim());

            if (!int.TryParse(parts[10].Trim(), out int gid))
                throw new FormatException($"MyoFileParser: invalid gesture_id at line {lineNumber}.");
            gestureIds.Add(gid);
        }

        private static void ValidateMetadata(Dictionary<string, string> meta)
        {
            string[] required = { "FORMAT_VERSION", "SAMPLE_RATE_HZ", "NUM_CHANNELS", "SUBJECT_ID", "GESTURE_LABELS", "DURATION_S" };
            foreach (var key in required)
            {
                if (!meta.ContainsKey(key))
                    throw new FormatException($"MyoFileParser: required metadata field '{key}' is missing.");
            }
            if (meta["FORMAT_VERSION"] != "1.0")
                throw new FormatException($"MyoFileParser: unsupported FORMAT_VERSION '{meta["FORMAT_VERSION"]}'. Expected '1.0'.");
            if (meta["NUM_CHANNELS"] != RequiredChannels.ToString())
                throw new FormatException($"MyoFileParser: NUM_CHANNELS must be {RequiredChannels}, got '{meta["NUM_CHANNELS"]}'.");
        }

        private static float[,] ToChannelArray(List<float[]> rows)
        {
            int n = rows.Count;
            var result = new float[n, RequiredChannels];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < RequiredChannels; j++)
                    result[i, j] = rows[i][j];
            return result;
        }
    }
}
