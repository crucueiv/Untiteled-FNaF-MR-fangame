using UnityEditor;
using UnityEngine;

public static class RunDriveRigRebuild
{
    public static string Execute()
    {
        DriveCharacterRebuild.RebuildPrefabs();
        DriveCharacterRebuild.BuildProceduralRig();
        DriveCharacterRebuild.AttachProceduralIdle();
        Debug.Log("[DriveRebuild] Rig + Idle regenerados desde RunDriveRigRebuild.");
        return "ok";
    }
}
