using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bisect 2: solo el modelo HWSpringBonnieIMPROVED.
/// </summary>
public static class AuraRoomInspectB
{
    private const string FbxPath = "Assets/GameStuff/Models/DriveRigged/SpringBonnieVR/HWSpringBonnieIMPROVED.fbx";

    public static string Execute()
    {
        var log = new StringBuilder();
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (fbx == null)
        {
            return "[B] NO EXISTE " + FbxPath;
        }

        var smrs = fbx.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        log.AppendLine("[B] SkinnedMeshRenderers=" + smrs.Length);
        for (int i = 0; i < smrs.Length; i++)
        {
            var mats = smrs[i].sharedMaterials;
            var nombres = new StringBuilder();
            for (int k = 0; k < mats.Length; k++)
            {
                nombres.Append(mats[k] != null ? mats[k].name : "null").Append(' ');
            }

            log.AppendLine("[B] SMR " + smrs[i].name
                + " | activo=" + smrs[i].gameObject.activeSelf
                + " | verts=" + (smrs[i].sharedMesh != null ? smrs[i].sharedMesh.vertexCount : 0)
                + " | mats=" + nombres);
        }

        var mfs = fbx.GetComponentsInChildren<MeshRenderer>(true);
        log.AppendLine("[B] MeshRenderers=" + mfs.Length);
        for (int i = 0; i < mfs.Length; i++)
        {
            var mat = mfs[i].sharedMaterial;
            log.AppendLine("[B] MR " + mfs[i].name + " | mat=" + (mat != null ? mat.name : "null"));
        }

        var huesos = fbx.GetComponentsInChildren<Transform>(true);
        log.AppendLine("[B] transforms=" + huesos.Length);

        var animator = fbx.GetComponentInChildren<Animator>(true);
        log.AppendLine("[B] Animator=" + (animator != null) + " avatar=" + (animator != null && animator.avatar != null));

        return log.ToString();
    }
}
