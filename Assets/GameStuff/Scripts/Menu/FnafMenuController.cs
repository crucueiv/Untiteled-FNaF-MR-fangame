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
        Transition
    }

    [Header("Pantallas")]
    public CanvasGroup splashPanel;
    public CanvasGroup mainMenuPanel;
    public CanvasGroup settingsPanel;
    public Image fadeOverlay;
    public TextMeshProUGUI pressPrompt;

    [Header("Botones")]
    public Button startButton;
    public Button settingsButton;
    public Button exitButton;
    public Button backButton;

    [Header("Escena de juego")]
    [Tooltip("Ruta de Build Settings sin la extensión .unity.")]
    public string gameScenePath = "Assets/GameStuff/TestScenes/InitialTestRoom";

    private MenuScreen screen = MenuScreen.Splash;
    private Coroutine transitionRoutine;

    public bool IsSplash => screen == MenuScreen.Splash;
    public bool CanSelect => screen == MenuScreen.Main || screen == MenuScreen.Settings;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (backButton != null) backButton.onClick.AddListener(BackToMainMenu);

        SetPanel(splashPanel, true);
        SetPanel(mainMenuPanel, false);
        SetPanel(settingsPanel, false);
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

        BeginTransition(mainMenuPanel, MenuScreen.Main);
    }

    public void OpenSettings()
    {
        if (screen != MenuScreen.Main)
            return;

        BeginTransition(settingsPanel, MenuScreen.Settings);
    }

    public void BackToMainMenu()
    {
        if (screen != MenuScreen.Settings)
            return;

        BeginTransition(mainMenuPanel, MenuScreen.Main);
    }

    public void StartGame()
    {
        if (screen != MenuScreen.Main || transitionRoutine != null)
            return;

        transitionRoutine = StartCoroutine(LoadGame());
    }

    public void ExitGame()
    {
        if (screen != MenuScreen.Main || transitionRoutine != null)
            return;

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

        yield return FadeOverlay(1f, 0f, 0.2f);
        screen = targetScreen;
        transitionRoutine = null;
    }

    private IEnumerator LoadGame()
    {
        screen = MenuScreen.Transition;
        SetAllPanelsInteractable(false);
        yield return FadeOverlay(0f, 1f, 0.24f);

        if (!Application.CanStreamedLevelBeLoaded(gameScenePath))
        {
            Debug.LogError("No se puede cargar la escena de juego. Comprueba que esté habilitada en Build Settings: " + gameScenePath);
            SetPanel(mainMenuPanel, true);
            SetPanel(splashPanel, false);
            SetPanel(settingsPanel, false);
            yield return FadeOverlay(1f, 0f, 0.18f);
            screen = MenuScreen.Main;
            transitionRoutine = null;
            yield break;
        }

        AsyncOperation load = SceneManager.LoadSceneAsync(gameScenePath, LoadSceneMode.Single);
        if (load == null)
        {
            Debug.LogError("Unity no pudo iniciar la carga de la escena: " + gameScenePath);
            screen = MenuScreen.Main;
            transitionRoutine = null;
            yield break;
        }

        while (!load.isDone)
            yield return null;
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
