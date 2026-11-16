using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Lista las raices de la escena activa y las instancias de Springbonnie que haya.</summary>
public static class AuraRootsDiag
{
    public static string Execute()
    {
        var log = new StringBuilder();
        var escena = EditorSceneManager.GetActiveScene();
        log.AppendLine("[R] escena=" + escena.path + "  dirty=" + escena.isDirty);

        var raices = escena.GetRootGameObjects();
        for (int i = 0; i < raices.Length; i++)
        {
            var r = raices[i];
            log.AppendLine("[R] root " + i + ": " + r.name
                + " activo=" + r.activeSelf
                + " pos=" + V(r.transform.position)
                + " euler=" + V(r.transform.eulerAngles)
                + " escala=" + V(r.transform.lossyScale)
                + " hijos=" + r.transform.childCount);
        }

        var todas = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < todas.Length; i++)
        {
            var t = todas[i];
            if (t.name != "Springbonnie_MR" && t.name != "SpringBonnie_Suit")
            {
                continue;
            }

            log.AppendLine("[R] FIGURA " + t.name + " ruta=" + Ruta(t)
                + " activo=" + t.gameObject.activeInHierarchy
                + " pos=" + V(t.position)
                + " euler=" + V(t.eulerAngles)
                + " escala=" + V(t.lossyScale));

            var comps = t.GetComponents<Component>();
            var nombres = new StringBuilder();
            for (int c = 0; c < comps.Length; c++)
            {
                nombres.Append(comps[c] == null ? "NULL" : comps[c].GetType().Name).Append(' ');
            }

            log.AppendLine("[R]   componentes: " + nombres);

            var res = t.GetComponentsInChildren<Renderer>(true);
            int activos = 0;
            for (int r2 = 0; r2 < res.Length; r2++)
            {
                if (res[r2].enabled)
                {
                    activos++;
                }
            }

            log.AppendLine("[R]   renderers=" + res.Length + " activos=" + activos);
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

    private static string V(Vector3 v)
    {
        return "(" + v.x.ToString("F3") + ", " + v.y.ToString("F3") + ", " + v.z.ToString("F3") + ")";
    }
}
