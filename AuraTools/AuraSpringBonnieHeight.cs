using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Paso 6: altura real del modelo (malla horneada) y escala de los prefabs de personajes existentes.
/// </summary>
public static class AuraSpringBonnieHeight
{
    private const string FbxPath = "Assets/GameStuff/Models/DriveRigged/SpringBonnieVR/HWSpringBonnieIMPROVED.fbx";

    public static string Execute()
    {
        var log = new StringBuilder();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        var temp = (GameObject)UnityEngine.Object.Instantiate(prefab);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        var smrs = temp.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < smrs.Length; i++)
        {
            var malla = new Mesh();
            smrs[i].BakeMesh(malla);
            var verts = malla.vertices;
            for (int v = 0; v < verts.Length; v++)
            {
                var p = smrs[i].transform.TransformPoint(verts[v]);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            log.AppendLine("[H] " + smrs[i].name + " verts=" + verts.Length);
            UnityEngine.Object.DestroyImmediate(malla);
        }

        log.AppendLine("[H] malla total min=" + min.ToString("0.###") + " max=" + max.ToString("0.###")
            + " alto=" + (max.y - min.y).ToString("0.###") + " ancho=" + (max.x - min.x).ToString("0.###"));

        UnityEngine.Object.DestroyImmediate(temp);

        // Convencion de escala de los personajes ya montados
        var rutas = new[]
        {
            "Assets/GameStuff/Prefabs/DriveCharacters/Bonnie.prefab",
            "Assets/GameStuff/Prefabs/DriveCharacters/Freddy.prefab"
        };
        for (int i = 0; i < rutas.Length; i++)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(rutas[i]);
            if (p == null)
            {
                log.AppendLine("[H] falta " + rutas[i]);
                continue;
            }

            log.AppendLine("[H] " + p.name + " raiz escala=" + p.transform.localScale.ToString("0.####"));
            for (int c = 0; c < p.transform.childCount; c++)
            {
                var hijo = p.transform.GetChild(c);
                log.AppendLine("[H] " + p.name + "/" + hijo.name + " escala=" + hijo.localScale.ToString("0.####")
                    + " pos=" + hijo.localPosition.ToString("0.###"));
            }
        }

        return log.ToString();
    }
}
