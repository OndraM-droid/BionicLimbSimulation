using UnityEngine;

namespace BionicLimb.Hand
{
    /// <summary>
    /// ScriptableObject holding 14 target Quaternion rotations for one gesture pose.
    /// Bone order: Thumb_PP, Thumb_DP,
    ///             Index_PP, Index_MP, Index_DP,
    ///             Middle_PP, Middle_MP, Middle_DP,
    ///             Ring_PP, Ring_MP, Ring_DP,
    ///             Pinky_PP, Pinky_MP, Pinky_DP
    /// Spec §5.3, §8
    /// </summary>
    [CreateAssetMenu(menuName = "BionicLimb/GesturePose", fileName = "NewGesturePose")]
    public class GesturePose : ScriptableObject
    {
        public const int BoneCount = 14;

        [Tooltip("14 local Quaternion rotations — one per phalanx, see BoneIndex enum.")]
        public Quaternion[] targetRotations = new Quaternion[BoneCount];

        private void Reset()
        {
            targetRotations = new Quaternion[BoneCount];
            for (int i = 0; i < BoneCount; i++)
                targetRotations[i] = Quaternion.identity;
        }
    }

    /// <summary>Index constants for GesturePose.targetRotations array.</summary>
    public static class BoneIndex
    {
        public const int Thumb_PP   = 0;
        public const int Thumb_DP   = 1;
        public const int Index_PP   = 2;
        public const int Index_MP   = 3;
        public const int Index_DP   = 4;
        public const int Middle_PP  = 5;
        public const int Middle_MP  = 6;
        public const int Middle_DP  = 7;
        public const int Ring_PP    = 8;
        public const int Ring_MP    = 9;
        public const int Ring_DP    = 10;
        public const int Pinky_PP   = 11;
        public const int Pinky_MP   = 12;
        public const int Pinky_DP   = 13;
    }
}
