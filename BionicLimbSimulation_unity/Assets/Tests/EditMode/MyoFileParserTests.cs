using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using BionicLimb.Core;

namespace BionicLimb.Tests
{
    /// <summary>
    /// EditMode tests for MyoFileParser.
    /// Verifies header parsing, sample reading, and error handling.
    /// </summary>
    public class MyoFileParserTests
    {
        private string _tempPath;

        [SetUp]
        public void SetUp()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.myo.csv");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempPath)) File.Delete(_tempPath);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void WriteFile(string content) => File.WriteAllText(_tempPath, content);

        private static string ValidFile(int samples = 3) =>
            "#FORMAT_VERSION,1\n" +
            "#SAMPLE_RATE_HZ,200\n" +
            "#NUM_CHANNELS,8\n" +
            "#SUBJECT_ID,test_subject\n" +
            "#GESTURE_LABELS,rest,fist,open_hand,pinch,point,thumbs_up,peace,ok\n" +
            "#DURATION_S,0.015\n" +
            "timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_id,gesture_label\n" +
            string.Concat(System.Linq.Enumerable.Range(0, samples)
                .Select(i => $"{i * 5},0.1,0.2,0.3,0.4,0.5,0.6,0.7,0.8,0,rest\n"));

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Parse_ValidFile_ReturnsSampleCount()
        {
            WriteFile(ValidFile(5));
            var buf = MyoFileParser.Parse(_tempPath);
            Assert.AreEqual(5, buf.SampleCount);
        }

        [Test]
        public void Parse_ValidFile_SampleRateParsed()
        {
            WriteFile(ValidFile(2));
            var buf = MyoFileParser.Parse(_tempPath);
            Assert.AreEqual(200, buf.SampleRate);
        }

        [Test]
        public void Parse_ValidFile_ChannelValuesCorrect()
        {
            WriteFile(ValidFile(1));
            var buf = MyoFileParser.Parse(_tempPath);
            var s = buf.GetSampleAt(0);
            Assert.AreEqual(8, s.Channels.Length);
            Assert.AreEqual(0.1f, s.Channels[0], 1e-5f);
            Assert.AreEqual(0.8f, s.Channels[7], 1e-5f);
        }

        [Test]
        public void Parse_ValidFile_GestureIdCorrect()
        {
            WriteFile(ValidFile(1));
            var buf = MyoFileParser.Parse(_tempPath);
            Assert.AreEqual(0, buf.GetSampleAt(0).GestureId);
        }

        [Test]
        public void Parse_OutOfRangeValues_AreClamped()
        {
            string content =
                "#FORMAT_VERSION,1\n#SAMPLE_RATE_HZ,200\n#NUM_CHANNELS,8\n" +
                "#SUBJECT_ID,x\n#GESTURE_LABELS,rest\n#DURATION_S,0.005\n" +
                "timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_id,gesture_label\n" +
                "0,2.0,-2.0,0.5,0.5,0.5,0.5,0.5,0.5,0,rest\n";
            WriteFile(content);
            var buf = MyoFileParser.Parse(_tempPath);
            var s = buf.GetSampleAt(0);
            Assert.LessOrEqual(s.Channels[0], 1f);
            Assert.GreaterOrEqual(s.Channels[1], -1f);
        }

        [Test]
        public void Parse_MissingFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => MyoFileParser.Parse("nonexistent.myo.csv"));
        }

        [Test]
        public void Parse_MissingHeader_ThrowsFormatException()
        {
            WriteFile("timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_id,gesture_label\n0,0,0,0,0,0,0,0,0,0,rest\n");
            Assert.Throws<FormatException>(() => MyoFileParser.Parse(_tempPath));
        }
    }
}
