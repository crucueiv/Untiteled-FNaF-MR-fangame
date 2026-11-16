using UnityEngine;

/// <summary>
/// Prueba minima para confirmar que execute_script compila y devuelve datos.
/// </summary>
public static class AuraMinimal
{
    public static string Execute()
    {
        return "ok " + Application.unityVersion;
    }
}
