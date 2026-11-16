using System.Text;
using UnityEngine;

// Fuerza la construccion del contorno en la instancia de escena (OnEnable no corre en edit mode).
public static class AuraShowHighlight
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        GameObject bloque = GameObject.Find("RoomObjects/GrabbableObject");
        if (bloque == null) return "no block";

        var h = bloque.GetComponent<InteractableHighlight>();
        if (h == null)
        {
            sb.AppendLine("[Aura] La instancia no tiene el componente todavia (prefab no aplicado).");
            h = InteractableHighlight.Attach(bloque);
        }
        else
        {
            h.Show();
        }

        int cascaras = 0;
        foreach (Transform t in bloque.transform)
        {
            if (t.name == "__HighlightOutline") cascaras++;
        }

        sb.AppendLine("[Aura] componente=" + (h != null) + " cascaras=" + cascaras +
                      " visible=" + (h != null && h.IsVisible));

        Debug.Log(sb.ToString());
        return sb.ToString();
    }
}
