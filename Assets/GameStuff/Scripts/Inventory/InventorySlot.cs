using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Punto físico de almacenamiento VR. Añade este componente a un collider
/// trigger para crear otro bolsillo o soporte.
/// </summary>
[RequireComponent(typeof(Collider))]
public class InventorySlot : MonoBehaviour
{
    private static readonly HashSet<InventorySlot> ActiveSlots = new HashSet<InventorySlot>();

    [Header("Punto de almacenamiento")]
    [Tooltip("Bolsillo para objetos normales o soporte de hombro para los objetos autorizados en su componente InventoryItem.")]
    [SerializeField] private InventorySlotKind slotKind = InventorySlotKind.Pocket;

    [Tooltip("Punto donde se coloca el objeto. Si se deja vacío, se usa el Transform de este objeto.")]
    [SerializeField] private Transform snapPoint;

    [Tooltip("Ajuste local de posición del objeto guardado.")]
    [SerializeField] private Vector3 snapPositionOffset;

    [Tooltip("Ajuste local de rotación del objeto guardado; permite orientar la linterna hacia delante.")]
    [SerializeField] private Vector3 snapRotationOffset;

    [Tooltip("Solo se guardan objetos cuya raíz tenga esta etiqueta. Déjalo vacío para aceptar cualquier etiqueta.")]
    [SerializeField] private string keepableTag = "KeepableObject";

    private sealed class Candidate
    {
        public InventoryItem Item;
        public GrabInteractable Interactable;
        public Action<InteractableStateChangeArgs> StateChanged;
        public bool StorePending;
        public readonly HashSet<Collider> Colliders = new HashSet<Collider>();
    }

    private readonly Dictionary<GameObject, Candidate> candidates = new Dictionary<GameObject, Candidate>();
    private GameObject storedItem;
    private InventoryItem storedItemData;
    private Rigidbody storedRigidbody;
    private bool storedWasKinematic;

