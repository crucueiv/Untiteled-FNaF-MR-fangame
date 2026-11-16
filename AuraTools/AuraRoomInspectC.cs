using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bisect 3: jerarquia de huesos del modelo.
/// </summary>
public static class AuraRoomInspectC
{
    private const string FbxPath = "Assets/GameStuff/Models/DriveRigged/SpringBonnieVR/HWSpringBonnieIMPROVED.fbx";

    public static string Execute()
    {
        var log = new StringBuilder();
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (fbx == null)
        {
            return "[C] NO EXISTE " + FbxPath;
        }

        VolcarHuesos(fbx.transform, 0, log);
        return log.ToString();
    }

    private static void VolcarHuesos(Transform hueso, int nivel, StringBuilder log)
    {
        var sb = new StringBuilder();
        sb.Append(' ', nivel * 2).Append(hueso.name);
        log.AppendLine(sb.ToString());

        for (int i = 0; i < hueso.childCount; i++)
        {
            VolcarHuesos(hueso.GetChild(i), nivel + 1, log);
        }
    }
}
