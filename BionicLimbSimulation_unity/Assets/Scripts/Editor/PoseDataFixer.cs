using UnityEngine;
using UnityEditor;
using BionicLimb.Hand;

/// <summary>
/// Loads every GesturePose asset in Assets/Resources/Poses/ and writes
/// anatomically correct targetRotations[14] + cmcRotation data.
///
/// Bone order in targetRotations[] (spec §5.3):
///   [0] Thumb_PP  [1] Thumb_DP
///   [2] Index_PP  [3] Index_MP   [4] Index_DP
///   [5] Middle_PP [6] Middle_MP  [7] Middle_DP
///   [8] Ring_PP   [9] Ring_MP   [10] Ring_DP
///  [11] Pinky_PP [12] Pinky_MP  [13] Pinky_DP
///
/// Euler convention: X = flexion (positive = flex), Y = abduction (positive = radial), Z = 0.
///
/// Run via Tools > BionicLimb > Fix Pose Data.
/// </summary>
public static class PoseDataFixer
{
    private const string PosesFolder = "Assets/Resources/Poses/";

    [MenuItem("Tools/BionicLimb/Fix Pose Data")]
    public static void FixPoseData()
    {
        Apply("Pose_Rest.asset",     SetRest);
        Apply("Pose_Fist.asset",     SetFist);
        Apply("Pose_OpenHand.asset", SetOpenHand);
        Apply("Pose_Pinch.asset",    SetPinch);
        Apply("Pose_Point.asset",    SetPoint);
        Apply("Pose_ThumbsUp.asset", SetThumbsUp);
        Apply("Pose_Peace.asset",    SetPeace);
        Apply("Pose_Ok.asset",       SetOk);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PoseDataFixer] All 8 GesturePose assets updated and saved.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Apply(string fileName, System.Action<GesturePose> configure)
    {
        string path = PosesFolder + fileName;
        var asset = AssetDatabase.LoadAssetAtPath<GesturePose>(path);
        if (asset == null)
        {
            Debug.LogError($"[PoseDataFixer] Asset not found: {path}");
            return;
        }

        // Ensure array is the right size (defensive)
        if (asset.targetRotations == null || asset.targetRotations.Length != GesturePose.BoneCount)
            asset.targetRotations = new Quaternion[GesturePose.BoneCount];

        configure(asset);
        EditorUtility.SetDirty(asset);
        Debug.Log($"[PoseDataFixer] Updated: {path}");
    }

    private static Quaternion Q(float flexion, float abduction = 0f)
        => Quaternion.Euler(flexion, abduction, 0f);

    // ── Pose implementations ─────────────────────────────────────────────────

    private static void SetRest(GesturePose p)
    {
        for (int i = 0; i < GesturePose.BoneCount; i++)
            p.targetRotations[i] = Quaternion.identity;
        p.cmcRotation = Quaternion.identity;
    }

    private static void SetFist(GesturePose p)
    {
        // Thumb
        p.targetRotations[0]  = Q(45);
        p.targetRotations[1]  = Q(60);
        // Index
        p.targetRotations[2]  = Q(85);
        p.targetRotations[3]  = Q(90);
        p.targetRotations[4]  = Q(70);
        // Middle
        p.targetRotations[5]  = Q(85);
        p.targetRotations[6]  = Q(77);
        p.targetRotations[7]  = Q(60);
        // Ring
        p.targetRotations[8]  = Q(85);
        p.targetRotations[9]  = Q(77);
        p.targetRotations[10] = Q(60);
        // Pinky
        p.targetRotations[11] = Q(85);
        p.targetRotations[12] = Q(77);
        p.targetRotations[13] = Q(60);
        // CMC1: slight thumb opposition, wraps over fingers
        p.cmcRotation = Q(30, 15);
    }

    private static void SetOpenHand(GesturePose p)
    {
        // Thumb — extended
        p.targetRotations[0]  = Quaternion.identity;
        p.targetRotations[1]  = Quaternion.identity;
        // Index — extended, spread radial
        p.targetRotations[2]  = Q(0, 15);
        p.targetRotations[3]  = Quaternion.identity;
        p.targetRotations[4]  = Quaternion.identity;
        // Middle — slight radial spread
        p.targetRotations[5]  = Q(0, 5);
        p.targetRotations[6]  = Quaternion.identity;
        p.targetRotations[7]  = Quaternion.identity;
        // Ring — slight ulnar spread
        p.targetRotations[8]  = Q(0, -5);
        p.targetRotations[9]  = Quaternion.identity;
        p.targetRotations[10] = Quaternion.identity;
        // Pinky — spread ulnar
        p.targetRotations[11] = Q(0, -15);
        p.targetRotations[12] = Quaternion.identity;
        p.targetRotations[13] = Quaternion.identity;
        // CMC1: thumb fully abducted wide open
        p.cmcRotation = Q(0, 35);
    }

