using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso 6 (bisect D): inventario de InitialTestRoom para saber que quitar de la copia MR.
/// Lista la jerarquia con componentes y busca referencias a la oscuridad.
/// </summary>
public static class AuraRoomInspectD
{
    public static string Execute()
    {
        var log = new StringBuilder();
        var escena = SceneManager.GetActiveScene();
        log.AppendLine("[D] escena=" + escena.path);

        var raices = escena.GetRootGameObjects();
        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == "Managers" || raices[i].name == "RoomObjects")
            {
                Volcar(raices[i].transform, 0, log, 5);
            }
        }

        log.AppendLine("[D] ---- referencias a la oscuridad ----");
        var componentes = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < componentes.Length; i++)
        {
            var mono = componentes[i];
            if (mono == null)
            {
                continue;
            }

            var so = new SerializedObject(mono);
            var it = so.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                var obj = it.objectReferenceValue;
                if (obj == null)
                {
                    continue;
                }

                var ruta = AssetDatabase.GetAssetPath(obj);
                var shader = obj as Shader;
                var texto = ruta + " " + obj.name;
                if (texto.Contains("BlockMaterial") || texto.Contains("DirectLight") || texto.Contains("MR_DirectLightOnly")
                    || (shader != null && shader.name.Contains("MR_DirectLightOnly")))
                {
                    log.AppendLine("[D] " + mono.name + "." + it.propertyPath + " -> " + texto);
                }
            }
        }

        log.AppendLine("[D] ---- materiales en escena ----");
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            var mats = renderers[i].sharedMaterials;
            for (int k = 0; k < mats.Length; k++)
            {
                if (mats[k] == null)
                {
                    continue;
                }

                log.AppendLine("[D] " + Ruta(renderers[i].transform) + " mat=" + mats[k].name
                    + " shader=" + mats[k].shader.name + " activo=" + renderers[i].gameObject.activeInHierarchy);
            }
        }

        return log.ToString();
    }

    private static string Ruta(Transform t)
    {
        var sb = new StringBuilder(t.name);
        var p = t.parent;
        while (p != null)
        {
            sb.Insert(0, p.name + "/");
            p = p.parent;
        }

        return sb.ToString();
    }

    private static void Volcar(Transform t, int nivel, StringBuilder log, int profundidadMax)
    {
        if (nivel > profundidadMax)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.Append(' ', nivel * 2).Append(t.name);
        var comps = t.GetComponents<Component>();
        sb.Append(" {");
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null)
            {
                sb.Append("MISSING ");
                continue;
            }

            sb.Append(comps[i].GetType().Name).Append(' ');
        }

        sb.Append('}');
        if (!t.gameObject.activeSelf)
        {
            sb.Append(" (inactivo)");
        }

        log.AppendLine(sb.ToString());

        for (int i = 0; i < t.childCount; i++)
        {
            Volcar(t.GetChild(i), nivel + 1, log, profundidadMax);
        }
    }
}
