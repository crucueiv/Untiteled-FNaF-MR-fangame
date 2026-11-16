using System.Text;
using UnityEditor;
using UnityEngine;

// Comprueba el contorno en la instancia de escena y encuadra el cubo en diagonal.
// Tambien borra la luz temporal de diagnostico para no dejarla en la escena.
public static class AuraOutlineCheck
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // 1. Limpieza de la luz temporal de diagnostico.
        GameObject luzTemp = GameObject.Find("__AuraTestSpot");
        if (luzTemp != null)
        {
            Object.DestroyImmediate(luzTemp);
            sb.AppendLine("[Aura] __AuraTestSpot eliminada.");
        }
        else
        {
            sb.AppendLine("[Aura] __AuraTestSpot ya no estaba.");
        }

        // 2. Contorno sobre la instancia de escena.
        GameObject bloque = GameObject.Find("RoomObjects/GrabbableObject");
        if (bloque == null) return sb.AppendLine("[Aura] no hay bloque").ToString();

        var h = bloque.GetComponent<InteractableHighlight>();
        if (h == null) h = InteractableHighlight.Attach(bloque);
        h.Show();

        int cascaras = 0;
        Transform cascara = null;
        foreach (Transform t in bloque.transform)
        {
            if (t.name == "__HighlightOutline") { cascaras++; cascara = t; }
        }
        sb.AppendLine($"[Aura] componente=True cascaras={cascaras} visible={h.IsVisible}");

        if (cascara != null)
        {
            var r = cascara.GetComponent<MeshRenderer>();
            sb.AppendLine($"[Aura] cascara active={cascara.gameObject.activeSelf} mat={(r.sharedMaterial != null ? r.sharedMaterial.name : "null")} shader={(r.sharedMaterial != null && r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "null")}");
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            sb.AppendLine($"[Aura] mpb color={mpb.GetVector("_OutlineColor")} width={mpb.GetFloat("_OutlineWidth")} pulso={mpb.GetFloat("_PulseEnabled")}");

            var filtro = cascara.GetComponent<MeshFilter>();
            sb.AppendLine($"[Aura] mesh={(filtro.sharedMesh != null ? filtro.sharedMesh.name : "null")} bounds={r.bounds.size}");
        }

        // 3. Camara del SceneView en diagonal cerca del cubo.
        Vector3 p = bloque.transform.position;
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            Vector3 camPos = p + new Vector3(0.09f, 0.05f, -0.09f);
            sv.LookAt(p, Quaternion.LookRotation(p - camPos), 0.14f);
            sv.Repaint();
            sb.AppendLine($"[Aura] sceneview -> {camPos} size=0.14");
        }

        Debug.Log(sb.ToString());
        return sb.ToString();
    }
}
