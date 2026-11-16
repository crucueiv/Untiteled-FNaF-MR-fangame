using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Render de cerca del torso de Springbonnie para comprobar que manos y antebrazos
/// no atraviesan el muslo ni el pecho. Deja dos PNG en AuraTools/.
/// </summary>
public static class AuraPoseClose
{
    public static string Execute()
    {
        Transform figura = BuscarGlobal("Springbonnie_MR");
        if (figura == null)
        {
            return "[C] no encuentro Springbonnie_MR";
        }

        var raices = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        ApagarTodoSalvoFigura(raices, figura);

        // Una luz frontal suave para que se lean los volumenes.
        var goLuz = new GameObject("__AuraLuzPose");
        var luz = goLuz.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 1.4f;
        goLuz.transform.rotation = Quaternion.Euler(35f, 200f, 0f);

        Render("AuraTools/pose_close_front.png", new Vector3(0.25f, 0.95f, 1.35f), new Vector3(0f, 0.85f, 0.05f), 0.75f);
        Render("AuraTools/pose_close_side.png", new Vector3(1.35f, 0.95f, 0.15f), new Vector3(0f, 0.85f, 0.05f), 0.75f);

        Object.DestroyImmediate(goLuz);
        Restaurar(raices);
        return "[C] renders en AuraTools/pose_close_front.png y pose_close_side.png";
    }

    private static void ApagarTodoSalvoFigura(GameObject[] raices, Transform figura)
    {
        Transform raizFigura = figura;
        while (raizFigura.parent != null)
        {
            raizFigura = raizFigura.parent;
        }

        for (int i = 0; i < raices.Length; i++)
        {
            raices[i].SetActive(raices[i].transform == raizFigura);
        }
    }

    private static void Restaurar(GameObject[] raices)
    {
        for (int i = 0; i < raices.Length; i++)
        {
            raices[i].SetActive(true);
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

    private static void Render(string ruta, Vector3 posicion, Vector3 objetivo, float tamanoOrtho)
    {
        const int lado = 640;
        var goCam = new GameObject("__AuraCamClose");
        var cam = goCam.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = tamanoOrtho;
        cam.transform.position = posicion;
        cam.transform.LookAt(objetivo);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
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
