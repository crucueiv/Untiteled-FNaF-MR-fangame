using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bisect 1: solo la parte de escena.
/// </summary>
public static class AuraRoomInspectA
{
    public static string Execute()
    {
        var log = new StringBuilder();
        var escena = SceneManager.GetActiveScene();
        log.AppendLine("[A] escena=" + escena.path);
        var raices = escena.GetRootGameObjects();
        log.AppendLine("[A] raices=" + raices.Length);
        for (int i = 0; i < raices.Length; i++)
        {
            VolcarObjeto(raices[i].transform, 0, log, 2);
        }

        return log.ToString();
    }

    private static void VolcarObjeto(Transform t, int nivel, StringBuilder log, int profundidadMax)
    {
        if (nivel > profundidadMax)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.Append(' ', nivel * 2).Append(t.name);

        var renderers = t.GetComponents<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            var m = renderers[i].sharedMaterial;
            sb.Append(" [mat=").Append(m != null ? m.name : "-").Append(']');
        }

        if (t.GetComponent<Light>() != null) sb.Append(" [LUZ]");
        if (!t.gameObject.activeSelf) sb.Append(" (inactivo)");
        if (t.childCount > 0) sb.Append(" hijos=").Append(t.childCount);
        log.AppendLine(sb.ToString());

        for (int i = 0; i < t.childCount; i++)
        {
            VolcarObjeto(t.GetChild(i), nivel + 1, log, profundidadMax);
        }
    }
}
