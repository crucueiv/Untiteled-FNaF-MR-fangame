using System.Text;
using UnityEditor;
using UnityEngine;

// Vuelca la jerarquia de MRThings.prefab con los componentes que nos interesan para las manos.
public static class AuraRigInspect
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        string ruta = "Assets/GameStuff/Prefabs/MRThings.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) return "[Aura] no se encuentra MRThings.prefab";

        foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
        {
            int prof = 0;
            Transform p = t.parent;
            while (p != null) { prof++; p = p.parent; }
            sb.Append(' ', prof * 2).Append(t.name);

            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null) continue;
                string n = c.GetType().Name;
                if (n == "Transform") continue;
                sb.Append(" [").Append(n).Append(']');
            }

            var mr = t.GetComponent<MeshRenderer>();
            if (mr != null) sb.Append(" {renderer}");
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
        return sb.ToString();
    }
}
