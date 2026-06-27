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
    private OVRInput.Button buttonToInteract = OVRInput.Button.None;

    void Awake()
    {
        // Buscamos automáticamente el componente de agarre de Meta SDK
        grabInteractable = GetComponentInChildren<GrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("FlashlightController: No se encontró un componente GrabInteractable en este objeto o sus hijos.");
        }
    }

    void Start()
    {
        if (spotlight != null)
        {
            spotlight.enabled = isFlashlightOn;
        }
        else
        {
            Debug.LogWarning("FlashlightController: No has asignado el Spotlight en el inspector.");
        }
    }

    void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.WhenStateChanged += HandleStateChanged;
        }
    }

    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.WhenStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(InteractableStateChangeArgs args)
    {
        // Se ejecuta cuando el jugador agarra el objeto
        if (args.NewState == InteractableState.Select)
        {
            isGrabbed = true;
            DetermineGrabbingController();
        }
        // Se ejecuta cuando el jugador suelta el objeto
        else if (args.NewState != InteractableState.Select && args.PreviousState == InteractableState.Select)
        {
            isGrabbed = false;
            grabbingController = OVRInput.Controller.None;

            // Opcional: Apagamos la linterna automáticamente al soltarla para evitar bugs de batería o iluminación
            if (isFlashlightOn)
            {
                ToggleFlashlight();
            }
        }
    }

    private void DetermineGrabbingController()
    {
        // Evaluamos cuál de los interactores de Meta ha seleccionado la linterna
            if(grabInteractable.SelectingInteractors.Count == 0) return;
            IInteractor interactor = grabInteractable.SelectingInteractors.FirstOrDefault();
            GameObject interactorGO = (interactor as MonoBehaviour)?.gameObject;
            string interactorName = interactorGO.name.ToLower();
            
            // Analizamos el nombre del bloque que interactúa (Meta suele llamarlos LeftController... o RightController...)
            if (interactorName.Contains("left") || interactorName.Contains("l_"))
            {
                grabbingController = OVRInput.Controller.LTouch;
                buttonToInteract = OVRInput.Button.PrimaryThumbstick;
                return;
            }
            if (interactorName.Contains("right") || interactorName.Contains("r_"))
            {
                grabbingController = OVRInput.Controller.RTouch;
                buttonToInteract = OVRInput.Button.SecondaryThumbstick;
                return;
            }
        
        
        // Failsafe: Si por alguna razón no lo detecta, usamos el mando activo
        grabbingController = OVRInput.Controller.Active;
    }

    void Update()
    {
        // Si no está agarrada o no se reconoce el mando, no hacemos nada
        if (!isGrabbed || grabbingController == OVRInput.Controller.None) return;

        // OVRInput.Button.PrimaryThumbstick mapea el "Click" hacia dentro del Joystick físico
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, grabbingController))
        {
            ToggleFlashlight();
        }
    }

    public void ToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn;
        if (spotlight != null)
        {
            spotlight.enabled = isFlashlightOn;
        }
    }
}