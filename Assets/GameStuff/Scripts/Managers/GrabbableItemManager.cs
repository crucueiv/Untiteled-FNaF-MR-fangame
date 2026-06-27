using UnityEngine;
using Oculus.Interaction;
using System.Linq;

public class GrabbableItemManager : MonoBehaviour
{
    [Header("Configuración de Agarre")]
    [Tooltip("Crea un GameObject vacío hijo de este objeto, colócalo donde quieras agarrarlo y arrástralo aquí.")]
    public Transform grabAnchor;

    [Header("Referencias de los Mandos Visuales")]
    [SerializeField] private GameObject mandoIzquierdoVisual;
    [SerializeField] private GameObject mandoDerechoVisual;

    private GameObject mandoOcultadoActualmente;

    private GrabInteractable grabInteractable;
    private Rigidbody rb;

    private void Awake()
    {
        // Obtenemos las referencias necesarias
        grabInteractable = this.GetComponentInChildren<GrabInteractable>();
        rb = this.GetComponent<Rigidbody>();

        if(grabInteractable != null)
        {
            grabInteractable.WhenStateChanged += OnInteractableStateChanged;
        }
    }

    private void Start() {
        mandoDerechoVisual = GameObject.Find("VisualsManager").GetComponent<VisualsManager>().rightVisual;
        mandoIzquierdoVisual = GameObject.Find("VisualsManager").GetComponent<VisualsManager>().leftVisual;     
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.WhenStateChanged -= OnInteractableStateChanged;
        }
    }

    private void OnInteractableStateChanged(InteractableStateChangeArgs args)
    {
        if (args.NewState == InteractableState.Select)
        {
            IInteractor interactor = grabInteractable.SelectingInteractors.FirstOrDefault();
            GameObject interactorGO = (interactor as MonoBehaviour)?.gameObject;

                    if (interactorGO == null) return;

            // BÚSQUEDA PROFUNDA: Revisamos toda la jerarquía hacia arriba para encontrar 
            // si este interactor pertenece al brazo izquierdo o derecho.
            bool esIzquierdo = false;
            Transform actual = interactorGO.transform;
            
            while (actual != null)
            {
                string nombre = actual.name.ToLower();
                if (nombre.Contains("left") || nombre.Contains("l_")) 
                { 
                    esIzquierdo = true; 
                    break; 
                }
                if (nombre.Contains("right") || nombre.Contains("r_")) 
                { 
                    break; // Se queda en false (es derecho)
                }
                actual = actual.parent;
            }

            // Ocultamos el mando correspondiente
            if (esIzquierdo && mandoIzquierdoVisual != null)
            {
                mandoIzquierdoVisual.SetActive(false);
                mandoOcultadoActualmente = mandoIzquierdoVisual;
            }
            else if (!esIzquierdo && mandoDerechoVisual != null)
            {
                mandoDerechoVisual.SetActive(false);
                mandoOcultadoActualmente = mandoDerechoVisual;
            }
    
            
            

            // 2. IMPORTANTE: Lo desvinculamos del slot del inventario para que deje de seguir tu cintura
            this.transform.SetParent(null);

            // 3. Despertamos las físicas ANTES de que el SDK procese el lanzamiento
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
        else if (args.PreviousState == InteractableState.Select && args.NewState != InteractableState.Select)
        {
            // Muestra los mandos al soltar
            if (mandoOcultadoActualmente != null)
        {
            mandoOcultadoActualmente.SetActive(true);
            mandoOcultadoActualmente = null;
        }
        }
    }
}