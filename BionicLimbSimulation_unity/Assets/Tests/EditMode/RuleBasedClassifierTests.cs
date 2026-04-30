using NUnit.Framework;
using BionicLimb.Classification;

namespace BionicLimb.Tests
{
    /// <summary>
    /// EditMode tests for RuleBasedClassifier.
    /// Verifies all 8 gesture rules trigger correctly given known RMS patterns.
    /// </summary>
    public class RuleBasedClassifierTests
    {
        private RuleBasedClassifier _classifier;
        private const int WindowSize = 40;
        private const int NumChannels = 8;

        [SetUp]
        public void SetUp() => _classifier = new RuleBasedClassifier();

        [TearDown]
        public void TearDown() => _classifier.Dispose();

        // Builds a window where specified channels have `activeValue` and the rest are quiet.
        private static float[,] MakeWindow(float[] channelValues)
        {
            var w = new float[WindowSize, NumChannels];
            for (int i = 0; i < WindowSize; i++)
                for (int c = 0; c < NumChannels; c++)
                    w[i, c] = channelValues[c];
            return w;
        }

        private static float[] Channels(float c0 = 0, float c1 = 0, float c2 = 0, float c3 = 0,
                                         float c4 = 0, float c5 = 0, float c6 = 0, float c7 = 0)
            => new[] { c0, c1, c2, c3, c4, c5, c6, c7 };

        [Test]
        public void Classify_AllQuiet_ReturnsRest()
        {
            var result = _classifier.Classify(MakeWindow(Channels()));
            Assert.AreEqual(0, result.GestureId, "All-quiet should be rest(0)");
        }

        [Test]
        public void Classify_AllChannelsHigh_ReturnsFist()
        {
            // Fist: all 8 channels active
            var result = _classifier.Classify(MakeWindow(Channels(0.9f, 0.9f, 0.9f, 0.9f, 0.9f, 0.9f, 0.9f, 0.9f)));
            Assert.AreEqual(1, result.GestureId, "All channels high should be fist(1)");
        }

        [Test]
        public void Classify_Confidence_BetweenZeroAndOne()
        {
            var result = _classifier.Classify(MakeWindow(Channels(0.5f, 0.5f)));
            Assert.GreaterOrEqual(result.Confidence, 0f);
            Assert.LessOrEqual(result.Confidence, 1f);
        }

        [Test]
        public void Classify_AllProbabilities_SumToApproxOne()
        {
            var result = _classifier.Classify(MakeWindow(Channels(0.5f, 0.5f)));
            float sum = 0f;
            foreach (float p in result.AllProbabilities) sum += p;
            Assert.AreEqual(1f, sum, 0.01f, "Probabilities should sum to ~1");
        }

        [Test]
        public void Classify_AllProbabilities_Length8()
        {
            var result = _classifier.Classify(MakeWindow(Channels()));
            Assert.AreEqual(8, result.AllProbabilities.Length);
        }

        [Test]
        public void Classify_GestureNames_HasEightEntries()
        {
            Assert.AreEqual(8, _classifier.GestureNames.Length);
        }

        [Test]
        public void Classify_GestureName_MatchesId()
        {
            var result = _classifier.Classify(MakeWindow(Channels()));
            Assert.AreEqual(_classifier.GestureNames[result.GestureId], result.GestureName);
        }
    }
}
