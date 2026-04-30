using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies anatomically correct localPosition, localScale, and localRotation to all
/// hand bone GameObjects. Uses the stacking formula:
///   child.localPosition.Y = parent.scale.y + child.scale.y
/// so capsule segments are flush (no gaps, no overlaps).
///
/// Run via Tools > BionicLimb > Fix Hand Geometry.
/// </summary>
public static class HandGeometryFixer
{
    [MenuItem("Tools/BionicLimb/Fix Hand Geometry")]
    public static void FixHandGeometry()
    {
        var handRoot = GameObject.Find("Hand_Root");
        if (handRoot == null)
        {
            Debug.LogError("[HandGeometryFixer] Hand_Root not found in scene.");
            return;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HandMaterial.mat");
        if (mat == null)
            Debug.LogWarning("[HandGeometryFixer] HandMaterial not found at Assets/Materials/HandMaterial.mat");

        // ── Metacarpals (local to Hand_Root) ──────────────────────────────────
        // Position = capsule center. Scale = (radius, half-height, radius).
        SetBone(handRoot.transform, "Thumb_MC",
            new Vector3(-0.030f, 0.005f, 0f),
            new Vector3(0.020f,  0.030f, 0.020f),
            Quaternion.Euler(0f, 0f, 40f));  // 40 deg tilt toward radial side

        SetBone(handRoot.transform, "Index_MC",
            new Vector3(-0.022f, 0.035f, 0f),
            new Vector3(0.016f,  0.035f, 0.016f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Middle_MC",
            new Vector3(-0.008f, 0.036f, 0f),
            new Vector3(0.016f,  0.036f, 0.016f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Ring_MC",
            new Vector3(0.008f, 0.034f, 0f),
            new Vector3(0.016f, 0.034f, 0.016f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Pinky_MC",
            new Vector3(0.022f, 0.0275f, 0f),
            new Vector3(0.015f, 0.0275f, 0.015f),
            Quaternion.identity);

        // ── Thumb phalanges ───────────────────────────────────────────────────
        SetBone(handRoot.transform, "Thumb_PP",
            new Vector3(0f, 0.049f, 0f),    // MC.sy(0.030) + self.sy(0.019)
            new Vector3(0.018f, 0.019f, 0.018f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Thumb_DP",
            new Vector3(0f, 0.0315f, 0f),   // PP.sy(0.019) + self.sy(0.0125)
            new Vector3(0.015f, 0.0125f, 0.015f),
            Quaternion.identity);

        // ── Index phalanges ───────────────────────────────────────────────────
        SetBone(handRoot.transform, "Index_PP",
            new Vector3(0f, 0.055f, 0f),    // MC.sy(0.035) + self.sy(0.020)
            new Vector3(0.014f, 0.020f, 0.014f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Index_MP",
            new Vector3(0f, 0.0325f, 0f),   // PP.sy(0.020) + self.sy(0.0125)
            new Vector3(0.013f, 0.0125f, 0.013f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Index_DP",
            new Vector3(0f, 0.0225f, 0f),   // MP.sy(0.0125) + self.sy(0.010)
            new Vector3(0.011f, 0.010f, 0.011f),
            Quaternion.identity);

        // ── Middle phalanges ──────────────────────────────────────────────────
        SetBone(handRoot.transform, "Middle_PP",
            new Vector3(0f, 0.0585f, 0f),   // MC.sy(0.036) + self.sy(0.0225)
            new Vector3(0.014f, 0.0225f, 0.014f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Middle_MP",
            new Vector3(0f, 0.0365f, 0f),   // PP.sy(0.0225) + self.sy(0.014)
            new Vector3(0.013f, 0.014f, 0.013f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Middle_DP",
            new Vector3(0f, 0.024f, 0f),    // MP.sy(0.014) + self.sy(0.010)
            new Vector3(0.011f, 0.010f, 0.011f),
            Quaternion.identity);

        // ── Ring phalanges ────────────────────────────────────────────────────
        SetBone(handRoot.transform, "Ring_PP",
            new Vector3(0f, 0.054f, 0f),    // MC.sy(0.034) + self.sy(0.020)
            new Vector3(0.014f, 0.020f, 0.014f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Ring_MP",
            new Vector3(0f, 0.033f, 0f),    // PP.sy(0.020) + self.sy(0.013)
            new Vector3(0.013f, 0.013f, 0.013f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Ring_DP",
            new Vector3(0f, 0.0225f, 0f),   // MP.sy(0.013) + self.sy(0.0095)
            new Vector3(0.011f, 0.0095f, 0.011f),
            Quaternion.identity);

        // ── Pinky phalanges ───────────────────────────────────────────────────
        SetBone(handRoot.transform, "Pinky_PP",
            new Vector3(0f, 0.0435f, 0f),   // MC.sy(0.0275) + self.sy(0.016)
            new Vector3(0.013f, 0.016f, 0.013f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Pinky_MP",
            new Vector3(0f, 0.025f, 0f),    // PP.sy(0.016) + self.sy(0.009)
            new Vector3(0.012f, 0.009f, 0.012f),
            Quaternion.identity);

        SetBone(handRoot.transform, "Pinky_DP",
            new Vector3(0f, 0.0165f, 0f),   // MP.sy(0.009) + self.sy(0.0075)
            new Vector3(0.010f, 0.0075f, 0.010f),
            Quaternion.identity);

        // ── Palm (flat box covering the palm pad) ─────────────────────────────
        SetupPalm(handRoot, mat);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[HandGeometryFixer] Done — 17 bones + Palm updated with anatomically correct proportions.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetBone(Transform root, string boneName,
        Vector3 localPos, Vector3 localScale, Quaternion localRot)
    {
        Transform t = FindDeep(root, boneName);
        if (t == null)
        {
            Debug.LogWarning($"[HandGeometryFixer] Bone not found: {boneName}");
            return;
        }

        Undo.RecordObject(t, $"HandGeometryFixer: {boneName}");
        t.localPosition = localPos;
        t.localScale    = localScale;
        t.localRotation = localRot;
    }

    /// <summary>Depth-first search for a named transform in a subtree.</summary>
    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void SetupPalm(GameObject handRoot, Material mat)
    {
        Transform palmT = FindDeep(handRoot.transform, "Palm");
        GameObject palm;

        if (palmT == null)
        {
            // Create a Cube, remove its default BoxCollider, parent to Hand_Root
            palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.name = "Palm";
            Undo.RegisterCreatedObjectUndo(palm, "HandGeometryFixer: Create Palm");
            palm.transform.SetParent(handRoot.transform, false);

            var col = palm.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
        else
        {
            palm = palmT.gameObject;
            Undo.RecordObject(palm.transform, "HandGeometryFixer: Palm Transform");

            // Remove any existing collider
            var col = palm.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        palm.transform.localPosition = Vector3.zero;
        palm.transform.localScale    = new Vector3(0.050f, 0.012f, 0.022f);
        palm.transform.localRotation = Quaternion.identity;

        // Match layer to parent
        palm.layer = handRoot.layer;

        // Apply HandMaterial
        if (mat != null)
        {
            var rend = palm.GetComponent<Renderer>();
            if (rend != null)
            {
                Undo.RecordObject(rend, "HandGeometryFixer: Palm Material");
                rend.sharedMaterial = mat;
            }
        }
    }
}