    private static void SetPinch(GesturePose p)
    {
        // Thumb
        p.targetRotations[0]  = Q(40);
        p.targetRotations[1]  = Q(55);
        // Index — curls toward thumb
        p.targetRotations[2]  = Q(50, -5);
        p.targetRotations[3]  = Q(55);
        p.targetRotations[4]  = Q(50);
        // Middle
        p.targetRotations[5]  = Q(85);
        p.targetRotations[6]  = Q(77);
        p.targetRotations[7]  = Q(60);
        // Ring
        p.targetRotations[8]  = Q(85);
        p.targetRotations[9]  = Q(77);
        p.targetRotations[10] = Q(60);
        // Pinky
        p.targetRotations[11] = Q(85);
        p.targetRotations[12] = Q(77);
        p.targetRotations[13] = Q(60);
        // CMC1: full opposition (max flex+abduction)
        p.cmcRotation = Q(45, 35);
    }

    private static void SetPoint(GesturePose p)
    {
        // Thumb — partial curl
        p.targetRotations[0]  = Q(20);
        p.targetRotations[1]  = Q(30);
        // Index — fully extended
        p.targetRotations[2]  = Quaternion.identity;
        p.targetRotations[3]  = Quaternion.identity;
        p.targetRotations[4]  = Quaternion.identity;
        // Middle
        p.targetRotations[5]  = Q(85);
        p.targetRotations[6]  = Q(77);
        p.targetRotations[7]  = Q(60);
        // Ring
        p.targetRotations[8]  = Q(85);
        p.targetRotations[9]  = Q(77);
        p.targetRotations[10] = Q(60);
        // Pinky
        p.targetRotations[11] = Q(85);
        p.targetRotations[12] = Q(77);
        p.targetRotations[13] = Q(60);
        // CMC1: partial opposition
        p.cmcRotation = Q(20, 20);
    }

    private static void SetThumbsUp(GesturePose p)
    {
        // Thumb — fully extended
        p.targetRotations[0]  = Quaternion.identity;
        p.targetRotations[1]  = Quaternion.identity;
        // Index
        p.targetRotations[2]  = Q(85);
        p.targetRotations[3]  = Q(77);
        p.targetRotations[4]  = Q(60);
        // Middle
        p.targetRotations[5]  = Q(85);
        p.targetRotations[6]  = Q(77);
        p.targetRotations[7]  = Q(60);
        // Ring
        p.targetRotations[8]  = Q(85);
        p.targetRotations[9]  = Q(77);
        p.targetRotations[10] = Q(60);
        // Pinky
        p.targetRotations[11] = Q(85);
        p.targetRotations[12] = Q(77);
        p.targetRotations[13] = Q(60);
        // CMC1: max abduction — thumb pointing up
        p.cmcRotation = Q(0, 40);
    }

    private static void SetPeace(GesturePose p)
    {
        // Thumb — slight curl
        p.targetRotations[0]  = Q(20);
        p.targetRotations[1]  = Q(25);
        // Index — extended, spread radial
        p.targetRotations[2]  = Q(0, 10);
        p.targetRotations[3]  = Quaternion.identity;
        p.targetRotations[4]  = Quaternion.identity;
        // Middle — extended, spread ulnar
        p.targetRotations[5]  = Q(0, -10);
        p.targetRotations[6]  = Quaternion.identity;
        p.targetRotations[7]  = Quaternion.identity;
        // Ring
        p.targetRotations[8]  = Q(85);
        p.targetRotations[9]  = Q(77);
        p.targetRotations[10] = Q(60);
        // Pinky
        p.targetRotations[11] = Q(85);
        p.targetRotations[12] = Q(77);
        p.targetRotations[13] = Q(60);
        // CMC1: partial opposition
        p.cmcRotation = Q(20, 25);
    }

    private static void SetOk(GesturePose p)
    {
        // Thumb
        p.targetRotations[0]  = Q(40);
        p.targetRotations[1]  = Q(50);
        // Index — curls to form circle with thumb
        p.targetRotations[2]  = Q(50, -5);
        p.targetRotations[3]  = Q(55);
        p.targetRotations[4]  = Q(45);
        // Middle — extended
        p.targetRotations[5]  = Quaternion.identity;
        p.targetRotations[6]  = Quaternion.identity;
        p.targetRotations[7]  = Quaternion.identity;
        // Ring — extended
        p.targetRotations[8]  = Quaternion.identity;
        p.targetRotations[9]  = Quaternion.identity;
        p.targetRotations[10] = Quaternion.identity;
        // Pinky — extended
        p.targetRotations[11] = Quaternion.identity;
        p.targetRotations[12] = Quaternion.identity;
        p.targetRotations[13] = Quaternion.identity;
        // CMC1: full opposition
        p.cmcRotation = Q(45, 35);
    }
}
