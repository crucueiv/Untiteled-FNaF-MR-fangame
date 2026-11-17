using UnityEditor;
using UnityEngine;

/// <summary>
/// Entrypoint de ejecución por lotes para regenerar prefabs, construir rigs procedurales y asociar componentes de idle.
/// Diseñado para invocación externa sin interfaz de usuario.
/// </summary>
public static class RunDriveRigRebuild
{
    /// <summary>
    /// Ejecuta secuencialmente la reconstrucción de prefabs, rigs procedurales y vinculación de ProceduralIdle.
    /// </summary>
    /// <returns>"ok" si la ejecución finaliza con éxito.</returns>
    public static string Execute()
    {
        DriveCharacterRebuild.RebuildPrefabs();
        DriveCharacterRebuild.BuildProceduralRig();
        DriveCharacterRebuild.AttachProceduralIdle();
        Debug.Log("[DriveRebuild] Rig + Idle regenerados desde RunDriveRigRebuild.");
        return "ok";
    }
}
