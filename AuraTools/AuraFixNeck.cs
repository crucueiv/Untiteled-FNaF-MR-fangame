using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ajusta el cuello (cabeza menos hundida) en el prefab y en la instancia de la escena,
/// reaplica la pose y guarda. Se usa tras cambiar codigo de la pose.
/// </summary>
public static class AuraFixNeck
{
    private const string RutaPrefab = "Assets/GameStuff/Prefabs/Characters/SpringBonnie_Suit.prefab";
    private static readonly Vector3 DireccionCuello = new Vector3(0f, -0.42f, 0.91f);

    public static string Execute()
    {
        var log = new StringBuilder();

        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        if (raiz == null)
        {
            return "[P] no puedo abrir el prefab";
        }

        var posePrefab = raiz.GetComponent<SpringBonnieStaticPose>();
        posePrefab.direccionCuello = DireccionCuello;
        posePrefab.AplicarPose();
        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        PrefabUtility.UnloadPrefabContents(raiz);
        log.AppendLine("[P] prefab: direccionCuello=" + DireccionCuello.ToString("F2"));

        var escena = EditorSceneManager.GetActiveScene();
        var todas = Object.FindObjectsOfType<SpringBonnieStaticPose>(true);
        for (int i = 0; i < todas.Length; i++)
        {
            var pose = todas[i];
            if (pose == null || EditorUtility.IsPersistent(pose))
            {
                continue;
            }

            var so = new SerializedObject(pose);
            so.FindProperty("direccionCuello").vector3Value = DireccionCuello;
            so.ApplyModifiedPropertiesWithoutUndo();
            pose.AplicarPose();
            EditorUtility.SetDirty(pose);
            log.AppendLine("[P] instancia: " + pose.gameObject.name);
        }

        AssetDatabase.SaveAssets();
        if (escena.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            log.AppendLine("[P] escena guardada: " + escena.name);
        }

        return log.ToString();
    }
}
