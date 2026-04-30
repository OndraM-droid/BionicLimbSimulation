using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using BionicLimb.Hand;
using BionicLimb.Core;
using BionicLimb.UI;

/// <summary>
/// One-shot editor helper: wires the procedural hand scene.
/// Run via Tools > BionicLimb > Setup Scene.
/// </summary>
public static class SceneSetupHelper
{
    [MenuItem("Tools/BionicLimb/Setup Scene")]
    public static void SetupScene()
    {
        // ── Task 4: HandRig ─────────────────────────────────────────────────
        var handRoot = GameObject.Find("Hand_Root");
        if (handRoot == null) { Debug.LogError("[Setup] Hand_Root not found"); return; }

        var rig = handRoot.GetComponent<HandRig>() ?? handRoot.AddComponent<HandRig>();

        string[] boneNames = {
            "Thumb_PP", "Thumb_DP",
            "Index_PP", "Index_MP", "Index_DP",
            "Middle_PP", "Middle_MP", "Middle_DP",
            "Ring_PP", "Ring_MP", "Ring_DP",
            "Pinky_PP", "Pinky_MP", "Pinky_DP"
        };
        rig.fingerBones = new Transform[14];
        foreach (var t in handRoot.GetComponentsInChildren<Transform>())
            for (int i = 0; i < 14; i++)
                if (t.name == boneNames[i]) { rig.fingerBones[i] = t; break; }

        int nullBones = 0;
        for (int i = 0; i < 14; i++) if (rig.fingerBones[i] == null) nullBones++;
        Debug.Log($"[Setup] HandRig wired. Null bones: {nullBones}/14");

        // ── Task 5: GesturePoseLibrary ──────────────────────────────────────
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Poses");

        var library = ScriptableObject.CreateInstance<GesturePoseLibrary>();
        library.poses = new GesturePose[8];

        // rest (0): all identity
        library.poses[0] = MakePose("Pose_Rest", new[] {
            // Thumb_PP, Thumb_DP
            Q(0,0,0), Q(0,0,0),
            // Index_PP, MP, DP
            Q(0,0,0), Q(0,0,0), Q(0,0,0),
            // Middle_PP, MP, DP
            Q(0,0,0), Q(0,0,0), Q(0,0,0),
            // Ring_PP, MP, DP
            Q(0,0,0), Q(0,0,0), Q(0,0,0),
            // Pinky_PP, MP, DP
            Q(0,0,0), Q(0,0,0), Q(0,0,0)
        });

        // fist (1)
        library.poses[1] = MakePose("Pose_Fist", new[] {
            Q(60,0,0), Q(80,0,0),
            Q(90,0,0), Q(100,0,0), Q(90,0,0),
            Q(90,0,0), Q(100,0,0), Q(90,0,0),
            Q(90,0,0), Q(100,0,0), Q(90,0,0),
            Q(90,0,0), Q(100,0,0), Q(90,0,0)
        });

        // open_hand (2): all identity
        library.poses[2] = MakePose("Pose_OpenHand", new[] {
            Q(0,0,0), Q(0,0,0),
            Q(0,0,0), Q(0,0,0), Q(0,0,0),
            Q(0,0,0), Q(0,0,0), Q(0,0,0),
            Q(0,0,0), Q(0,0,0), Q(0,0,0),
            Q(0,0,0), Q(0,0,0), Q(0,0,0)
        });

        // pinch (3): index+thumb curl, others open
        library.poses[3] = MakePose("Pose_Pinch", new[] {
            Q(70,0,0), Q(70,0,0),
            Q(80,0,0), Q(80,0,0), Q(80,0,0),
            Q(0,0,0),  Q(0,0,0),  Q(0,0,0),
            Q(0,0,0),  Q(0,0,0),  Q(0,0,0),
            Q(0,0,0),  Q(0,0,0),  Q(0,0,0)
        });

        // point (4): index+thumb open, rest curled
        library.poses[4] = MakePose("Pose_Point", new[] {
            Q(30,30,0), Q(30,0,0),
            Q(0,0,0),   Q(0,0,0),   Q(0,0,0),
            Q(80,0,0),  Q(90,0,0),  Q(80,0,0),
            Q(80,0,0),  Q(90,0,0),  Q(80,0,0),
            Q(80,0,0),  Q(90,0,0),  Q(80,0,0)
        });

        // thumbs_up (5): fingers curled, thumb extended
        library.poses[5] = MakePose("Pose_ThumbsUp", new[] {
            Q(0,0,0),  Q(0,0,0),
            Q(90,0,0), Q(100,0,0), Q(90,0,0),
            Q(90,0,0), Q(100,0,0), Q(90,0,0),
            Q(90,0,0), Q(100,0,0), Q(90,0,0),
            Q(90,0,0), Q(100,0,0), Q(90,0,0)
        });

        // peace (6): ring+pinky curled, index+middle extended
        library.poses[6] = MakePose("Pose_Peace", new[] {
            Q(30,30,0), Q(30,0,0),
            Q(0,0,0),   Q(0,0,0),  Q(0,0,0),
            Q(0,0,0),   Q(0,0,0),  Q(0,0,0),
            Q(80,0,0),  Q(90,0,0), Q(80,0,0),
            Q(80,0,0),  Q(90,0,0), Q(80,0,0)
        });

        // ok (7): index+thumb circle, others extended
        library.poses[7] = MakePose("Pose_Ok", new[] {
            Q(70,0,0),  Q(70,0,0),
            Q(80,0,0),  Q(80,0,0), Q(80,0,0),
            Q(0,0,0),   Q(0,0,0),  Q(0,0,0),
            Q(0,0,0),   Q(0,0,0),  Q(0,0,0),
            Q(10,0,0),  Q(0,0,0),  Q(0,0,0)
        });

        var libPath = "Assets/Resources/Poses/GesturePoseLibrary.asset";
        AssetDatabase.DeleteAsset(libPath);
        AssetDatabase.CreateAsset(library, libPath);
        AssetDatabase.SaveAssets();
        var savedLib = AssetDatabase.LoadAssetAtPath<GesturePoseLibrary>(libPath);
        Debug.Log($"[Setup] GesturePoseLibrary created at {libPath} with {savedLib?.poses?.Length} poses");

        // ── Task 7: Controllers GO ──────────────────────────────────────────
        var controllers = GameObject.Find("Controllers");
        if (controllers == null) controllers = new GameObject("Controllers");
        var playback = controllers.GetComponent<PlaybackController>()
                    ?? controllers.AddComponent<PlaybackController>();

        // ── Task 7: UIDocument GO ───────────────────────────────────────────
        var uiDocGO = GameObject.Find("UIDocument");
        if (uiDocGO == null) uiDocGO = new GameObject("UIDocument");
        var uiDoc = uiDocGO.GetComponent<UIDocument>() ?? uiDocGO.AddComponent<UIDocument>();

        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PlaybackPanel.uxml");
        if (uxml != null) uiDoc.visualTreeAsset = uxml;
        else Debug.LogWarning("[Setup] PlaybackPanel.uxml not found at Assets/UI/");

        var playbackUI = uiDocGO.GetComponent<PlaybackUI>() ?? uiDocGO.AddComponent<PlaybackUI>();
        playbackUI.playbackController = playback;

        // ── Task 8: GesturePoser ────────────────────────────────────────────
        var poser = handRoot.GetComponent<GesturePoser>() ?? handRoot.AddComponent<GesturePoser>();
        poser.handRig     = rig;
        poser.poseLibrary = savedLib;
        playback.gesturePoser = poser;

        // ── Mark scene dirty ────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Setup] Scene setup complete!");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts = path.Split('/');
            var parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var child = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(child))
                    AssetDatabase.CreateFolder(parent, parts[i]);
                parent = child;
            }
        }
    }

    private static GesturePose MakePose(string name, Quaternion[] rots)
    {
        var pose = ScriptableObject.CreateInstance<GesturePose>();
        pose.targetRotations = new Quaternion[14];
        for (int i = 0; i < Mathf.Min(rots.Length, 14); i++)
            pose.targetRotations[i] = rots[i];
        var path = $"Assets/Resources/Poses/{name}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(pose, path);
        return AssetDatabase.LoadAssetAtPath<GesturePose>(path);
    }
}
