using BionicLimb.Classification;
using UnityEngine;

namespace BionicLimb.Hand
{
    /// <summary>
    /// Bridges classifier output to HandRig.
    /// Receives GestureResult each frame, looks up the pose from the library,
    /// and drives HandRig.ApplyGesturePose.
    /// Spec §4 (GesturePoser)
    /// </summary>
    public class GesturePoser : MonoBehaviour
    {
        [Header("References")]
        public HandRig handRig;
        public GesturePoseLibrary poseLibrary;

        private int _currentGestureId = 0;
        private float _currentConfidence = 1f;
        private GesturePose _activePose;

        private void Start()
        {
            if (handRig == null)
                Debug.LogError("GesturePoser: HandRig reference is not set.");
            if (poseLibrary == null)
                Debug.LogError("GesturePoser: GesturePoseLibrary reference is not set.");

            SetGesture(0, 1f);
        }

        private void Update()
        {
            if (handRig == null || _activePose == null) return;
            handRig.ApplyGesturePose(_activePose, _currentConfidence);
        }

        /// <summary>Called by PlaybackController each sample tick.</summary>
        public void SetGesture(int gestureId, float confidence)
        {
            _currentGestureId = gestureId;
            _currentConfidence = confidence;
            _activePose = poseLibrary != null ? poseLibrary.GetPose(gestureId) : null;
        }

        public void SetGestureResult(GestureResult result)
        {
            SetGesture(result.GestureId, result.Confidence);
        }
    }
}
