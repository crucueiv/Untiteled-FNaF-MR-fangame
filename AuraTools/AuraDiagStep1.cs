using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Diagnostico de la Tarea 1: backup + inspeccion de InitialTestRoom + luz de prueba.
public static class AuraDiagStep1
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // ---------- 1) Copia de seguridad ----------
        string backup = "Assets/GameStuff/_Backup/2026-11-13";
        Directory.CreateDirectory(backup + "/Shaders");
        Directory.CreateDirectory(backup + "/Materials");
        Directory.CreateDirectory(backup + "/Scenes");

        Copiar("Assets/GameStuff/Shaders/MR_DirectLightOnly.shader", backup + "/Shaders/MR_DirectLightOnly.shader", sb);
        Copiar("Assets/GameStuff/Materials/BlockMaterial_DirectLight.mat", backup + "/Materials/BlockMaterial_DirectLight.mat", sb);
        Copiar("Assets/GameStuff/TestScenes/InitialTestRoom.unity", backup + "/Scenes/InitialTestRoom.unity", sb);
        Copiar("Assets/GameStuff/TestScenes/TestMenu.unity", backup + "/Scenes/TestMenu.unity", sb);
        AssetDatabase.Refresh();

        // ---------- 2) Abrir la escena de pruebas ----------
        Scene activa = EditorSceneManager.GetActiveScene();
        if (activa.isDirty)
        {
            EditorSceneManager.SaveScene(activa);
            sb.AppendLine("[Aura] Escena activa guardada antes de cambiar: " + activa.path);
        }

        Scene escena = EditorSceneManager.OpenScene("Assets/GameStuff/TestScenes/InitialTestRoom.unity", OpenSceneMode.Single);
        sb.AppendLine("[Aura] Escena abierta: " + escena.path);

        // ---------- 3) Luces presentes ----------
        var luces = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        sb.AppendLine("[Aura] Luces en la escena: " + luces.Length);
        foreach (var l in luces)
        {
            sb.AppendLine($"   - {l.name}: {l.type} int={l.intensity} rango={l.range} sombras={l.shadows}");
        }

        // ---------- 4) El bloque grabbable ----------
        GameObject bloque = GameObject.Find("RoomObjects/GrabbableObject");
        if (bloque == null)
        {
            var todos = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var mr in todos)
            {
                if (mr.sharedMaterial != null && mr.sharedMaterial.name.Contains("BlockMaterial_DirectLight"))
                {
                    bloque = mr.gameObject;
                    break;
                }
            }
        }

        if (bloque == null)
        {
            sb.AppendLine("[Aura][ERROR] No se encontro el bloque con BlockMaterial_DirectLight.");
            Debug.Log(sb.ToString());
            return sb.ToString();
        }

        sb.AppendLine("[Aura] Bloque: " + bloque.name + " worldPos=" + bloque.transform.position +
                      " lossyScale=" + bloque.transform.lossyScale);

        var mr2 = bloque.GetComponent<MeshRenderer>();
        if (mr2 != null)
        {
            sb.AppendLine($"   renderer: receiveShadows={mr2.receiveShadows} castShadows={mr2.shadowCastingMode} " +
                          $"lightProbeUsage={mr2.lightProbeUsage} reflectionProbe={mr2.reflectionProbeUsage}");
            var mat = mr2.sharedMaterial;
            sb.AppendLine($"   material: {mat.name} shader={mat.shader.name} queue={mat.renderQueue}");
            sb.AppendLine($"   keywords: [{string.Join(",", mat.shaderKeywords)}]");
            sb.AppendLine($"   hasShadowCasterPass: {mat.shader.name.StartsWith("Custom/")}  shaderPassCount={mat.passCount}");
        }

        var malla = bloque.GetComponent<MeshFilter>();
        if (malla != null && malla.sharedMesh != null)
        {
            sb.AppendLine($"   mesh: {malla.sharedMesh.name} verts={malla.sharedMesh.vertexCount} bounds={malla.sharedMesh.bounds.size}");
        }

        // ---------- 5) Luz de prueba (solo en el editor, no se guarda la escena) ----------
        GameObject anterior = GameObject.Find("__AuraTestSpot");
        if (anterior != null) Object.DestroyImmediate(anterior);

        var lgo = new GameObject("__AuraTestSpot");
        var spot = lgo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.spotAngle = 45f;
        spot.innerSpotAngle = 20f;
        spot.range = 6f;
        spot.intensity = 6f;
        spot.color = Color.white;
        spot.shadows = LightShadows.Soft;
        Vector3 objetivo = bloque.transform.position;
        lgo.transform.position = objetivo + new Vector3(0.25f, 0.45f, -1.0f);
        lgo.transform.LookAt(objetivo);
        sb.AppendLine("[Aura] Luz de prueba creada: " + lgo.transform.position + " -> " + objetivo);

        Debug.Log(sb.ToString());
        return sb.ToString();
    }

    private static void Copiar(string origen, string destino, StringBuilder sb)
    {
        if (!File.Exists(origen))
        {
            sb.AppendLine("[Aura][WARN] No existe para backup: " + origen);
            return;
        }
        File.Copy(origen, destino, true);
        sb.AppendLine("[Aura] Backup: " + destino);
    }
}