    public InventorySlotKind SlotKind => slotKind;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (snapPoint == null)
        {
            snapPoint = transform;
        }
    }

    private void OnEnable()
    {
        ActiveSlots.Add(this);
    }

    private void OnDisable()
    {
        ActiveSlots.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        GameObject itemRoot = ResolveItemRoot(other);
        if (itemRoot == null || !HasKeepableTag(other, itemRoot))
        {
            return;
        }

        InventoryItem itemData = itemRoot.GetComponent<InventoryItem>();
        if (itemData == null)
        {
            // Los objetos antiguos siguen funcionando como artículos de bolsillo.
            // El componente también evita que dos slots reclamen el mismo objeto.
            itemData = itemRoot.AddComponent<InventoryItem>();
        }

        if (!itemData.CanStoreIn(slotKind))
        {
            return;
        }

        GrabInteractable grabInteractable = itemRoot.GetComponentInChildren<GrabInteractable>(true);
        if (grabInteractable == null)
        {
            return;
        }

        if (!candidates.TryGetValue(itemRoot, out Candidate candidate))
        {
            candidate = new Candidate
            {
                Item = itemData,
                Interactable = grabInteractable
            };
            GameObject capturedItem = itemRoot;
            candidate.StateChanged = args => HandleItemStateChanged(capturedItem, args);
            candidates.Add(itemRoot, candidate);
            grabInteractable.WhenStateChanged += candidate.StateChanged;
        }

        candidate.Colliders.Add(other);

        // También recoge objetos que entran en un bolsillo justo después de soltarlos.
        if (grabInteractable.State != InteractableState.Select)
        {
            ScheduleStore(itemRoot, candidate);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null)
        {
            return;
        }

        GameObject itemRoot = ResolveItemRoot(other);
        if (itemRoot == null || !candidates.TryGetValue(itemRoot, out Candidate candidate))
        {
            return;
        }

        candidate.Colliders.Remove(other);
        if (candidate.Colliders.Count == 0 && storedItem != itemRoot)
        {
            Unsubscribe(itemRoot, candidate);
        }
    }

    private void HandleItemStateChanged(GameObject item, InteractableStateChangeArgs args)
    {
        if (args.NewState == InteractableState.Select)
        {
            if (storedItem == item)
            {
                RemoveStoredItem();
            }

            return;
        }

        if (args.PreviousState == InteractableState.Select && storedItem == null &&
            candidates.TryGetValue(item, out Candidate candidate) && candidate.Colliders.Count > 0 &&
            candidate.Item != null)
        {
            ScheduleStore(item, candidate);
        }
    }

    private void ScheduleStore(GameObject item, Candidate candidate)
    {
        if (candidate.StorePending || storedItem != null)
        {
            return;
        }

        candidate.StorePending = true;
        StartCoroutine(StoreItemAfterRelease(item, candidate));
    }

    private static InventorySlot FindPreferredSlot(GameObject item, InventoryItem itemData)
    {
        if (item == null || itemData == null || itemData.ReservedSlot != null)
        {
            return null;
        }

        InventorySlot bestSlot = null;
        float bestDistance = float.PositiveInfinity;

        foreach (InventorySlot slot in ActiveSlots)
        {
            if (slot == null || slot.storedItem != null || !itemData.CanStoreIn(slot.slotKind) ||
                !slot.candidates.TryGetValue(item, out Candidate candidate) || candidate.Colliders.Count == 0)
            {
                continue;
            }

            float distance = (item.transform.position - slot.snapPoint.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSlot = slot;
            }
        }

        return bestSlot;
    }

    private IEnumerator StoreItemAfterRelease(GameObject item, Candidate pendingCandidate)
    {
        // Deja que el SDK termine el cálculo de la suelta y del lanzamiento.
        yield return new WaitForFixedUpdate();

        if (this == null)
        {
            yield break;
        }

        pendingCandidate.StorePending = false;
        if (item == null || storedItem != null || pendingCandidate.Interactable == null ||
            pendingCandidate.Interactable.State == InteractableState.Select ||
            !candidates.TryGetValue(item, out Candidate candidate) || candidate != pendingCandidate ||
            candidate.Colliders.Count == 0 || candidate.Item == null ||
            FindPreferredSlot(item, candidate.Item) != this || !candidate.Item.TryReserveSlot(this))
        {
            yield break;
        }

        storedItem = item;
        storedItemData = candidate.Item;
        storedRigidbody = item.GetComponent<Rigidbody>();
        if (storedRigidbody != null)
        {
            storedWasKinematic = storedRigidbody.isKinematic;
            storedRigidbody.isKinematic = true;
            storedRigidbody.linearVelocity = Vector3.zero;
            storedRigidbody.angularVelocity = Vector3.zero;
        }

        item.transform.SetParent(snapPoint, false);
        item.transform.localPosition = snapPositionOffset;
        item.transform.localRotation = Quaternion.Euler(snapRotationOffset);
    }

    private void RemoveStoredItem()
    {
        GameObject item = storedItem;
        InventoryItem itemData = storedItemData;
        Rigidbody body = storedRigidbody;

        storedItem = null;
        storedItemData = null;
        storedRigidbody = null;

        if (itemData != null)
        {
            itemData.ReleaseSlot(this);
        }

        if (item != null)
        {
            item.transform.SetParent(null, true);
            if (body != null)
            {
                body.isKinematic = storedWasKinematic;
            }
        }

        if (item != null && candidates.TryGetValue(item, out Candidate candidate) && candidate.Colliders.Count == 0)
        {
            Unsubscribe(item, candidate);
        }
    }

    private static GameObject ResolveItemRoot(Collider other)
    {
        InventoryItem itemData = other.GetComponentInParent<InventoryItem>();
        if (itemData != null)
        {
            return itemData.gameObject;
        }

        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.gameObject;
        }

        return other.transform.root.gameObject;
    }

    private bool HasKeepableTag(Collider other, GameObject itemRoot)
    {
        return string.IsNullOrEmpty(keepableTag) || itemRoot.tag == keepableTag || other.tag == keepableTag;
    }

    private void Unsubscribe(GameObject item, Candidate candidate)
    {
        if (candidate.Interactable != null && candidate.StateChanged != null)
        {
            candidate.Interactable.WhenStateChanged -= candidate.StateChanged;
        }

        candidates.Remove(item);
    }

    private void OnDestroy()
    {
        ActiveSlots.Remove(this);

        if (storedItem != null)
        {
            RemoveStoredItem();
        }

        foreach (KeyValuePair<GameObject, Candidate> pair in candidates)
        {
            if (pair.Value.Interactable != null && pair.Value.StateChanged != null)
            {
                pair.Value.Interactable.WhenStateChanged -= pair.Value.StateChanged;
            }
        }

        candidates.Clear();
    }
}
