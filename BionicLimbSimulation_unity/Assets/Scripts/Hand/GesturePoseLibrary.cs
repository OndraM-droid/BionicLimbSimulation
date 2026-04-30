using UnityEngine;

namespace BionicLimb.Hand
{
    /// <summary>
    /// ScriptableObject library storing one GesturePose per gesture class.
    /// Spec §8
    /// Gesture order must match classifier GestureIds:
    ///   0=rest, 1=fist, 2=open_hand, 3=pinch, 4=point, 5=thumbs_up, 6=peace, 7=ok
    /// </summary>
    [CreateAssetMenu(menuName = "BionicLimb/GesturePoseLibrary", fileName = "GesturePoseLibrary")]
    public class GesturePoseLibrary : ScriptableObject
    {
        public static readonly string[] GestureNames =
            { "rest", "fist", "open_hand", "pinch", "point", "thumbs_up", "peace", "ok" };

        [Tooltip("8 poses in order: rest, fist, open_hand, pinch, point, thumbs_up, peace, ok")]
        public GesturePose[] poses = new GesturePose[8];

        /// <summary>Returns the pose for a given gesture ID, or null if not found.</summary>
        public GesturePose GetPose(int gestureId)
        {
            if (gestureId < 0 || gestureId >= poses.Length) return null;
            return poses[gestureId];
        }
    }
}
