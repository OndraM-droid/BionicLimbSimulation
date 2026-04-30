using NUnit.Framework;
using UnityEngine;
using BionicLimb.Hand;

namespace BionicLimb.Tests
{
    /// <summary>
    /// EditMode tests for GesturePoseLibrary ScriptableObject.
    /// </summary>
    public class GesturePoseLibraryTests
    {
        private GesturePoseLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _library = ScriptableObject.CreateInstance<GesturePoseLibrary>();
            _library.poses = new GesturePose[8];
            for (int i = 0; i < 8; i++)
            {
                var pose = ScriptableObject.CreateInstance<GesturePose>();
                pose.targetRotations = new Quaternion[GesturePose.BoneCount];
                for (int b = 0; b < GesturePose.BoneCount; b++)
                    pose.targetRotations[b] = Quaternion.identity;
                _library.poses[i] = pose;
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var p in _library.poses)
                if (p != null) Object.DestroyImmediate(p);
            Object.DestroyImmediate(_library);
        }

        [Test]
        public void GetPose_ValidIndex_ReturnsCorrectPose()
        {
            for (int i = 0; i < 8; i++)
                Assert.AreSame(_library.poses[i], _library.GetPose(i));
        }

        [Test]
        public void GetPose_OutOfRange_ReturnsNull()
        {
            Assert.IsNull(_library.GetPose(-1));
            Assert.IsNull(_library.GetPose(8));
        }

        [Test]
        public void GestureNames_HasEightEntries()
        {
            Assert.AreEqual(8, GesturePoseLibrary.GestureNames.Length);
        }

        [Test]
        public void Pose_BoneCount_Is14()
        {
            Assert.AreEqual(14, GesturePose.BoneCount);
            Assert.AreEqual(14, _library.GetPose(0).targetRotations.Length);
        }
    }
}
