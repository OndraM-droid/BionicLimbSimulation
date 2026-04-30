using UnityEngine;

namespace BionicLimb.Hand
{
    /// <summary>
    /// Per-finger helper that exposes individual finger flexion/abduction
    /// as normalised [0,1] values, for procedural animation and debug visualisation.
    /// Spec §4 (FingerController)
    /// </summary>
    public class FingerController : MonoBehaviour
    {
        [Header("Phalanx Transforms")]
        public Transform proximal;
        public Transform middle;   // null for thumb
        public Transform distal;

        [Header("ROM (degrees)")]
        public float maxFlexion = 90f;
        public float maxAbduction = 20f;

        [Range(0f, 1f)] public float flexion;
        [Range(-1f, 1f)] public float abduction;

        private Quaternion _proximalRest, _middleRest, _distalRest;

        private void Awake()
        {
            _proximalRest = proximal != null ? proximal.localRotation : Quaternion.identity;
            _middleRest   = middle   != null ? middle.localRotation   : Quaternion.identity;
            _distalRest   = distal   != null ? distal.localRotation   : Quaternion.identity;
        }

        /// <summary>
        /// Drive the finger directly by flexion [0,1] and abduction [-1,1].
        /// Applies anatomical coupling: MP = 0.9 × PP, DP = 0.7 × PP flexion.
        /// Spec §8 note on proportional extrapolation.
        /// </summary>
        public void Apply(float flex, float abd)
        {
            flexion   = Mathf.Clamp01(flex);
            abduction = Mathf.Clamp(abd, -1f, 1f);

            float ppDeg = flexion * maxFlexion;
            float mpDeg = ppDeg * 0.9f;
            float dpDeg = ppDeg * 0.7f;
            float abdDeg = abduction * maxAbduction;

            if (proximal != null)
                proximal.localRotation = _proximalRest * Quaternion.Euler(ppDeg, abdDeg, 0f);
            if (middle != null)
                middle.localRotation = _middleRest * Quaternion.Euler(mpDeg, 0f, 0f);
            if (distal != null)
                distal.localRotation = _distalRest * Quaternion.Euler(dpDeg, 0f, 0f);
        }
    }
}
