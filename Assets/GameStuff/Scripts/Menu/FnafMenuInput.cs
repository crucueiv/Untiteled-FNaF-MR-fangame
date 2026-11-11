using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Entrada del menú de FNaF en VR (Meta Quest).
/// - Splash: avanza al pulsar CUALQUIER botón físico del mando (los toques capacitivos y la
///   inclinación del stick no cuentan).
/// - Menú: cada mando emite un láser (LineRenderer) que apunta a los botones y también se
///   pueden pulsar llevando el mando/la mano físicamente contra la pantalla (poke).
/// </summary>
public sealed class FnafMenuInput : MonoBehaviour
{
    private enum PointerSide
    {
        None,
        Left,
        Right
    }

    [Header("Referencias")]
    public FnafMenuController menu;
    public Camera viewCamera;
    public Transform leftPointer;
    public Transform rightPointer;
    public Transform playerHead;
    public Transform reticle;
    public LayerMask buttonLayer;
    public float rayDistance = 8f;

    [Header("Láser")]
    public bool showLaser = true;
    public float laserWidth = 0.006f;
    public Color laserIdleColor = new Color(1f, 0.22f, 0.16f, 0.55f);
    public Color laserHitColor = new Color(1f, 0.62f, 0.16f, 0.95f);

    [Header("Poke (contacto físico)")]
    public float pokeForwardOffset = 0.075f;
    public float pokeRadius = 0.032f;
    public float pokeCooldown = 0.35f;

    [Header("Editor")]
    public bool allowEditorMouseFallback = true;

    private Button hoveredButton;
    private PointerSide hoveredPointerSide;
    private MaterialPropertyBlock reticleProperties;
    private Renderer reticleRenderer;
    private LineRenderer leftLaser;
    private LineRenderer rightLaser;
    private Button leftTouchingButton;
    private Button rightTouchingButton;
    private float leftCooldown;
    private float rightCooldown;

    private static readonly OVRInput.Button[] PhysicalButtons =
    {
        OVRInput.Button.One,
        OVRInput.Button.Two,
        OVRInput.Button.Three,
        OVRInput.Button.Four,
        OVRInput.Button.PrimaryIndexTrigger,
        OVRInput.Button.SecondaryIndexTrigger,
        OVRInput.Button.PrimaryHandTrigger,
        OVRInput.Button.SecondaryHandTrigger,
        OVRInput.Button.PrimaryThumbstick,
        OVRInput.Button.SecondaryThumbstick,
        OVRInput.Button.Start,
        OVRInput.Button.Back,
    };

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
            Debug.LogError("[FnafMenuInput] No se encontró ningún ancla de controlador (Left/RightControllerAnchor) para el láser del menú.");
        else
            Debug.Log($"[FnafMenuInput] Punteros listos: left={(leftPointer != null ? leftPointer.name : "-")} right={(rightPointer != null ? rightPointer.name : "-")}");

        if (reticle != null)
            reticleRenderer = reticle.GetComponentInChildren<Renderer>();

