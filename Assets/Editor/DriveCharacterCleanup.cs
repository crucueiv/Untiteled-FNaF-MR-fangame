using UnityEditor;
using UnityEngine;

/// <summary>
/// Elimina las mallas temporales generadas por versiones anteriores de DriveCharacterRebuild
/// (la carpeta ya no se usa: las piezas ocultas se resuelven con texturas, no recortando submeshes).
/// </summary>
public static class DriveCharacterCleanup
{
    private const string TempMeshFolder = "Assets/GameStuff/Prefabs/DriveCharacters/Meshes";

    [MenuItem("Tools/Drive Characters/9 - Clean Temp Meshes")]
    public static void CleanTempMeshes()
    {
        if (!AssetDatabase.IsValidFolder(TempMeshFolder))
        {
            Debug.Log("[DriveRebuild] CLEAN: no hay mallas temporales que borrar.");
            return;
        }

        AssetDatabase.DeleteAsset(TempMeshFolder);
        AssetDatabase.Refresh();
        Debug.Log($"[DriveRebuild] CLEAN: eliminada {TempMeshFolder}");
    }
}
