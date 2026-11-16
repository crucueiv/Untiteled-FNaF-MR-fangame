using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FnafMenuController : MonoBehaviour
{
    public enum MenuScreen
    {
        Splash,
        Main,
        Settings,
        SceneSelect,
        Transition
    }

    [Header("Pantallas")]
    public CanvasGroup splashPanel;
    public CanvasGroup mainMenuPanel;
    public CanvasGroup settingsPanel;

    [Tooltip("Pantalla con la lista de escenas que se pueden jugar.")]
    public CanvasGroup sceneSelectPanel;
    public Image fadeOverlay;
    public TextMeshProUGUI pressPrompt;

    [Header("Botones")]
    public Button startButton;
    public Button settingsButton;
    public Button exitButton;
    public Button backButton;

    [Tooltip("Boton \"Sala de pruebas (oscuridad)\" del selector de escena.")]
    public Button testRoomButton;

    [Tooltip("Boton \"Habitacion MR - Springbonnie\" del selector de escena.")]
    public Button springBonnieButton;

    [Tooltip("Boton \"Volver\" del selector de escena.")]
    public Button sceneSelectBackButton;

    [Header("Escenas de juego")]
    [Tooltip("Ruta de Build Settings de la sala de pruebas a oscuras, sin la extension .unity.")]
    public string testRoomScenePath = "Assets/GameStuff/TestScenes/InitialTestRoom";

    [Tooltip("Ruta de Build Settings de la habitacion MR con Springbonnie, sin la extension .unity.")]
    public string springBonnieScenePath = "Assets/GameStuff/Scenes/MR_SpringBonnieRoom";

    [Tooltip("Escena que se carga si el selector no esta cableado (compatibilidad).")]
    public string gameScenePath = "Assets/GameStuff/TestScenes/InitialTestRoom";

    [Header("Encendido de la pantalla")]
    [Tooltip("Duración del efecto de encendido del monitor al aparecer el menú")]
    public float powerOnDuration = 0.32f;
    [Tooltip("Escala inicial del panel antes de crecer hasta su tamaño final")]
    public float powerOnStartScale = 0.84f;

    private MenuScreen screen = MenuScreen.Splash;
    private Coroutine transitionRoutine;

    public bool IsSplash => screen == MenuScreen.Splash;
    public bool CanSelect => screen == MenuScreen.Main || screen == MenuScreen.Settings || screen == MenuScreen.SceneSelect;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (backButton != null) backButton.onClick.AddListener(BackToMainMenu);
        if (sceneSelectBackButton != null) sceneSelectBackButton.onClick.AddListener(BackToMainMenu);
        if (testRoomButton != null) testRoomButton.onClick.AddListener(SelectTestRoom);
        if (springBonnieButton != null) springBonnieButton.onClick.AddListener(SelectSpringBonnieRoom);

        SetPanel(splashPanel, true);
        SetPanel(mainMenuPanel, false);
        SetPanel(settingsPanel, false);
        SetPanel(sceneSelectPanel, false);
        SetFadeAlpha(0f);
        screen = MenuScreen.Splash;
    }

    private void Update()
    {
        if (IsSplash && pressPrompt != null)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * 3.45f) + 1f) * 0.5f;
            pressPrompt.alpha = Mathf.Lerp(0.58f, 1f, pulse);
        }
    }

    public void ShowMainMenu()
    {
        if (screen != MenuScreen.Splash)
            return;

        Debug.Log("[FnafMenuController] Encendiendo la pantalla del menú principal.");
        BeginTransition(mainMenuPanel, MenuScreen.Main);
    }

    public void OpenSettings()
    {
        if (screen != MenuScreen.Main)
            return;

        // Ajustes todavía no tiene pantalla propia: por ahora solo avisa.
        Debug.Log("[FnafMenuController] Ajustes: todavía no implementado (no lleva a ninguna pantalla).");
    }

    public void BackToMainMenu()
    {
        if (screen != MenuScreen.Settings && screen != MenuScreen.SceneSelect)
            return;

        BeginTransition(mainMenuPanel, MenuScreen.Main);
    }

    /// <summary>
    /// "Iniciar" del menu principal: en vez de cargar directamente, abre el selector de escena.
    /// </summary>
    public void StartGame()
    {
        if (screen != MenuScreen.Main || transitionRoutine != null)
            return;

        if (sceneSelectPanel == null)
        {
            // Sin selector cableado se mantiene el comportamiento anterior.
            Debug.Log("[FnafMenuController] Jugar -> " + gameScenePath);
            transitionRoutine = StartCoroutine(LoadGame(gameScenePath));
            return;
        }

        Debug.Log("[FnafMenuController] Jugar -> selector de escena.");
        BeginTransition(sceneSelectPanel, MenuScreen.SceneSelect);
    }

    /// <summary>Boton "Sala de pruebas (oscuridad)".</summary>
    public void SelectTestRoom()
    {
        SelectScene(testRoomScenePath);
    }

    /// <summary>Boton "Habitacion MR - Springbonnie".</summary>
    public void SelectSpringBonnieRoom()
    {
        SelectScene(springBonnieScenePath);
    }

    /// <summary>Carga la escena indicada desde el selector.</summary>
    public void SelectScene(string scenePath)
    {
        if (screen != MenuScreen.SceneSelect || transitionRoutine != null)
            return;

        Debug.Log("[FnafMenuController] Selector -> " + scenePath);
        transitionRoutine = StartCoroutine(LoadGame(scenePath));
    }

    public void ExitGame()
    {
        if (screen != MenuScreen.Main || transitionRoutine != null)
            return;

        Debug.Log("[FnafMenuController] Salir del juego.");
        transitionRoutine = StartCoroutine(QuitGame());
    }

    private void BeginTransition(CanvasGroup target, MenuScreen targetScreen)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionTo(target, targetScreen));
    }

    private IEnumerator TransitionTo(CanvasGroup target, MenuScreen targetScreen)
    {
        screen = MenuScreen.Transition;
        SetAllPanelsInteractable(false);
        yield return FadeOverlay(0f, 1f, 0.16f);

        SetPanel(splashPanel, target == splashPanel);
        SetPanel(mainMenuPanel, target == mainMenuPanel);
        SetPanel(settingsPanel, target == settingsPanel);
        SetPanel(sceneSelectPanel, target == sceneSelectPanel);

        if (target == mainMenuPanel)
            yield return PowerOn(mainMenuPanel);

        yield return FadeOverlay(1f, 0f, 0.2f);
        screen = targetScreen;
        transitionRoutine = null;
    }

    /// <summary>
    /// Efecto "se enciende el monitor": el panel crece ligeramente mientras aparece.
    /// </summary>
    private IEnumerator PowerOn(CanvasGroup panel)
    {
        if (panel == null)
            yield break;

        Transform tr = panel.transform;
        Vector3 finalScale = Vector3.one;
        float elapsed = 0f;

        while (elapsed < powerOnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / powerOnDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            panel.alpha = eased;
            tr.localScale = Vector3.one * Mathf.Lerp(powerOnStartScale, 1f, eased);
            yield return null;
        }

        panel.alpha = 1f;
        tr.localScale = finalScale;
    }

    private IEnumerator LoadGame(string configuredPath)
    {
        screen = MenuScreen.Transition;
        SetAllPanelsInteractable(false);
        yield return FadeOverlay(0f, 1f, 0.24f);

        // Application.CanStreamedLevelBeLoaded solo acepta la ruta exacta del Build Settings
        // (con ".unity") o el nombre de la escena; se prueban ambas variantes.
        string scenePath = ResolveScenePath(configuredPath);

        if (scenePath == null)
        {
            Debug.LogError("No se puede cargar la escena de juego. Comprueba que esté habilitada en Build Settings: " + configuredPath);

            // Se vuelve al selector para poder elegir otra escena.
            bool haySelector = sceneSelectPanel != null;
            SetPanel(sceneSelectPanel, haySelector);
            SetPanel(mainMenuPanel, !haySelector);
            SetPanel(splashPanel, false);
            SetPanel(settingsPanel, false);
            yield return FadeOverlay(1f, 0f, 0.18f);
            screen = haySelector ? MenuScreen.SceneSelect : MenuScreen.Main;
            transitionRoutine = null;
            yield break;
        }

        AsyncOperation load = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
        if (load == null)
        {
            Debug.LogError("Unity no pudo iniciar la carga de la escena: " + scenePath);
            screen = MenuScreen.Main;
            transitionRoutine = null;
            yield break;
        }

        while (!load.isDone)
            yield return null;
    }

    /// <summary>
    /// Devuelve una ruta que Unity puede cargar, o null si la escena no está en Build Settings.
    /// </summary>
    private static string ResolveScenePath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return null;

        string withExtension = configuredPath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)
            ? configuredPath
            : configuredPath + ".unity";

        if (Application.CanStreamedLevelBeLoaded(withExtension))
            return withExtension;

        if (Application.CanStreamedLevelBeLoaded(configuredPath))
            return configuredPath;

        return null;
    }

    private IEnumerator QuitGame()
    {
        screen = MenuScreen.Transition;
        SetAllPanelsInteractable(false);
        yield return FadeOverlay(0f, 1f, 0.2f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeOverlay == null)
            yield break;

        float elapsed = 0f;
        SetFadeAlpha(from);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private void SetPanel(CanvasGroup panel, bool visible)
    {
        if (panel == null)
            return;

        panel.gameObject.SetActive(visible);
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }

    private void SetAllPanelsInteractable(bool interactable)
    {
        SetInteractable(splashPanel, interactable);
        SetInteractable(mainMenuPanel, interactable);
        SetInteractable(settingsPanel, interactable);
        SetInteractable(sceneSelectPanel, interactable);
    }

    private static void SetInteractable(CanvasGroup panel, bool interactable)
    {
        if (panel == null)
            return;

        panel.interactable = interactable;
        panel.blocksRaycasts = interactable;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null)
            return;

        Color color = fadeOverlay.color;
        color.a = alpha;
        fadeOverlay.color = color;
    }
}
