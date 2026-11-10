using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using UnityEngine.XR;

public sealed class FnafMenuInput : MonoBehaviour
{
    public FnafMenuController menu;
    public Camera viewCamera;
    public Transform reticle;
    public LayerMask buttonLayer;
    public float rayDistance = 8f;

    private Button hoveredButton;
    private MaterialPropertyBlock reticleProperties;
    private Renderer reticleRenderer;

    private void Awake()
    {
        reticleProperties = new MaterialPropertyBlock();

        if (viewCamera == null)
            viewCamera = Camera.main;

        if (reticle != null)
            reticleRenderer = reticle.GetComponentInChildren<Renderer>();
    }

    private void Update()
    {
        if (menu == null || viewCamera == null)
            return;

        bool pressed = ReadPrimaryPress();

        if (menu.IsSplash)
        {
            ClearHoveredButton();
            PlaceReticle(viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), 2.45f, false, true);
            if (pressed)
                menu.ShowMainMenu();
            return;
        }

        if (!menu.CanSelect)
        {
            ClearHoveredButton();
            if (reticle != null)
                reticle.gameObject.SetActive(false);
            return;
        }

        Ray ray = GetPointerRay();
        bool hitButton = Physics.Raycast(ray, out RaycastHit hit, rayDistance, buttonLayer, QueryTriggerInteraction.Collide);
        Button candidate = hitButton ? hit.collider.GetComponentInParent<Button>() : null;
        if (candidate != null && candidate.isActiveAndEnabled && candidate.IsInteractable())
        {
            SetHoveredButton(candidate);
            PlaceReticle(ray, hit.point, hit.normal, true, true);
        }
        else
        {
            ClearHoveredButton();
            PlaceReticle(ray, ray.origin + ray.direction * 2.45f, -ray.direction, false, true);
        }

        if (pressed && hoveredButton != null && hoveredButton.IsInteractable())
            hoveredButton.onClick.Invoke();
    }

    private Ray GetPointerRay()
    {
        if (XRSettings.isDeviceActive)
            return viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Mouse.current != null)
            return viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        return viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    private static bool ReadPrimaryPress()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        for (int deviceIndex = 0; deviceIndex < InputSystem.devices.Count; deviceIndex++)
        {
            UnityEngine.InputSystem.InputDevice device = InputSystem.devices[deviceIndex];
            if (device == Keyboard.current || device == Mouse.current)
                continue;

            for (int controlIndex = 0; controlIndex < device.allControls.Count; controlIndex++)
            {
                if (device.allControls[controlIndex] is ButtonControl button && button.wasPressedThisFrame)
                    return true;
            }
        }

        return false;
    }

    private void SetHoveredButton(Button next)
    {
        if (hoveredButton == next)
            return;

        if (hoveredButton != null && hoveredButton.targetGraphic != null)
            hoveredButton.targetGraphic.color = hoveredButton.colors.normalColor;

        hoveredButton = next;
        if (hoveredButton != null && hoveredButton.targetGraphic != null)
            hoveredButton.targetGraphic.color = hoveredButton.colors.highlightedColor;
    }

    private void ClearHoveredButton()
    {
        if (hoveredButton != null && hoveredButton.targetGraphic != null)
            hoveredButton.targetGraphic.color = hoveredButton.colors.normalColor;

        hoveredButton = null;
    }

    private void PlaceReticle(Ray ray, float distance, bool highlighted, bool visible)
    {
        PlaceReticle(ray, ray.origin + ray.direction * distance, -ray.direction, highlighted, visible);
    }

    private void PlaceReticle(Ray ray, Vector3 position, Vector3 normal, bool highlighted, bool visible)
    {
        if (reticle == null)
            return;

        reticle.gameObject.SetActive(visible);
        if (!visible)
            return;

        reticle.position = position + normal.normalized * 0.012f;
        reticle.rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0f ? normal : -ray.direction);

        if (reticleRenderer != null)
        {
            reticleRenderer.GetPropertyBlock(reticleProperties);
            Color color = highlighted ? FnafMenuPalette.Ember : FnafMenuPalette.Scarlet;
            reticleProperties.SetColor("_BaseColor", color);
            reticleProperties.SetColor("_Color", color);
            reticleRenderer.SetPropertyBlock(reticleProperties);
        }
    }
}
