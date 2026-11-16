using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Paso 6: mide la altura real del modelo renderizando su silueta con una camara ortografica
/// (asi la escala 1:1 se calcula con pixeles, no con bounds dudosos). No modifica assets.
/// </summary>
public static class AuraSpringBonnieSilhouette
{
    private const string FbxPath = "Assets/GameStuff/Models/DriveRigged/SpringBonnieVR/HWSpringBonnieIMPROVED.fbx";
    private const int Lado = 512;
    private const float TamanoOrto = 2.5f;

    public static string Execute()
    {
        var log = new StringBuilder();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null)
        {
            return "[I] NO EXISTE " + FbxPath;
        }

        var temp = (GameObject)UnityEngine.Object.Instantiate(prefab);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        // Material blanco unlit para la silueta (temporal, en memoria)
        var shader = Shader.Find("Unlit/Color");
        var matBlanco = new Material(shader);
        matBlanco.SetColor("_Color", Color.white);
        var renderers = temp.GetComponentsInChildren<Renderer>(true);
        var origMats = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            origMats[i] = renderers[i].sharedMaterial;
            renderers[i].sharedMaterial = matBlanco;
        }

        var goCam = new GameObject("__AuraCamSB");
        var cam = goCam.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = TamanoOrto;
        cam.transform.position = new Vector3(0f, 2f, -12f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 40f;
        cam.allowHDR = false;

        var rt = new RenderTexture(Lado, Lado, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var previo = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, Lado, Lado), 0, 0);
        tex.Apply();
        RenderTexture.active = previo;

        int minX = Lado, maxX = -1, minY = Lado, maxY = -1;
        var pixeles = tex.GetPixels();
        for (int y = 0; y < Lado; y++)
        {
            for (int x = 0; x < Lado; x++)
            {
                var c = pixeles[y * Lado + x];
                if (c.r > 0.35f)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        float metrosPorPixel = (TamanoOrto * 2f) / Lado;
        if (maxY >= 0)
        {
            float altoM = (maxY - minY + 1) * metrosPorPixel;
            float anchoM = (maxX - minX + 1) * metrosPorPixel;
            // y del mundo del borde inferior de la silueta (camara en y=2, orto 2.5)
            float yInferior = 2f - TamanoOrto + minY * metrosPorPixel;
            float ySuperior = 2f - TamanoOrto + (maxY + 1) * metrosPorPixel;
            log.AppendLine("[I] pixeles x=[" + minX + "," + maxX + "] y=[" + minY + "," + maxY + "]");
            log.AppendLine("[I] alto(m)=" + altoM.ToString("0.###") + " ancho(m)=" + anchoM.ToString("0.###"));
            log.AppendLine("[I] y inferior=" + yInferior.ToString("0.###") + " y superior=" + ySuperior.ToString("0.###"));
            log.AppendLine("[I] metros por pixel=" + metrosPorPixel.ToString("0.#####"));

            // Imagen de control
            var png = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes("AuraTools/sb_silueta.png", png);
            log.AppendLine("[I] guardado AuraTools/sb_silueta.png");
        }
        else
        {
            log.AppendLine("[I] la silueta salio vacia (revisar camara/culling)");
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial = origMats[i];
        }

        cam.targetTexture = null;
        rt.Release();
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(goCam);
        UnityEngine.Object.DestroyImmediate(matBlanco);
        UnityEngine.Object.DestroyImmediate(temp);
        log.AppendLine("[I] temporales destruidos");
        return log.ToString();
    }
}
