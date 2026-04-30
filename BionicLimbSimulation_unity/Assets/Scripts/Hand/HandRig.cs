using UnityEngine;

namespace BionicLimb.Hand
{
    /// <summary>
    /// Applies gesture poses to the procedural hand rig via Quaternion.Slerp.
    /// All rotations are clamped to anatomical ROM before assignment.
    /// Spec §5.3
    ///
    /// Bone order in fingerBones[] (14 entries):
    ///   0  Thumb_PP   1  Thumb_DP
    ///   2  Index_PP   3  Index_MP   4  Index_DP
    ///   5  Middle_PP  6  Middle_MP  7  Middle_DP
    ///   8  Ring_PP    9  Ring_MP   10  Ring_DP
    ///  11  Pinky_PP  12  Pinky_MP  13  Pinky_DP
    /// </summary>
    public class HandRig : MonoBehaviour
    {
        [Header("Bone References (14 phalanges)")]
        [Tooltip("Ordered: Thumb_PP, Thumb_DP, Index_PP/MP/DP, Middle_PP/MP/DP, Ring_PP/MP/DP, Pinky_PP/MP/DP")]
        public Transform[] fingerBones = new Transform[GesturePose.BoneCount];

        [Header("Animation")]
        [Tooltip("Slerp speed for pose blending (frames independent).")]
        public float poseBlendSpeed = 8f;

        // Anatomical ROM limits per bone index (flexion X axis), degrees.
        // Spec §2.2
        private static readonly float[] FlexionMin = new float[GesturePose.BoneCount]
        {
            // Thumb_PP (MCP1), Thumb_DP (IP1)
            0f, 0f,
            // Index PP (MCP2-5), MP (PIP), DP (DIP)
            0f, 0f, 0f,
            // Middle
            0f, 0f, 0f,
            // Ring
            0f, 0f, 0f,
            // Pinky
            0f, 0f, 0f
        };

        private static readonly float[] FlexionMax = new float[GesturePose.BoneCount]
        {
            60f, 80f,       // Thumb MCP1, IP1
            90f, 100f, 90f, // Index MCP2-5, PIP, DIP
            90f, 100f, 90f,
            90f, 100f, 90f,
            90f, 100f, 90f
        };

        private Quaternion[] _currentRotations;

        private void Awake()
        {
            _currentRotations = new Quaternion[GesturePose.BoneCount];
            for (int i = 0; i < GesturePose.BoneCount; i++)
            {
                _currentRotations[i] = fingerBones[i] != null
                    ? fingerBones[i].localRotation
                    : Quaternion.identity;
            }
        }

        /// <summary>
        /// Smoothly blend toward the given GesturePose.
        /// Call every frame (from GesturePoser.Update).
        /// blendWeight: 0=stay current, 1=full target.
        /// </summary>
        public void ApplyGesturePose(GesturePose pose, float blendWeight = 1f)
        {
            if (pose == null || pose.targetRotations == null) return;
            float t = Mathf.Clamp01(Time.deltaTime * poseBlendSpeed * blendWeight);

            for (int i = 0; i < GesturePose.BoneCount; i++)
            {
                if (fingerBones[i] == null) continue;

                Quaternion target = ClampToROM(pose.targetRotations[i], i);
                _currentRotations[i] = Quaternion.Slerp(_currentRotations[i], target, t);
                fingerBones[i].localRotation = _currentRotations[i];
            }
        }

        /// <summary>Clamp localRotation X-axis (flexion) to anatomical ROM.</summary>
        private static Quaternion ClampToROM(Quaternion q, int boneIndex)
        {
            Vector3 euler = q.eulerAngles;
            // Convert Unity's 0-360 range to -180..180 for clamping
            float x = euler.x > 180f ? euler.x - 360f : euler.x;
            x = Mathf.Clamp(x, FlexionMin[boneIndex], FlexionMax[boneIndex]);
            return Quaternion.Euler(x, euler.y, euler.z);
        }

        /// <summary>Immediately snap all bones to rest (identity) without blending.</summary>
        public void ResetToRestPose()
        {
            for (int i = 0; i < GesturePose.BoneCount; i++)
            {
                if (fingerBones[i] == null) continue;
                fingerBones[i].localRotation = Quaternion.identity;
                _currentRotations[i] = Quaternion.identity;
            }
        }
    }
}
