using UnityEngine;

/// <summary>Comprobacion minima del ejecutor de scripts.</summary>
public static class AuraPing
{
    public static void Execute()
    {
        Debug.Log("[AuraPing] ok " + Application.unityVersion);
    }
}
