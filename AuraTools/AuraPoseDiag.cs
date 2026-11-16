using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Diagnostico numerico de la pose de Springbonnie en MR_SpringBonnieRoom:
/// imprime posiciones y direcciones de los huesos clave y saca tres vistas
/// ortograficas aislando solo la figura, el proxy de cama y las luces.
/// </summary>
public static class AuraPoseDiag
{
    public static string Execute()
    {
        var log = new StringBuilder();
        var escena = EditorSceneManager.GetActiveScene();
        var raices = escena.GetRootGameObjects();

        // ---- Localizar figura y cama en cualquier punto de la jerarquia.
        Transform figura = BuscarGlobal("Springbonnie_MR");
        Transform cama = BuscarGlobal("BedProxy_MR");
        if (figura == null)
        {
            return "[D] no encuentro Springbonnie_MR";
        }

        log.AppendLine("[D] figura pos=" + V(figura.position) + " euler=" + V(figura.eulerAngles)
            + " escala=" + V(figura.lossyScale));

        if (cama != null)
        {
            var mr = cama.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Bounds b = mr.bounds;
                log.AppendLine("[D] cama centro=" + V(b.center) + " tamano=" + V(b.size)
                    + " techoY=" + b.max.y.ToString("F3") + " bordeZ=" + b.max.z.ToString("F3"));
            }
        }

        // ---- Huesos clave
        string[] huesos = new string[]
        {
            "bip_pelvis", "bip_spine_0", "bip_neck", "bip_head",
            "bip_hip_r", "bip_knee_r", "bip_foot_r", "bip_foot_r_end",
            "bip_hip_l", "bip_knee_l", "bip_foot_l",
            "bip_upperArm_r", "bip_lowerArm_r", "bip_hand_r",
            "bip_upperArm_l", "bip_lowerArm_l", "bip_hand_l"
        };

        for (int i = 0; i < huesos.Length; i++)
        {
            Transform t = Buscar(figura, huesos[i]);
            log.AppendLine("[D] " + huesos[i] + " = " + (t == null ? "NO ENCONTRADO" : V(t.position)));
        }

        // ---- Direcciones derivadas
        Dir(log, "muslo_r", figura, "bip_hip_r", "bip_knee_r");
        Dir(log, "espinilla_r", figura, "bip_knee_r", "bip_foot_r");
        Dir(log, "pie_r", figura, "bip_foot_r", "bip_foot_r_end");
        Dir(log, "columna", figura, "bip_spine_0", "bip_neck");
        Dir(log, "cuello", figura, "bip_neck", "bip_head");
        Dir(log, "brazo_r", figura, "bip_upperArm_r", "bip_lowerArm_r");
        Dir(log, "antebrazo_r", figura, "bip_lowerArm_r", "bip_hand_r");
        Dir(log, "brazo_l", figura, "bip_upperArm_l", "bip_lowerArm_l");
        Dir(log, "antebrazo_l", figura, "bip_lowerArm_l", "bip_hand_l");
        Palma(log, "palma_r", figura, "bip_hand_r", "bip_index_0_r", "bip_thumb_0_r");
        Palma(log, "palma_l", figura, "bip_hand_l", "bip_index_0_l", "bip_thumb_0_l");

        float escala = figura.lossyScale.y;
        log.AppendLine("[D] escala=" + escala.ToString("F4"));

        // ---- Aislar y renderizar
        var apagados = GuardarApagados();
        Aislar(figura, cama);

        var luces = Object.FindObjectsOfType<Light>();
        var estadoLuces = new bool[luces.Length];
        for (int i = 0; i < luces.Length; i++)
        {
            estadoLuces[i] = luces[i].enabled;
            luces[i].enabled = true;
        }

        Render("AuraTools/pose_front.png", new Vector3(0f, 0.75f, 3.2f), new Vector3(0f, 0.75f, 0f), 1.45f);
        Render("AuraTools/pose_side.png", new Vector3(3.2f, 0.75f, 0f), new Vector3(0f, 0.75f, 0f), 1.45f);
        Render("AuraTools/pose_three.png", new Vector3(2.4f, 2.0f, 2.4f), new Vector3(0f, 0.70f, 0f), 1.55f);

