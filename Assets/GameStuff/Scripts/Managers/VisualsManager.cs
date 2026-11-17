using UnityEngine;

/// <summary>
/// Almacén de referencias para los modelos visuales de los controladores (mando izquierdo y derecho).
/// Consultado por componentes como <see cref="GrabbableItemManager"/> para ocultar o mostrar los mandos durante el agarre.
/// </summary>
public class VisualsManager : MonoBehaviour
{
    /// <summary>GameObject que contiene la representación visual del controlador izquierdo.</summary>
    public GameObject leftVisual;

    /// <summary>GameObject que contiene la representación visual del controlador derecho.</summary>
    public GameObject rightVisual;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
