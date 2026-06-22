using UnityEngine;
using Oculus.Interaction;
using System.Collections; // Necesario para usar Corrutinas

[RequireComponent(typeof(Collider))]
public class InventorySlot : MonoBehaviour
{
    [Header("Configuración del Slot")]
    [Tooltip("El punto exacto donde se colocará el objeto. Puede ser este mismo GameObject.")]
    public Transform snapPoint;

    private GameObject currentItemInRange;
    private GameObject storedItem;
    private GrabInteractable itemGrabInteractable;

    private void Awake()
    {
        // Nos aseguramos de que el collider sea un trigger
        GetComponent<Collider>().isTrigger = true;
        if (snapPoint == null) snapPoint = this.transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si el slot está vacío y entra un objeto con nuestro Tag
        if (other.CompareTag("KeepableObject") && storedItem == null)
        {
            // Obtenemos el objeto raíz (por si el collider está en un hijo)
            currentItemInRange = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
            
            // Buscamos el componente de interacción del Meta SDK
            itemGrabInteractable = currentItemInRange.GetComponentInChildren<GrabInteractable>();
            if (itemGrabInteractable != null)
            {
                itemGrabInteractable.WhenStateChanged += OnItemStateChanged;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Si el objeto sale del rango antes de soltarlo, cancelamos la detección
        if (currentItemInRange != null && (other.gameObject == currentItemInRange || (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == currentItemInRange)))
        {
            if (storedItem == null && itemGrabInteractable != null)
            {
                itemGrabInteractable.WhenStateChanged -= OnItemStateChanged;
                itemGrabInteractable = null;
                currentItemInRange = null;
            }
        }
    }

    private void OnItemStateChanged(InteractableStateChangeArgs args)
    {
        // Si el jugador SUELTA el objeto dentro del slot
        if (args.PreviousState == InteractableState.Select && args.NewState != InteractableState.Select)
        {
            if (storedItem == null)
            {
                // Iniciamos la corrutina para esperar al final del frame
                StartCoroutine(StoreItemRoutine(currentItemInRange));
            }
        }
        // Si el jugador VUELVE A AGARRAR el objeto ya guardado
        else if (args.NewState == InteractableState.Select && storedItem == currentItemInRange)
        {
            RemoveItem();
        }
    }

    private IEnumerator StoreItemRoutine(GameObject item)
    {
        storedItem = item;
        
        // Magia aquí: Esperamos a que Meta SDK termine de calcular y aplicar sus físicas de lanzamiento
        yield return new WaitForSeconds(0.05f);;

        // Verificamos que el objeto siga existiendo después de la pausa
        if (storedItem != null)
        {
            Rigidbody rb = storedItem.GetComponent<Rigidbody>();
            if (rb != null) 
            {
                rb.isKinematic = true;
                // Unity 6 usa linearVelocity en lugar del antiguo velocity, lo ponemos a 0 por seguridad
                rb.linearVelocity = Vector3.zero; 
                rb.angularVelocity = Vector3.zero;
            }

            // Lo emparentamos y lo centramos en el slot
            storedItem.transform.SetParent(snapPoint);
            storedItem.transform.localPosition = Vector3.zero;
            storedItem.transform.localRotation = Quaternion.identity;
        }
    }

    private void RemoveItem()
    {
        if (storedItem != null)
        {
            // Lo liberamos del parentesco. El PhysicsGrabbable de Meta reactivará sus físicas automáticamente
            storedItem.transform.SetParent(null); 
            storedItem = null;
        }
    }

    private void OnDestroy()
    {
        // Limpieza de eventos por seguridad
        if (itemGrabInteractable != null)
        {
            itemGrabInteractable.WhenStateChanged -= OnItemStateChanged;
        }
    }
}