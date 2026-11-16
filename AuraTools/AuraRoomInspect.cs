using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Inspeccion para el paso 6: que hay en InitialTestRoom (para copiarla sin la parte de oscuridad)
/// y como esta montado HWSpringBonnieIMPROVED (mallas, materiales y huesos). Solo lee.
/// </summary>
public static class AuraRoomInspect
{
    private const string FbxPath = "Assets/GameStuff/Models/DriveRigged/SpringBonnieVR/HWSpringBonnieIMPROVED.fbx";

    public static string Execute()
    {
        var log = new StringBuilder();

        // ---- 1. Escena InitialTestRoom (la que esta abierta)
        var escena = SceneManager.GetActiveScene();
        log.AppendLine("[AuraRoom] escena activa=" + escena.path);

        var raices = escena.GetRootGameObjects();
        for (int i = 0; i < raices.Length; i++)
        {
            VolcarObjeto(raices[i].transform, 0, log, 2);
        }

        // ---- 2. Modelo de Springbonnie
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (fbx == null)
        {
            log.AppendLine("[AuraRoom] NO EXISTE " + FbxPath);
            Debug.Log(log.ToString());
            return;
        }

        log.AppendLine("[AuraRoom] ---- " + FbxPath + " ----");

        var smrs = fbx.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        log.AppendLine("[AuraRoom] SkinnedMeshRenderers=" + smrs.Length);
        for (int i = 0; i < smrs.Length; i++)
        {
            var mat = smrs[i].sharedMaterial;
            var mats = smrs[i].sharedMaterials;
            var nombres = new StringBuilder();
            for (int k = 0; k < mats.Length; k++)
            {
                nombres.Append(mats[k] != null ? mats[k].name : "null").Append(' ');
            }

            log.AppendLine("[AuraRoom] SMR " + smrs[i].name
                + " | activo=" + smrs[i].gameObject.activeSelf
                + " | verts=" + (smrs[i].sharedMesh != null ? smrs[i].sharedMesh.vertexCount : 0)
                + " | mats=" + nombres
                + " | shader=" + (mat != null ? mat.shader.name : "-"));
        }

        var mfs = fbx.GetComponentsInChildren<MeshRenderer>(true);
        log.AppendLine("[AuraRoom] MeshRenderers=" + mfs.Length);
        for (int i = 0; i < mfs.Length; i++)
        {
            log.AppendLine("[AuraRoom] MR " + mfs[i].name + " | mats=" + (mfs[i].sharedMaterial != null ? mfs[i].sharedMaterial.name : "null"));
        }

        var huesos = fbx.GetComponentsInChildren<Transform>(true);
        log.AppendLine("[AuraRoom] huesos totales=" + huesos.Length);
        VolcarHuesos(fbx.transform, 0, log);

        var animator = fbx.GetComponentInChildren<Animator>(true);
        log.AppendLine("[AuraRoom] Animator=" + (animator != null) + " avatar=" + (animator != null && animator.avatar != null));

        Debug.Log(log.ToString());
        return log.ToString();
    }

    private static void VolcarObjeto(Transform t, int nivel, StringBuilder log, int profundidadMax)
    {
        if (nivel > profundidadMax)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.Append(' ', nivel * 2).Append(t.name);

        var renderers = t.GetComponents<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            var m = renderers[i].sharedMaterial;
            sb.Append(" [mat=").Append(m != null ? m.name : "-").Append(']');
        }

        if (t.GetComponent<Light>() != null) sb.Append(" [LUZ]");
        if (!t.gameObject.activeSelf) sb.Append(" (inactivo)");
        if (t.childCount > 0) sb.Append(" hijos=").Append(t.childCount);
        log.AppendLine(sb.ToString());

        for (int i = 0; i < t.childCount; i++)
        {
            VolcarObjeto(t.GetChild(i), nivel + 1, log, profundidadMax);
        }
    }

    private static void VolcarHuesos(Transform hueso, int nivel, StringBuilder log)
    {
        var sb = new StringBuilder();
        sb.Append("  ", nivel % 20).Append(hueso.name);
        log.AppendLine(sb.ToString());

        for (int i = 0; i < hueso.childCount; i++)
        {
            VolcarHuesos(hueso.GetChild(i), nivel + 1, log);
        }
    }
}
