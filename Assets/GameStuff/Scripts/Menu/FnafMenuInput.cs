using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

public sealed class FnafMenuInput : MonoBehaviour
{
    private enum PointerSide
    {
        None,
        Left,
        Right
    }

    public FnafMenuController menu;
    public Camera viewCamera;
    public Transform leftPointer;
    public Transform rightPointer;
    public Transform playerHead;
    public Transform reticle;
    public LayerMask buttonLayer;
    public float rayDistance = 8f;
    public bool allowEditorMouseFallback = true;

    private Button hoveredButton;
    private PointerSide hoveredPointerSide;
    private MaterialPropertyBlock reticleProperties;
    private Renderer reticleRenderer;

    private void Awake()
    {
        reticleProperties = new MaterialPropertyBlock();

        if (viewCamera == null)
            viewCamera = Camera.main;

        if (playerHead == null && viewCamera != null)
            playerHead = viewCamera.transform;

        if (leftPointer == null)
            leftPointer = FindTransform("LeftControllerAnchor");

        if (rightPointer == null)
            rightPointer = FindTransform("RightControllerAnchor");

        if (leftPointer == null && rightPointer == null)
            Debug.LogError("FnafMenuInput: no se encontró ningún ancla de controlador para el raycast del menú.");

        if (reticle != null)
            reticleRenderer = reticle.GetComponentInChildren<Renderer>();
    }

    private void Update()
    {
        if (menu == null || viewCamera == null)
            return;

        if (XRSettings.isDeviceActive && leftPointer == null && rightPointer == null)
        {
            ClearHoveredButton();
            if (reticle != null)
                reticle.gameObject.SetActive(false);
            return;
        }

        if (menu.IsSplash)
        {
            ClearHoveredButton();
            PointerSide splashPointer = ChoosePointerSide();
            Ray splashRay = GetRay(splashPointer);
            PlaceReticle(splashRay, 2.45f, false, true);
            if (ReadTriggerDown(splashPointer))
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

        PointerSide pointerSide = ChoosePointerSide(out Ray ray, out RaycastHit hit, out Button candidate);
        if (candidate != null && candidate.isActiveAndEnabled && candidate.IsInteractable())
        {
            SetHoveredButton(candidate);
            hoveredPointerSide = pointerSide;
            PlaceReticle(ray, hit.point, hit.normal, true, true);
        }
        else
        {
            ClearHoveredButton();
            PlaceReticle(ray, ray.origin + ray.direction * 2.45f, -ray.direction, false, true);
        }

        if (ReadTriggerDown(hoveredPointerSide) && hoveredButton != null && hoveredButton.IsInteractable())
            hoveredButton.onClick.Invoke();
    }

    private PointerSide ChoosePointerSide()
    {
        return ChoosePointerSide(out _, out _, out _);
    }

    private PointerSide ChoosePointerSide(out Ray selectedRay, out RaycastHit selectedHit, out Button selectedButton)
    {
        selectedRay = GetFallbackRay();
        selectedHit = default;
        selectedButton = null;

        PointerSide bestSide = PointerSide.None;
        float bestScore = float.NegativeInfinity;
        bool foundButton = false;

        EvaluatePointer(PointerSide.Left, leftPointer, ref bestSide, ref bestScore, ref foundButton, ref selectedRay, ref selectedHit, ref selectedButton);
        EvaluatePointer(PointerSide.Right, rightPointer, ref bestSide, ref bestScore, ref foundButton, ref selectedRay, ref selectedHit, ref selectedButton);

        if (bestSide != PointerSide.None)
            return bestSide;

        if (allowEditorMouseFallback && !XRSettings.isDeviceActive && Mouse.current != null)
        {
            selectedRay = GetFallbackRay();
            if (Physics.Raycast(selectedRay, out selectedHit, rayDistance, buttonLayer, QueryTriggerInteraction.Collide))
            {
                selectedButton = selectedHit.collider.GetComponentInParent<Button>();
                if (selectedButton != null && selectedButton.isActiveAndEnabled && selectedButton.IsInteractable())
                    return PointerSide.None;
            }
        }

        return PointerSide.None;
    }

    private void EvaluatePointer(
        PointerSide side,
        Transform pointer,
        ref PointerSide bestSide,
        ref float bestScore,
        ref bool foundButton,
        ref Ray selectedRay,
        ref RaycastHit selectedHit,
        ref Button selectedButton)
    {
        if (pointer == null)
            return;

        Ray ray = new Ray(pointer.position, pointer.forward);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, rayDistance, buttonLayer, QueryTriggerInteraction.Collide);
        Button button = hit ? hitInfo.collider.GetComponentInParent<Button>() : null;
        bool validButton = button != null && button.isActiveAndEnabled && button.IsInteractable();

        Vector3 viewDirection = viewCamera.transform.forward;
        float score = Vector3.Dot(viewDirection, ray.direction);
        if (validButton)
            score += 2f;

        if ((validButton && !foundButton) || (validButton == foundButton && score > bestScore))
        {
            bestSide = side;
            bestScore = score;
            foundButton = validButton;
            selectedRay = ray;
            selectedHit = hitInfo;
            selectedButton = validButton ? button : null;
        }
    }

    private Ray GetRay(PointerSide side)
    {
        if (side == PointerSide.Left && leftPointer != null)
            return new Ray(leftPointer.position, leftPointer.forward);

        if (side == PointerSide.Right && rightPointer != null)
            return new Ray(rightPointer.position, rightPointer.forward);

        return GetFallbackRay();
    }

    private Ray GetFallbackRay()
    {
        if (allowEditorMouseFallback && !XRSettings.isDeviceActive && Mouse.current != null)
            return viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!XRSettings.isDeviceActive)
            return viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        return new Ray(Vector3.zero, Vector3.forward);
    }

    private bool ReadTriggerDown(PointerSide side)
    {
        if (side == PointerSide.Left && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            return true;

        if (side == PointerSide.Right && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            return true;

        if (side == PointerSide.None)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
                return true;

            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                return true;
        }

        return allowEditorMouseFallback &&
               !XRSettings.isDeviceActive &&
               Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
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
        hoveredPointerSide = PointerSide.None;
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

    private static Transform FindTransform(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }
}