        for (int i = 0; i < luces.Length; i++)
        {
            if (luces[i] != null)
            {
                luces[i].enabled = estadoLuces[i];
            }
        }

        Restaurar(raices, apagados);
        log.AppendLine("[D] renders en AuraTools/pose_front.png, pose_side.png, pose_three.png");
        return log.ToString();
    }

    private static void Dir(StringBuilder log, string etiqueta, Transform raiz, string a, string b)
    {
        Transform ta = Buscar(raiz, a);
        Transform tb = Buscar(raiz, b);
        if (ta == null || tb == null)
        {
            log.AppendLine("[D] dir " + etiqueta + " = falta hueso");
            return;
        }

        Vector3 d = (tb.position - ta.position).normalized;
        log.AppendLine("[D] dir " + etiqueta + " = " + V(d)
            + "  giroY=" + (Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg).ToString("F1")
            + "  bajoHorizonte=" + (Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg).ToString("F1"));
    }

    private static void Palma(StringBuilder log, string etiqueta, Transform raiz, string mano,
        string indice, string pulgar)
    {
        Transform tm = Buscar(raiz, mano);
        Transform ti = Buscar(raiz, indice);
        Transform tp = Buscar(raiz, pulgar);
        if (tm == null || ti == null || tp == null)
        {
            log.AppendLine("[D] " + etiqueta + " = falta hueso");
            return;
        }

        Vector3 d = (ti.position - tm.position).normalized;
        Vector3 p = (tp.position - tm.position).normalized;
        Vector3 n = Vector3.Cross(d, p).normalized;
        log.AppendLine("[D] " + etiqueta + " dedos=" + V(d) + " normal=" + V(n)
            + " anguloNormalArriba=" + Vector3.Angle(n, Vector3.up).ToString("F1"));
    }

    private static List<GameObject> GuardarApagados()
    {
        var apagados = new List<GameObject>();
        var todos = Object.FindObjectsOfType<GameObject>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            if (!todos[i].activeSelf)
            {
                apagados.Add(todos[i]);
            }
        }

        return apagados;
    }

    private static void Restaurar(GameObject[] raices, List<GameObject> apagados)
    {
        for (int i = 0; i < raices.Length; i++)
        {
            raices[i].SetActive(true);
        }

        for (int i = 0; i < apagados.Count; i++)
        {
            if (apagados[i] != null)
            {
                apagados[i].SetActive(false);
            }
        }
    }

    /// <summary>Apaga todo salvo la figura, el proxy de cama y las luces.</summary>
    private static void Aislar(Transform figura, Transform cama)
    {
        var raicesFigura = new List<Transform>();
        Transform p = figura;
        while (p.parent != null)
        {
            p = p.parent;
        }

        raicesFigura.Add(p);

        var raices = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < raices.Length; i++)
        {
            GameObject r = raices[i];
            bool esFigura = r.transform == p;
            bool esCama = cama != null && (r.transform == cama || cama.IsChildOf(r.transform));
            bool esLuz = r.GetComponent<Light>() != null;
            r.SetActive(esFigura || esCama || esLuz);
        }
    }

    private static Transform BuscarGlobal(string nombre)
    {
        var todas = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < todas.Length; i++)
        {
            if (todas[i].name == nombre)
            {
                return todas[i];
            }
        }

        return null;
    }

    private static Transform Buscar(Transform raiz, string nombre)
    {
        var todos = raiz.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == nombre)
            {
                return todos[i];
            }
        }

        return null;
    }

    private static string V(Vector3 v)
    {
        return "(" + v.x.ToString("F3") + ", " + v.y.ToString("F3") + ", " + v.z.ToString("F3") + ")";
    }

    private static void Render(string ruta, Vector3 posicion, Vector3 objetivo, float tamanoOrtho)
    {
        const int lado = 640;
        var goCam = new GameObject("__AuraCamPose");
        var cam = goCam.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = tamanoOrtho;
        cam.transform.position = posicion;
        cam.transform.LookAt(objetivo);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.06f, 1f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 40f;

        var rt = new RenderTexture(lado, lado, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var previo = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0f, 0f, lado, lado), 0, 0);
        tex.Apply();
        RenderTexture.active = previo;
        File.WriteAllBytes(ruta, tex.EncodeToPNG());

        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(goCam);
    }
}
