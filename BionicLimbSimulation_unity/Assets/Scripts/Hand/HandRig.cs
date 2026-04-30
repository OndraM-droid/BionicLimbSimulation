using UnityEngine;

namespace BionicLimb.Hand
{
    /// <summary>
    /// Applies gesture poses to the procedural hand rig via Quaternion.Slerp.
    /// All rotations are clamped to anatomical ROM before assignment.
    /// Spec §5.3, §2.2
    ///
    /// fingerBones[] bone order (14 phalanges):
    ///   0  Thumb_PP   1  Thumb_DP
    ///   2  Index_PP   3  Index_MP   4  Index_DP
    ///   5  Middle_PP  6  Middle_MP  7  Middle_DP
    ///   8  Ring_PP    9  Ring_MP   10  Ring_DP
    ///  11  Pinky_PP  12  Pinky_MP  13  Pinky_DP
    ///
    /// thumbMetacarpal is animated separately (CMC1 saddle joint, 2 DOF).
    /// </summary>
    public class HandRig : MonoBehaviour
    {
        [Header("Bone References (14 phalanges)")]
        [Tooltip("Ordered: Thumb_PP, Thumb_DP, Index_PP/MP/DP, Middle_PP/MP/DP, Ring_PP/MP/DP, Pinky_PP/MP/DP")]
        public Transform[] fingerBones = new Transform[GesturePose.BoneCount];

        [Header("Thumb Metacarpal (CMC1 saddle joint)")]
        [Tooltip("Thumb_MC: 2-axis saddle joint. X = flexion 0-50 deg, Y = abduction 0-40 deg. Spec §2.2")]
        public Transform thumbMetacarpal;

        [Header("Animation")]
        [Tooltip("Slerp speed for pose blending (frame-rate independent).")]
        public float poseBlendSpeed = 8f;

        // ── Flexion ROM (X axis) ──────────────────────────────────────────────
        // Spec §2.2: MCP1=0-60, IP1=0-80, MCP2-5=0-90, PIP=0-100, DIP=0-90.
        private static readonly float[] FlexionMin =
        {
            0f, 0f,             // Thumb_PP (MCP1), Thumb_DP (IP1)
            0f, 0f, 0f,         // Index  PP(MCP), MP(PIP), DP(DIP)
            0f, 0f, 0f,         // Middle
            0f, 0f, 0f,         // Ring
            0f, 0f, 0f          // Pinky
        };

        private static readonly float[] FlexionMax =
        {
            60f, 80f,
            90f, 100f, 90f,
            90f, 100f, 90f,
            90f, 100f, 90f,
            90f, 100f, 90f
        };

        // ── Abduction ROM (Y axis) ────────────────────────────────────────────
        // Spec §2.2: MCP2-5 condyloid joints have ±20° abduction.
        // PP bones correspond to the MCP joint; MP and DP are hinges (Y = 0).
        // Thumb PP (MCP1) is a hinge — no abduction here (CMC1 handles it).
        private static readonly float[] AbductionMin =
        {
            0f,   0f,           // Thumb_PP, Thumb_DP
            -20f, 0f, 0f,       // Index  PP, MP, DP
            -20f, 0f, 0f,       // Middle
            -20f, 0f, 0f,       // Ring
            -20f, 0f, 0f        // Pinky
        };

        private static readonly float[] AbductionMax =
        {
            0f,  0f,
            20f, 0f, 0f,
            20f, 0f, 0f,
            20f, 0f, 0f,
            20f, 0f, 0f
        };

        // ── CMC1 (Thumb_MC) limits ────────────────────────────────────────────
        // Spec §2.2: CMC1 saddle joint — flex 0-50°, abduction 0-40°.
        private const float CmcFlexMin = 0f,  CmcFlexMax = 50f;
        private const float CmcAbdMin  = 0f,  CmcAbdMax  = 40f;

        private Quaternion[] _currentRotations;
        private Quaternion   _cmcCurrentRotation = Quaternion.identity;

        private void Awake()
        {
            _currentRotations = new Quaternion[GesturePose.BoneCount];
            for (int i = 0; i < GesturePose.BoneCount; i++)
                _currentRotations[i] = fingerBones[i] != null
                    ? fingerBones[i].localRotation
                    : Quaternion.identity;

            _cmcCurrentRotation = thumbMetacarpal != null
                ? thumbMetacarpal.localRotation
                : Quaternion.identity;
        }

        /// <summary>
        /// Smoothly blend toward <paramref name="pose"/>.
        /// Call every frame from GesturePoser.Update.
        /// blendWeight: 0 = hold current, 1 = full target.
        /// </summary>
        public void ApplyGesturePose(GesturePose pose, float blendWeight = 1f)
        {
            if (pose == null || pose.targetRotations == null) return;
            float t = Mathf.Clamp01(Time.deltaTime * poseBlendSpeed * blendWeight);

            for (int i = 0; i < GesturePose.BoneCount; i++)
            {
                if (fingerBones[i] == null) continue;
                Quaternion target = ClampPhalanx(pose.targetRotations[i], i);
                _currentRotations[i] = Quaternion.Slerp(_currentRotations[i], target, t);
                fingerBones[i].localRotation = _currentRotations[i];
            }

            // Animate CMC1 (Thumb_MC) — saddle joint, 2 DOF.
            if (thumbMetacarpal != null)
            {
                Quaternion cmcTarget = ClampCMC(pose.cmcRotation);
                _cmcCurrentRotation = Quaternion.Slerp(_cmcCurrentRotation, cmcTarget, t);
                thumbMetacarpal.localRotation = _cmcCurrentRotation;
            }
        }

        /// <summary>Clamp phalanx rotation to anatomical ROM (flexion X + abduction Y).</summary>
        private static Quaternion ClampPhalanx(Quaternion q, int boneIndex)
        {
            Vector3 e = q.eulerAngles;
            float x = e.x > 180f ? e.x - 360f : e.x;
            float y = e.y > 180f ? e.y - 360f : e.y;
            x = Mathf.Clamp(x, FlexionMin[boneIndex], FlexionMax[boneIndex]);
            y = Mathf.Clamp(y, AbductionMin[boneIndex], AbductionMax[boneIndex]);
            return Quaternion.Euler(x, y, e.z);
        }

        /// <summary>Clamp CMC1 rotation to saddle-joint ROM (flex X + abduction Y).</summary>
        private static Quaternion ClampCMC(Quaternion q)
        {
            Vector3 e = q.eulerAngles;
            float x = e.x > 180f ? e.x - 360f : e.x;
            float y = e.y > 180f ? e.y - 360f : e.y;
            x = Mathf.Clamp(x, CmcFlexMin, CmcFlexMax);
            y = Mathf.Clamp(y, CmcAbdMin,  CmcAbdMax);
            return Quaternion.Euler(x, y, e.z);
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
            if (thumbMetacarpal != null)
            {
                thumbMetacarpal.localRotation = Quaternion.identity;
                _cmcCurrentRotation = Quaternion.identity;
            }
        }
    }
}
