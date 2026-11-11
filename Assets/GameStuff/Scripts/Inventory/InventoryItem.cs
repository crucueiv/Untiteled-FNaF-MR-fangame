using UnityEngine;

public enum InventorySlotKind
{
    Pocket = 0,
    Shoulder = 1
}

[System.Flags]
public enum InventorySlotMask
{
    None = 0,
    Pocket = 1 << 0,
    Shoulder = 1 << 1
}

/// <summary>
/// Define qué puntos de almacenamiento pueden aceptar este objeto.
/// </summary>
[DisallowMultipleComponent]
public class InventoryItem : MonoBehaviour
{
    [Tooltip("Activa Hombro para los objetos que quieras poder apoyar y seguir usando con las manos libres, como la linterna.")]
    [SerializeField] private InventorySlotMask allowedSlots = InventorySlotMask.Pocket;

    private InventorySlot reservedSlot;

    public InventorySlot ReservedSlot => reservedSlot;

    public bool CanStoreIn(InventorySlotKind slotKind)
    {
        InventorySlotMask requestedSlot = slotKind == InventorySlotKind.Shoulder
            ? InventorySlotMask.Shoulder
            : InventorySlotMask.Pocket;

        return (allowedSlots & requestedSlot) != 0;
    }

    public bool TryReserveSlot(InventorySlot slot)
    {
        if (slot == null || !CanStoreIn(slot.SlotKind))
        {
            return false;
        }

        if (reservedSlot != null && reservedSlot != slot)
        {
            return false;
        }

        reservedSlot = slot;
        return true;
    }

    public void ReleaseSlot(InventorySlot slot)
    {
        if (reservedSlot == slot)
        {
            reservedSlot = null;
        }
    }
}
