using UnityEngine;
using Oculus.Interaction;
using System.Linq;

public class FlashlightController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el objeto Light (Spotlight) hijo de la linterna")]
    public Light spotlight;

    private GrabInteractable grabInteractable;
    private bool isFlashlightOn = false;
    private bool isGrabbed = false;
    private OVRInput.Controller grabbingController = OVRInput.Controller.None;

    void Awake()
    {
        grabInteractable = GetComponentInChildren<GrabInteractable>();
        if (grabInteractable == null)
            Debug.LogError("FlashlightController: No se encontró GrabInteractable en este objeto o sus hijos.");
    }

    void Start()
    {
        if (spotlight != null)
            spotlight.enabled = isFlashlightOn;
        else
            Debug.LogWarning("FlashlightController: No has asignado el Spotlight en el inspector.");
    }

    void OnEnable()
    {
        if (grabInteractable != null)
            grabInteractable.WhenStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (grabInteractable != null)
            grabInteractable.WhenStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(InteractableStateChangeArgs args)
    {
        if (args.NewState == InteractableState.Select)
        {
            isGrabbed = true;
            DetermineGrabbingController();
        }
        else if (args.PreviousState == InteractableState.Select)
        {
            isGrabbed = false;
            grabbingController = OVRInput.Controller.None;

            // El estado (on/off) persiste independientemente de si está agarrada o no.
        }
    }

    private void DetermineGrabbingController()
    {
        if (grabInteractable.SelectingInteractors.Count == 0) return;

        IInteractor interactor = grabInteractable.SelectingInteractors.FirstOrDefault();
        GameObject interactorGO = (interactor as MonoBehaviour)?.gameObject;
        if (interactorGO == null) return;

        bool isLeft = false;
        bool foundHand = false;
        Transform current = interactorGO.transform;

        // BÚSQUEDA PROFUNDA: Revisamos toda la jerarquía hacia arriba para encontrar 
        // si este interactor pertenece al brazo izquierdo o derecho, igual que GrabbableItemManager.
        while (current != null)
        {
            string nameLower = current.name.ToLower();
            if (nameLower.Contains("left") || nameLower.Contains("l_"))
            {
                isLeft = true;
                foundHand = true;
                break;
            }
            if (nameLower.Contains("right") || nameLower.Contains("r_"))
            {
                foundHand = true;
                break; // remains false (is right)
            }
            current = current.parent;
        }

        if (foundHand)
        {
            grabbingController = isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        }
        else
        {
            // Failsafe: usa cualquier mando activo
            grabbingController = OVRInput.Controller.Active;
        }
    }

    void Update()
    {
        // Solo actúa si está agarrada y el mando está identificado
        if (!isGrabbed || grabbingController == OVRInput.Controller.None) return;

        // PrimaryThumbstick + controller específico (LTouch/RTouch) siempre se refiere
        // al joystick de ESE mando. No es necesario cambiar el botón según la mano.
        // El error anterior: SecondaryThumbstick + RTouch buscaba el joystick izquierdo
        // filtrado por el mando derecho, lo cual nunca se disparaba.
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, grabbingController))
        {
            ToggleFlashlight();
        }
    }

    public void ToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn;
        if (spotlight != null)
            spotlight.enabled = isFlashlightOn;
    }
}