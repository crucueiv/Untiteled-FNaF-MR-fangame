using System.Text;
using UnityEngine;

/// <summary>Vuelca los nombres de huesos que contienen index/thumb/foot para fijar los nombres reales.</summary>
public static class AuraBoneNames
{
    public static string Execute()
    {
        var log = new StringBuilder();
        var todas = Object.FindObjectsOfType<Transform>(true);
        Transform figura = null;
        for (int i = 0; i < todas.Length; i++)
        {
            if (todas[i].name == "Springbonnie_MR")
            {
                figura = todas[i];
                break;
            }
        }

        if (figura == null)
        {
            return "[B] no encuentro Springbonnie_MR";
        }

        var huesos = figura.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < huesos.Length; i++)
        {
            string n = huesos[i].name;
            if (n.Contains("index") || n.Contains("thumb") || n.Contains("middle")
                || n.Contains("pinky") || n.Contains("ring") || n.Contains("foot") || n.Contains("hand"))
            {
                log.AppendLine("[B] " + n);
            }
        }

        return log.ToString();
    }
}
