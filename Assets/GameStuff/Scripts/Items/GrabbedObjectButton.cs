using System.Linq;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detecta qué mando está agarrando este objeto y ejecuta una acción
/// cuando se pulsa el botón configurado en ese mismo mando.
/// </summary>
public class GrabbedObjectButton : MonoBehaviour
{
    [Header("Configuración de entrada")]
    [Tooltip("Botón del mando que ejecutará la acción mientras se agarra el objeto.")]
    [SerializeField] private OVRInput.Button button = OVRInput.Button.PrimaryThumbstick;

    [Header("Acción")]
    [Tooltip("Acción que se ejecuta al pulsar el botón configurado.")]
    [SerializeField] private UnityEvent onButtonPressed = new UnityEvent();

    private GrabInteractable grabInteractable;
    private OVRInput.Controller grabbingController = OVRInput.Controller.None;
    private bool isGrabbed;

    private void Awake()
    {
        FindGrabInteractable();
    }

    private void OnEnable()
    {
        if (grabInteractable == null)
        {
            FindGrabInteractable();
        }

        if (grabInteractable == null)
        {
            Debug.LogError("GrabbedObjectButton: No se encontró GrabInteractable en este objeto o sus hijos.", this);
            return;
        }

        grabInteractable.WhenStateChanged += HandleStateChanged;

        // Recupera el mando si este componente se activa mientras el objeto ya está agarrado.
        if (grabInteractable.SelectingInteractors.Count > 0)
        {
            isGrabbed = true;
            DetermineGrabbingController();
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.WhenStateChanged -= HandleStateChanged;
        }

        isGrabbed = false;
        grabbingController = OVRInput.Controller.None;
    }

    private void Update()
    {
        if (!isGrabbed || grabbingController == OVRInput.Controller.None)
        {
            return;
        }

        if (OVRInput.GetDown(button, grabbingController))
        {
            onButtonPressed?.Invoke();
        }
    }

    private void FindGrabInteractable()
    {
        grabInteractable = GetComponentInChildren<GrabInteractable>();
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
        }
    }

    private void DetermineGrabbingController()
    {
        if (grabInteractable.SelectingInteractors.Count == 0)
        {
            return;
        }

        IInteractor interactor = grabInteractable.SelectingInteractors.FirstOrDefault();
        GameObject interactorObject = (interactor as MonoBehaviour)?.gameObject;
        if (interactorObject == null)
        {
            return;
        }

        bool isLeftHand = false;
        bool foundHand = false;
        Transform current = interactorObject.transform;

        // Busca en toda la jerarquía la mano propietaria del interactor.
        while (current != null)
        {
            string nameLower = current.name.ToLowerInvariant();

            if (nameLower.Contains("left") || nameLower.Contains("l_"))
            {
                isLeftHand = true;
                foundHand = true;
                break;
            }

            if (nameLower.Contains("right") || nameLower.Contains("r_"))
            {
                foundHand = true;
                break;
            }

            current = current.parent;
        }

        if (foundHand)
        {
            grabbingController = isLeftHand
                ? OVRInput.Controller.LTouch
                : OVRInput.Controller.RTouch;
        }
        else
        {
            // Conserva el comportamiento de respaldo del controlador anterior.
            grabbingController = OVRInput.Controller.Active;
        }
    }
}
