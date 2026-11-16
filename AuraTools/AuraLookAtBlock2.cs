using UnityEditor;
using UnityEngine;

// Encuadra el cubo en diagonal para ver a la vez la cara iluminada y la cara a 90 grados.
public static class AuraLookAtBlock2
{
    public static string Execute()
    {
        GameObject bloque = GameObject.Find("RoomObjects/GrabbableObject");
        if (bloque == null) return "no block";

        Vector3 p = bloque.transform.position;
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return "no scene view";

        Vector3 camPos = p + new Vector3(0.10f, 0.06f, -0.10f);
        sv.LookAt(p, Quaternion.LookRotation(p - camPos), 0.22f);
        sv.Repaint();
        return $"scene view -> {camPos}";
    }
}
