using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BionicLimb.Hand;
using BionicLimb.Core;

/// <summary>
/// Fixes hand bone transforms, materials, and component wiring for visual audit.
/// Run via Tools > BionicLimb > Fix Hand (Audit).
/// </summary>
public static class HandAuditFixer
{
    [MenuItem("Tools/BionicLimb/Fix Hand (Audit)")]
    public static void FixAll()
    {
        FixTransformsAndMaterials();
        SceneSetupHelper.SetupScene();
    }

    [MenuItem("Tools/BionicLimb/Fix Hand Transforms And Materials")]
    public static void FixTransformsAndMaterials()
    {
        var handRoot = GameObject.Find("Hand_Root");
        if (handRoot == null) { Debug.LogError("[HandAuditFixer] Hand_Root not found"); return; }

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HandMaterial.mat");
        if (mat == null)
            Debug.LogWarning("[HandAuditFixer] HandMaterial not found at Assets/Materials/HandMaterial.mat");

        // ── Metacarpals (relative to Hand_Root) ──────────────────────────────
        SetBone(handRoot.transform, "Thumb_MC",
            new Vector3(-0.04f, 0f, 0f),
            new Vector3(0.018f, 0.04f, 0.018f),
            new Vector3(0f, 0f, 30f), mat);

        SetBone(handRoot.transform, "Index_MC",
            new Vector3(-0.02f, 0.05f, 0f),
            new Vector3(0.015f, 0.045f, 0.015f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Middle_MC",
            new Vector3(0f, 0.055f, 0f),
            new Vector3(0.015f, 0.048f, 0.015f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Ring_MC",
            new Vector3(0.02f, 0.05f, 0f),
            new Vector3(0.014f, 0.044f, 0.014f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Pinky_MC",
            new Vector3(0.04f, 0.045f, 0f),
            new Vector3(0.012f, 0.038f, 0.012f),
            Vector3.zero, mat);

        // ── Thumb phalanges ───────────────────────────────────────────────────
        SetBone(handRoot.transform, "Thumb_PP",
            new Vector3(0f, 0.05f, 0f),
            new Vector3(0.015f, 0.035f, 0.015f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Thumb_DP",
            new Vector3(0f, 0.045f, 0f),
            new Vector3(0.013f, 0.028f, 0.013f),
            Vector3.zero, mat);

        // ── Index phalanges ───────────────────────────────────────────────────
        SetBone(handRoot.transform, "Index_PP",
            new Vector3(0f, 0.055f, 0f),
            new Vector3(0.013f, 0.04f, 0.013f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Index_MP",
            new Vector3(0f, 0.05f, 0f),
            new Vector3(0.012f, 0.03f, 0.012f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Index_DP",
            new Vector3(0f, 0.04f, 0f),
            new Vector3(0.011f, 0.025f, 0.011f),
            Vector3.zero, mat);

        // ── Middle phalanges ──────────────────────────────────────────────────
        SetBone(handRoot.transform, "Middle_PP",
            new Vector3(0f, 0.058f, 0f),
            new Vector3(0.013f, 0.042f, 0.013f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Middle_MP",
            new Vector3(0f, 0.052f, 0f),
            new Vector3(0.012f, 0.032f, 0.012f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Middle_DP",
            new Vector3(0f, 0.042f, 0f),
            new Vector3(0.011f, 0.026f, 0.011f),
            Vector3.zero, mat);

        // ── Ring phalanges ────────────────────────────────────────────────────
        SetBone(handRoot.transform, "Ring_PP",
            new Vector3(0f, 0.054f, 0f),
            new Vector3(0.012f, 0.038f, 0.012f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Ring_MP",
            new Vector3(0f, 0.048f, 0f),
            new Vector3(0.011f, 0.029f, 0.011f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Ring_DP",
            new Vector3(0f, 0.038f, 0f),
            new Vector3(0.010f, 0.023f, 0.010f),
            Vector3.zero, mat);

        // ── Pinky phalanges ───────────────────────────────────────────────────
        SetBone(handRoot.transform, "Pinky_PP",
            new Vector3(0f, 0.048f, 0f),
            new Vector3(0.011f, 0.034f, 0.011f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Pinky_MP",
            new Vector3(0f, 0.042f, 0f),
            new Vector3(0.010f, 0.026f, 0.010f),
            Vector3.zero, mat);

        SetBone(handRoot.transform, "Pinky_DP",
            new Vector3(0f, 0.034f, 0f),
            new Vector3(0.009f, 0.021f, 0.009f),
            Vector3.zero, mat);

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[HandAuditFixer] Bone transforms and materials applied.");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    static void SetBone(Transform root, string boneName,
        Vector3 localPos, Vector3 localScale, Vector3 eulerAngles, Material mat)
    {
        Transform t = FindDeep(root, boneName);
        if (t == null)
        {
            Debug.LogWarning("[HandAuditFixer] Bone not found: " + boneName);
            return;
        }

        t.localPosition    = localPos;
        t.localScale       = localScale;
        t.localEulerAngles = eulerAngles;

        if (mat != null)
        {
            var mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }
    }

    static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
