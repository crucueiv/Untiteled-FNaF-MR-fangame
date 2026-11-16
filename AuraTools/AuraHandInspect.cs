using System.Text;
using UnityEditor;
using UnityEngine;

// Vuelca la jerarquia del FBX de la mano derecha: huesos, posiciones locales y renderers.
public static class AuraHandInspect
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        string ruta = "Assets/GameStuff/Models/Player Hands/RIG_Right_PlayerHands_10k.fbx";
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (asset == null) return "[Aura] no se encuentra el FBX";

        sb.AppendLine("[Aura] raiz asset: " + asset.name);

        foreach (var t in asset.GetComponentsInChildren<Transform>(true))
        {
            int prof = 0;
            Transform p = t.parent;
            while (p != null) { prof++; p = p.parent; }
            sb.Append(' ', prof * 2);
            sb.Append(t.name);
            sb.Append(" | lp=").Append(t.localPosition.ToString("F4"));
            sb.Append(" | ls=").Append(t.localScale.ToString("F4"));
            var smr = t.GetComponent<SkinnedMeshRenderer>();
            if (smr != null) sb.Append(" | SMR verts=").Append(smr.sharedMesh != null ? smr.sharedMesh.vertexCount : 0);
            var mr = t.GetComponent<MeshRenderer>();
            if (mr != null) sb.Append(" | MR");
            if (t.GetComponent<Animator>() != null) sb.Append(" | Animator");
            sb.AppendLine();
        }

        foreach (var smr in asset.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            sb.AppendLine("[Aura] SMR '" + smr.name + "' bounds=" + smr.localBounds.size +
                          " mat=" + (smr.sharedMaterial != null ? smr.sharedMaterial.name + "/" + smr.sharedMaterial.shader.name : "null") +
                          " huesos=" + (smr.bones != null ? smr.bones.Length : 0));
        }

        Debug.Log(sb.ToString());
        return sb.ToString();
    }
}
