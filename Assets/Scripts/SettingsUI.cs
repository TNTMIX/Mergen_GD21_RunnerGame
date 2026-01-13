using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
public class SettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button backButton;
    [SerializeField] private Button exitButton;
    // Track what was selected before opening settings so we can restore it on close
    private GameObject previousSelected;
    [SerializeField] private bool pauseGameOnOpen = true;
    [SerializeField] private bool pauseAudioOnOpen = false;
    private float previousTimeScale = 1f;
    private const string VolumeKey = "MasterVolume";
    private void Start()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("SettingsUI: 'settingsPanel' is not assigned in the inspector.");
            // Try to auto-assign to the current GameObject if it looks like a panel
            if (gameObject.name.ToLower().Contains("settings") || gameObject.GetComponent<Canvas>() != null)
            {
                settingsPanel = gameObject;
                Debug.Log("SettingsUI: auto-assigned 'settingsPanel' to the GameObject the script is on.");
            }
        }
        if (volumeSlider == null)
        {
            Debug.LogWarning("SettingsUI: 'volumeSlider' is not assigned in the inspector.");
            // Try to find a Slider in children
            var found = GetComponentInChildren<Slider>(true);
            if (found != null)
            {
                volumeSlider = found;
                Debug.Log("SettingsUI: auto-assigned 'volumeSlider' from children.");
            }
        }

        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        Debug.Log($"SettingsUI.Start: savedVolume={savedVolume}, sliderAssigned={volumeSlider != null}, panelAssigned={settingsPanel != null}");
        AudioListener.volume = savedVolume;

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Wire Back button if assigned
        if (backButton != null)
        {
            backButton.onClick.AddListener(CloseSettings);
        }

        // Auto-assign Exit button if possible (helps if inspector isn't wired)
        if (exitButton == null)
        {
            var candidateButtons = settingsPanel != null ? settingsPanel.GetComponentsInChildren<Button>(true) : GetComponentsInChildren<Button>(true);
            foreach (var b in candidateButtons)
            {
                var name = b.name.ToLower();
                if (name.Contains("exit") || name.Contains("close") || name.Contains("menu"))
                {
                    exitButton = b;
                    Debug.Log($"SettingsUI: Auto-assigned 'exitButton' to '{b.name}'.");
                    break;
                }
            }
        }

        // Wire Exit button if assigned (delegates to UIManager.OnExitClicked)
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitFromSettings);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Make runtime fixes if UI won't respond
        // Ensure there's an EventSystem so UI receives input
        if (EventSystem.current == null)
        {
            Debug.LogWarning("SettingsUI: No EventSystem found in scene — creating a default one.");
            var esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            // Note: if using the new Input System package, replace StandaloneInputModule with
            // InputSystemUIInputModule in the Editor. Creating the fallback helps pointer input work.
        }

        // Ensure the slider is interactable and has a handle assigned
        if (volumeSlider != null)
        {
            if (!volumeSlider.interactable)
            {
                volumeSlider.interactable = true;
                Debug.Log("SettingsUI: Slider was not interactable — set interactable = true.");
            }
            if (volumeSlider.handleRect == null)
            {
                // Try to find a child RectTransform named like 'handle'
                var rects = volumeSlider.GetComponentsInChildren<RectTransform>(true);
                RectTransform found = null;
                foreach (var r in rects)
                {
                    if (r.name.ToLower().Contains("handle"))
                    {
                        found = r;
                        break;
                    }
                }
                if (found != null)
                {
                    volumeSlider.handleRect = found;
                    Debug.Log("SettingsUI: Auto-assigned slider.handleRect from child named 'handle'.");
                }
                else
                {
                    Debug.LogWarning("SettingsUI: slider.handleRect is null and no child named 'handle' was found. The slider may not be draggable.");
                }
            }

            // Ensure parent Canvas has a GraphicRaycaster so the slider can receive pointer events
            var parentCanvas = volumeSlider.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                var gr = parentCanvas.GetComponent<GraphicRaycaster>();
                if (gr == null)
                {
                    parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("SettingsUI: Added missing GraphicRaycaster to parent Canvas.");
                }
            }

            // Check for CanvasGroup blocking raycasts on the slider or parents
            Transform t = volumeSlider.transform;
            while (t != null)
            {
                var cg = t.GetComponent<CanvasGroup>();
                if (cg != null && !cg.blocksRaycasts)
                {
                    Debug.LogWarning($"SettingsUI: CanvasGroup on '{t.name}' has blocksRaycasts=false — this can block slider interaction.");
                }
                t = t.parent;
            }
        }
    }
    public void OpenSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("SettingsUI.OpenSettings called but 'settingsPanel' is null.");
            return;
        }
        // Don't allow opening settings if the game is over
        var UIManager = Object.FindFirstObjectByType<UIManager>();
        if (UIManager != null && UIManager.IsGameOver)
        {
            Debug.Log("SettingsUI: Cannot open settings when the game is over.");
            return;
        }
        // Remember current selection so we can restore it when closing
        if (EventSystem.current != null)
        {
            previousSelected = EventSystem.current.currentSelectedGameObject;
        }

        settingsPanel.SetActive(true);
        Debug.Log("SettingsUI: OpenSettings called — panel opened.");

        // Set initial selected object to the slider (for keyboard/controller navigation)
        if (EventSystem.current != null && volumeSlider != null)
        {
            EventSystem.current.SetSelectedGameObject(volumeSlider.gameObject);
        }

        // Pause the game if requested
        if (pauseGameOnOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Debug.Log($"SettingsUI: Game paused (previous timescale={previousTimeScale}).");
        }
        if (pauseAudioOnOpen)
        {
            AudioListener.pause = true;
            Debug.Log("SettingsUI: Audio paused.");
        }
    }
    public void CloseSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("SettingsUI.CloseSettings called but 'settingsPanel' is null.");
            return;
        }
        settingsPanel.SetActive(false);
        Debug.Log("SettingsUI: CloseSettings called — panel closed.");

        // Restore previous selection (useful for controller/keyboard navigation)
        if (EventSystem.current != null && previousSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(previousSelected);
            previousSelected = null;
        }
        // Resume the game if it was paused by this panel
        if (pauseGameOnOpen)
        {
            Time.timeScale = previousTimeScale;
            Debug.Log($"SettingsUI: Game resumed (timescale restored to {previousTimeScale}).");
        }
        if (pauseAudioOnOpen)
        {
            AudioListener.pause = false;
            Debug.Log("SettingsUI: Audio resumed.");
        }
    }
    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
        Debug.Log($"SettingsUI: OnVolumeChanged -> {value}");
    }

    // Called when the Exit button in the Settings panel is clicked. This mirrors the lose-panel exit behavior.
    public void OnExitFromSettings()
    {
        Debug.Log("SettingsUI: Exit clicked — returning to Main Menu.");
        // Close settings first to restore time state if this panel paused the game
        CloseSettings();

        var uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.OnExitClicked();
        }
        else
        {
            Debug.LogWarning("SettingsUI: No UIManager found to perform exit to Main Menu.");
        }
    }

    private void OnValidate()
    {
        // Help catch missing assignments in the editor
        if (volumeSlider == null)
        {
            var found = GetComponentInChildren<Slider>(true);
            if (found != null)
            {
                volumeSlider = found;
            }
        }
        if (settingsPanel == null && (gameObject.name.ToLower().Contains("settings") || gameObject.GetComponent<Canvas>() != null))
        {
            settingsPanel = gameObject;
        }
    }
}