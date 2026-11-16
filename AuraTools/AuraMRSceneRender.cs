using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renders de comprobacion de la escena MR: frontal, lateral y tres cuartos.
/// Oculta el rig del jugador para que no tape la figura.
/// </summary>
public static class AuraMRSceneRender
{
    public static string Execute()
    {
        var log = new StringBuilder();
        var escena = EditorSceneManager.GetActiveScene();
        log.AppendLine("[R] escena=" + escena.path);

        var raices = escena.GetRootGameObjects();
        var ocultos = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == "MRThings" || raices[i].name == "Managers")
            {
                if (raices[i].activeSelf)
                {
                    raices[i].SetActive(false);
                    ocultos.Add(raices[i]);
                }
            }
        }

        Render("AuraTools/mr_front.png", new Vector3(0f, 1.35f, 2.3f), new Vector3(0f, 0.80f, -0.10f));
        Render("AuraTools/mr_side.png", new Vector3(2.3f, 1.20f, -0.10f), new Vector3(0f, 0.80f, -0.10f));
        Render("AuraTools/mr_three_quarter.png", new Vector3(1.6f, 1.60f, 1.7f), new Vector3(0f, 0.70f, -0.30f));

        for (int i = 0; i < ocultos.Count; i++)
        {
            ocultos[i].SetActive(true);
        }

        log.AppendLine("[R] renders guardados en AuraTools/mr_front.png, mr_side.png, mr_three_quarter.png");
        return log.ToString();
    }

    private static void Render(string ruta, Vector3 posicion, Vector3 objetivo)
    {
        const int lado = 640;
        var goCam = new GameObject("__AuraCamRender");
        var cam = goCam.AddComponent<Camera>();
        cam.orthographic = false;
        cam.fieldOfView = 50f;
        cam.transform.position = posicion;
        cam.transform.LookAt(objetivo);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f);
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