        leftLaser = CreateLaser("Left Laser", laserIdleColor);
        rightLaser = CreateLaser("Right Laser", laserIdleColor);
    }

    private void Update()
    {
        if (menu == null)
            return;

        if (menu.IsSplash)
        {
            SetLaserVisible(leftLaser, false);
            SetLaserVisible(rightLaser, false);
            ClearHoveredButton();
            PlaceReticleVisible(false);
            leftTouchingButton = null;
            rightTouchingButton = null;

            if (AnyPhysicalButtonPressed())
            {
                Debug.Log("[FnafMenuInput] Botón físico detectado: saliendo del splash.");
                menu.ShowMainMenu();
            }
            return;
        }

        if (!menu.CanSelect)
        {
            SetLaserVisible(leftLaser, false);
            SetLaserVisible(rightLaser, false);
            ClearHoveredButton();
            PlaceReticleVisible(false);
            return;
        }

        // se recalcula el hover cada frame para que no se quede pegado a un botón
        ClearHoveredButton();

        UpdatePointer(PointerSide.Left, leftPointer, leftLaser, ref leftTouchingButton, ref leftCooldown);
        UpdatePointer(PointerSide.Right, rightPointer, rightLaser, ref rightTouchingButton, ref rightCooldown);

        if (!menu.CanSelect)
            return;

        if (ReadSelectPressed(hoveredPointerSide) && hoveredButton != null && hoveredButton.IsInteractable())
        {
            Debug.Log("[FnafMenuInput] Pulsando botón con el láser: " + hoveredButton.name);
            PressButton(hoveredButton);
        }

        if (allowEditorMouseFallback && !XRSettings.isDeviceActive && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame && hoveredButton != null)
        {
            Debug.Log("[FnafMenuInput] Click de ratón sobre: " + hoveredButton.name);
            PressButton(hoveredButton);
        }
    }

    private void UpdatePointer(PointerSide side, Transform pointer, LineRenderer laser, ref Button touchingButton, ref float cooldown)
    {
        if (pointer == null)
        {
            SetLaserVisible(laser, false);
            return;
        }

        if (cooldown > 0f)
            cooldown -= Time.unscaledDeltaTime;

        Ray ray = new Ray(pointer.position, pointer.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, rayDistance, buttonLayer, QueryTriggerInteraction.Collide);
        Button hitButton = hitSomething ? hit.collider.GetComponentInParent<Button>() : null;
        bool validButton = hitButton != null && hitButton.isActiveAndEnabled && hitButton.IsInteractable();

        // --- Láser visual ---
        if (showLaser)
        {
            SetLaserVisible(laser, true);
            Vector3 end = hitSomething ? hit.point : ray.origin + ray.direction * rayDistance;
            laser.SetPosition(0, ray.origin);
            laser.SetPosition(1, end);
            Color color = validButton ? laserHitColor : laserIdleColor;
            laser.startColor = color;
            laser.endColor = new Color(color.r, color.g, color.b, color.a * 0.35f);
            laser.widthMultiplier = validButton ? laserWidth * 1.6f : laserWidth;
        }
        else
        {
            SetLaserVisible(laser, false);
        }

        // --- Poke: el mando/hand toca físicamente la pantalla ---
        Vector3 pokePoint = pointer.position + pointer.forward * pokeForwardOffset;
        Collider[] overlaps = Physics.OverlapSphere(pokePoint, pokeRadius, buttonLayer, QueryTriggerInteraction.Collide);
        Button pokeButton = null;
        foreach (var col in overlaps)
        {
            var candidate = col.GetComponentInParent<Button>();
            if (candidate != null && candidate.isActiveAndEnabled && candidate.IsInteractable())
            {
                pokeButton = candidate;
                break;
            }
        }

        if (pokeButton != null && pokeButton != touchingButton && cooldown <= 0f)
        {
            cooldown = pokeCooldown;
            Debug.Log($"[FnafMenuInput] Poke ({side}) sobre: {pokeButton.name}");
            PressButton(pokeButton);
        }
        touchingButton = pokeButton;

        // --- Hover (gana el mando que apunta mejor al botón) ---
        float score = float.NegativeInfinity;
        if (validButton && viewCamera != null)
            score = Vector3.Dot(viewCamera.transform.forward, ray.direction) + 2f;
        else if (validButton)
            score = 2f;

        if (validButton && score > lastScore)
        {
            lastScore = score;
            hoveredPointerSide = side;
            SetHoveredButton(hitButton);
            PlaceReticleAt(hit.point, hit.normal, true);
        }
        else if (hoveredButton == null)
        {
            PlaceReticleAt(ray.origin + ray.direction * 1.4f, -ray.direction, false);
        }
    }

    private float lastScore = float.NegativeInfinity;

    private bool AnyPhysicalButtonPressed()
    {
        foreach (var button in PhysicalButtons)
        {
            if (OVRInput.GetDown(button, OVRInput.Controller.LTouch) ||
                OVRInput.GetDown(button, OVRInput.Controller.RTouch))
                return true;
        }

        if (!XRSettings.isDeviceActive)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private bool ReadSelectPressed(PointerSide side)
    {
        bool pressed;
        if (side == PointerSide.Left)
            pressed = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) ||
                      OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch);
        else if (side == PointerSide.Right)
            pressed = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) ||
                      OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
        else
            pressed = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) ||
                      OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

        return pressed;
    }

    private void PressButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.Invoke();
    }

    private LineRenderer CreateLaser(string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.widthMultiplier = laserWidth;
        lr.numCapVertices = 4;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // Sprites/Default multiplica el color de vértice, que es lo que LineRenderer usa para
        // startColor/endColor. URP/Unlit lo ignora y el láser saldría blanco.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader) { name = "Menu Laser Material" };
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        lr.material = mat;

        lr.startColor = color;
        lr.endColor = new Color(color.r, color.g, color.b, color.a * 0.35f);
        return lr;
    }

    private static void SetLaserVisible(LineRenderer laser, bool visible)
    {
        if (laser != null && laser.gameObject.activeSelf != visible)
            laser.gameObject.SetActive(visible);
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
        lastScore = float.NegativeInfinity;
    }

    private void PlaceReticleVisible(bool visible)
    {
        if (reticle == null)
            return;

        if (reticle.gameObject.activeSelf != visible)
            reticle.gameObject.SetActive(visible);
    }

    private void PlaceReticleAt(Vector3 position, Vector3 normal, bool highlighted)
    {
        PlaceReticleVisible(true);
        if (reticle == null)
            return;

        Vector3 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.back;
        reticle.position = position + n * 0.012f;
        reticle.rotation = Quaternion.LookRotation(n);

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
