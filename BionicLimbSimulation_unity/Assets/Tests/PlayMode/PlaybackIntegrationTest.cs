using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BionicLimb.Core;
using BionicLimb.Hand;
using BionicLimb.Classification;

namespace BionicLimb.Tests
{
    /// <summary>
    /// PlayMode integration test: verifies the full pipeline from file load
    /// through classification and pose output over real frames.
    /// </summary>
    public class PlaybackIntegrationTest
    {
        private GameObject _controllerGO;
        private PlaybackController _controller;
        private string _tempCsvPath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Build minimal scene
            _controllerGO = new GameObject("PlaybackController");
            _controller = _controllerGO.AddComponent<PlaybackController>();

            // Create a GesturePoser with stub HandRig
            var handGO = new GameObject("Hand_Root");
            var handRig = handGO.AddComponent<HandRig>();
            handRig.fingerBones = new Transform[14];
            for (int i = 0; i < 14; i++)
            {
                var bone = new GameObject($"Bone_{i}").transform;
                bone.SetParent(handGO.transform);
                handRig.fingerBones[i] = bone;
            }

            var library = ScriptableObject.CreateInstance<GesturePoseLibrary>();
            library.poses = new GesturePose[8];
            for (int i = 0; i < 8; i++)
            {
                var pose = ScriptableObject.CreateInstance<GesturePose>();
                pose.targetRotations = new Quaternion[GesturePose.BoneCount];
                for (int b = 0; b < GesturePose.BoneCount; b++)
                    pose.targetRotations[b] = Quaternion.identity;
                library.poses[i] = pose;
            }

            var poser = handGO.AddComponent<GesturePoser>();
            poser.handRig = handRig;
            poser.poseLibrary = library;

            _controller.gesturePoser = poser;

            // Write a minimal .myo.csv to a temp file
            _tempCsvPath = Path.Combine(Application.temporaryCachePath, "integration_test.myo.csv");
            WriteSyntheticCsv(_tempCsvPath, samples: 200, sampleRate: 200);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.DestroyImmediate(_controllerGO);
            if (File.Exists(_tempCsvPath)) File.Delete(_tempCsvPath);
            yield return null;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator LoadFile_TransitionsToLoadedState()
        {
            _controller.LoadFile(_tempCsvPath);
            yield return null;

            Assert.AreEqual(PlaybackController.PlaybackState.Loaded, _controller.State,
                $"Expected Loaded, got {_controller.State}: {_controller.ErrorMessage}");
        }

        [UnityTest]
        public IEnumerator Play_AdvancesSampleIndex()
        {
            _controller.LoadFile(_tempCsvPath);
            yield return null;

            _controller.Play();
            yield return new WaitForSeconds(0.1f); // allow several samples to process

            Assert.Greater(_controller.CurrentSampleIndex, 0, "Sample index should have advanced.");
        }

        [UnityTest]
        public IEnumerator Play_Pause_StopsAdvancing()
        {
            _controller.LoadFile(_tempCsvPath);
            yield return null;

            _controller.Play();
            yield return new WaitForSeconds(0.05f);

            _controller.Pause();
            int indexAtPause = _controller.CurrentSampleIndex;
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(indexAtPause, _controller.CurrentSampleIndex, "Index should freeze after Pause.");
        }

        [UnityTest]
        public IEnumerator ClassifierMode_IsSet_AfterLoad()
        {
            _controller.LoadFile(_tempCsvPath);
            yield return null;

            Assert.IsNotEmpty(_controller.ClassifierMode, "ClassifierMode should be set after load.");
        }

        [UnityTest]
        public IEnumerator CurrentResult_IsSet_AfterPlayback()
        {
            _controller.LoadFile(_tempCsvPath);
            yield return null;
            _controller.Play();
            yield return new WaitForSeconds(0.15f);

            Assert.IsNotNull(_controller.CurrentResult, "CurrentResult should be populated.");
            Assert.IsNotNull(_controller.CurrentResult.GestureName);
        }

        // ── CSV helper ────────────────────────────────────────────────────────

        private static void WriteSyntheticCsv(string path, int samples, int sampleRate)
        {
            using var w = new StreamWriter(path);
            w.WriteLine("#FORMAT_VERSION,1");
            w.WriteLine($"#SAMPLE_RATE_HZ,{sampleRate}");
            w.WriteLine("#NUM_CHANNELS,8");
            w.WriteLine("#SUBJECT_ID,integration_test");
            w.WriteLine("#GESTURE_LABELS,rest,fist,open_hand,pinch,point,thumbs_up,peace,ok");
            w.WriteLine($"#DURATION_S,{(double)samples / sampleRate:F3}");
            w.WriteLine("timestamp_ms,ch0,ch1,ch2,ch3,ch4,ch5,ch6,ch7,gesture_id,gesture_label");
            for (int i = 0; i < samples; i++)
            {
                int gid = (i * 8) / samples; // sweep through all 8 gestures
                string glabel = new[] { "rest","fist","open_hand","pinch","point","thumbs_up","peace","ok" }[gid];
                float v = gid > 0 ? 0.6f : 0.05f;
                w.WriteLine($"{i * (1000 / sampleRate)},{v},{v},{v},{v},{v},{v},{v},{v},{gid},{glabel}");
            }
        }
    }
}
