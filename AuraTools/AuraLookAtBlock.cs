using UnityEditor;
using UnityEngine;

// Acerca la vista de escena al bloque para poder inspeccionar el sombreado de cerca.
public static class AuraLookAtBlock
{
    public static string Execute()
    {
        GameObject bloque = GameObject.Find("RoomObjects/GrabbableObject");
        if (bloque == null) return "no block";

        Vector3 p = bloque.transform.position;
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return "no scene view";

        // Camara a 0.22 m del cubo, mirandolo, con encuadre pequeno.
        Vector3 camPos = p + new Vector3(0.16f, 0.10f, -0.16f);
        sv.LookAt(p, Quaternion.LookRotation(p - camPos), 0.22f);
        sv.Repaint();

        return $"scene view -> {camPos} size 0.22";
    }
}
