using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject losePanel;
    // Track whether the game is actually over. The lose panel will only open when this is true.
    private bool isGameOver = false;

    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button startButton;

    // Optional: Quit button shown on the main menu — assigned automatically if not set in the inspector
    [SerializeField] private Button mainMenuQuitButton;

    [Header("Optional UI Targets")]
    [Tooltip("If set, ScoreBoard will be reparented into this GameObject when gameplay starts.")]
    [SerializeField] private GameObject gameplayUIRoot;

    // Track score UI reparenting so we can restore it when returning to the menu
    private Transform scoreOriginalParent = null;
    private GameObject scoreReparented = null;
    // If true, the gameplay duplicate was created at runtime by this script and should be destroyed on Exit.
    // If false and `scoreReparented` refers to a pre-existing editor object, we will deactivate it instead.
    private bool scoreReparentedCreatedByScript = false;

    // Tags used for cleanup when returning to menu. Leave pickupTag empty to skip pickup cleanup.
    [SerializeField] private string obstacleTag = "Obstacle";
    [SerializeField] private string pickupTag = "";

    // How long to wait (in real time seconds) before showing the lose panel — allows death animation to finish
    [SerializeField] private float losePanelDelay = 0.5f;

    // Public read-only accessor so other systems (like Settings) can know the game-over state
    public bool IsGameOver => isGameOver;

    // Track whether gameplay has started (main menu Start button pressed)
    private bool isGameStarted = false;
    public bool IsGameStarted => isGameStarted;

    // If true, the next scene loaded should start immediately (used by Restart)
    private static bool autoStartNextScene = false;

    [SerializeField] private float autoStartDelay = 0.05f;

    private void Start()
    {
        // Auto-find and wire the Restart button if the inspector field isn't assigned.
        if (restartButton == null)
            FindAndAssignRestartButton();

        if (restartButton != null)
        {
            // Ensure our listener is present exactly once
            restartButton.onClick.RemoveListener(OnRestartClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        // Auto-find and wire the Exit button if the inspector field isn't assigned.
        if (exitButton == null)
            FindAndAssignExitButton();

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitClicked);
            exitButton.onClick.AddListener(OnExitClicked);
        }

        // Auto-find and wire the Start button (main menu). If present, hook it up to begin gameplay.
        if (startButton == null)
            FindAndAssignStartButton();

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);
        }

        // Auto-find and wire the Main Menu Quit button (if inspector isn't assigned). This should quit the application.
        if (mainMenuQuitButton == null)
            FindAndAssignMainMenuQuitButton();

        if (mainMenuQuitButton != null)
        {
            mainMenuQuitButton.onClick.RemoveListener(OnMainMenuClicked);
            mainMenuQuitButton.onClick.AddListener(OnMainMenuClicked);
            Debug.Log($"UIManager: Auto-wired 'mainMenuQuitButton' to '{mainMenuQuitButton.name}'.");
        }

        // Default to not started: pause gameplay elements that rely on this flag
        isGameStarted = false;
        Time.timeScale = 0f; // keep the game paused until Start is clicked

        // Ensure ScoreManager exists so scoring works even if the component wasn't manually added to the scene
        if (Object.FindFirstObjectByType<ScoreManager>() == null)
        {
            var smGo = new GameObject("ScoreManager");
            smGo.AddComponent<ScoreManager>();
            Debug.Log("UIManager: Created missing ScoreManager at scene start.");
        }

        // Ensure ScoreBoard is visible at Main Menu start and that ScoreManager is wired to it
        var initialScoreGO = GameObject.Find("ScoreBoard") ?? GameObject.Find("ScoreText");
        if (initialScoreGO != null)
        {
            initialScoreGO.SetActive(true);
            var smInit = Object.FindFirstObjectByType<ScoreManager>();
            if (smInit != null)
                smInit.FindAndAssignText();
            Debug.Log($"UIManager: Ensured initial ScoreBoard ('{initialScoreGO.name}') is active at scene start.");
        }

        // Ensure Start button label is visible
        if (startButton != null)
        {
            var startText = startButton.GetComponentInChildren<TMP_Text>(true);
            if (startText != null)
                startText.gameObject.SetActive(true);
        }

        // If Restart requested immediate start of the scene, do it now and clear the flag
        if (autoStartNextScene)
        {
            // Run start slightly later to ensure all Start() methods finish and objects are initialized
            StartCoroutine(AutoStartAfterDelay());
        }
    }

    private IEnumerator AutoStartAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoStartDelay);
        autoStartNextScene = false;
        Debug.Log("UIManager: autoStartNextScene detected — starting gameplay automatically after restart (delayed).");
        OnStartClicked();
    }

    private void FindAndAssignRestartButton()
    {
        if (losePanel == null)
            return;

        var buttons = losePanel.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            var name = b.name.ToLower();
            if (name.Contains("restart") || name.Contains("retry"))
            {
                restartButton = b;
                Debug.Log($"UIManager: Auto-assigned 'restartButton' to '{b.name}'.");
                return;
            }
        }

        // Fallback: use the first button found under the panel
        if (buttons.Length > 0)
        {
            restartButton = buttons[0];
            Debug.Log($"UIManager: Auto-assigned 'restartButton' to '{restartButton.name}' (fallback).");
        }
    }

    private void FindAndAssignExitButton()
    {
        if (losePanel == null)
            return;

        var buttons = losePanel.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            var name = b.name.ToLower();
            if (name.Contains("exit") || name.Contains("quit"))
            {
                exitButton = b;
                Debug.Log($"UIManager: Auto-assigned 'exitButton' to '{b.name}'.");
                return;
            }
        }

        // Try a 'close' match
        foreach (var b in buttons)
        {
            var name = b.name.ToLower();
            if (name.Contains("close"))
            {
                exitButton = b;
                Debug.Log($"UIManager: Auto-assigned 'exitButton' to '{b.name}' (close match).");
                return;
            }
        }

        // Last-resort fallback: pick the first button that's not the restart button
        foreach (var b in buttons)
        {
            if (b != restartButton)
            {
                exitButton = b;
                Debug.Log($"UIManager: Auto-assigned 'exitButton' to '{b.name}' (fallback).");
                return;
            }
        }
    }

    private void FindAndAssignStartButton()
    {
        // Look through all UI buttons in the scene (not only lose panel) to find the main Start button
        var allButtons = Object.FindObjectsOfType<Button>();
        foreach (var b in allButtons)
        {
            var name = b.name.ToLower();
            if (name.Contains("start") || name.Contains("play"))
            {
                startButton = b;
                Debug.Log($"UIManager: Auto-assigned 'startButton' to '{b.name}'.");
                return;
            }
        }

        // Fallback: try to find GameObject named MainMenu
        var mainMenu = GameObject.Find("MainMenu") ?? GameObject.Find("Main Menu");
        if (mainMenu != null)
        {
            var found = mainMenu.GetComponentInChildren<Button>(true);
            if (found != null)
            {
                startButton = found;
                Debug.Log($"UIManager: Auto-assigned 'startbutton' to '{found.name}' from MainMenu.");
                return;
            }
        }
    }

    // Find the quit button used in the Main Menu and assign it to OnMainMenuClicked. This purposely avoids buttons under the lose panel
    private void FindAndAssignMainMenuQuitButton()
    {
        var allButtons = Object.FindObjectsOfType<Button>();
        foreach (var b in allButtons)
        {
            var name = b.name.ToLower();
            if (name.Contains("quit") || name.Contains("exit"))
            {
                // Skip if this button is part of the lose panel (we handle that separately with exitButton)
                if (losePanel != null && b.transform.IsChildOf(losePanel.transform))
                    continue;

                mainMenuQuitButton = b;
                Debug.Log($"UIManager: Auto-assigned 'mainMenuQuitButton' to '{b.name}'.");
                return;
            }
        }

        // Fallback: look for a button inside a GameObject called 'MainMenu' that contains 'quit'/'exit'
        var mainMenu = GameObject.Find("MainMenu") ?? GameObject.Find("Main Menu");
        if (mainMenu != null)
        {
            var buttons = mainMenu.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                var name = b.name.ToLower();
                if (name.Contains("quit") || name.Contains("exit"))
                {
                    mainMenuQuitButton = b;
                    Debug.Log($"UIManager: Auto-assigned 'mainMenuQuitButton' to '{b.name}' from MainMenu.");
                    return;
                }
            }
        }
    }

    public void OnStartClicked()
    {
        if (isGameStarted)
            return;

        isGameStarted = true;

        // As an early fallback, attempt to duplicate ScoreBoard into gameplay UI even if we can't find a menu panel yet
        if (scoreReparented == null)
        {
            var earlyCandidate = GameObject.Find("ScoreBoard") ?? GameObject.Find("ScoreText");
            if (earlyCandidate == null)
            {
                var anyTmp = Object.FindFirstObjectByType<TMP_Text>(UnityEngine.FindObjectsInactive.Exclude);
                if (anyTmp != null)
                    earlyCandidate = anyTmp.gameObject;
            }
            if (earlyCandidate != null)
            {
                if (Object.FindFirstObjectByType<ScoreManager>() == null)
                {
                    var smGo = new GameObject("ScoreManager");
                    smGo.AddComponent<ScoreManager>();
                    Debug.Log("UIManager: Created missing ScoreManager before early duplication.");
                }
                GameObject target = gameplayUIRoot != null ? gameplayUIRoot : (FindGameplayCanvas()?.gameObject);
                if (target != null)
                {
                    // If a gameplay copy already exists in the scene (created by the designer), reuse it instead of instantiating
                    var existing = GameObject.Find(earlyCandidate.name + "_Gameplay");
                    if (existing != null)
                    {
                        existing.SetActive(true);
                        scoreReparented = existing;
                        scoreReparentedCreatedByScript = false;
                        Debug.Log($"UIManager: Reused existing gameplay '{existing.name}' instead of creating a new duplicate.");
                        var sm = Object.FindFirstObjectByType<ScoreManager>();
                        if (sm != null)
                        {
                            var tmpText = existing.GetComponentInChildren<TMP_Text>(true);
                            if (tmpText != null) sm.SetTextComponent(tmpText);
                            else sm.FindAndAssignText();
                        }
                    }
                    else
                    {
                        var duplicate = Instantiate(earlyCandidate, target.transform, false);
                        duplicate.name = earlyCandidate.name + "_Gameplay";
                        duplicate.SetActive(true);
                        scoreReparented = duplicate;
                        scoreReparentedCreatedByScript = true;
                        Debug.Log($"UIManager: Early duplicated '{earlyCandidate.name}' into '{target.name}' as '{duplicate.name}'.");
                        var sm = Object.FindFirstObjectByType<ScoreManager>();
                        if (sm != null)
                        {
                            var tmpText = duplicate.GetComponentInChildren<TMP_Text>(true);
                            if (tmpText != null) sm.SetTextComponent(tmpText);
                            else sm.FindAndAssignText();
                        }
                    }
                }
            }
        }

        // Hide main menu (try to find a parent menu of the start button)
        GameObject panelRoot = null;
        if (startButton != null)
        {
            Transform t = startButton.transform;
            // look up for a parent whose name contains 'menu' or 'main'
            Transform panelToHide = null;
            while (t != null)
            {
                var n = t.name.ToLower();
                if (n.Contains("menu") || n.Contains("main"))
                {
                    panelToHide = t;
                    break;
                }
                t = t.parent;
            }
            if (panelToHide != null)
            {
                panelRoot = panelToHide.gameObject;
                // Before hiding, ensure a ScoreManager exists and create a gameplay copy of the ScoreBoard so the Main Menu original remains visible
                if (Object.FindFirstObjectByType<ScoreManager>() == null)
                {
                    var smGo = new GameObject("ScoreManager");
                    smGo.AddComponent<ScoreManager>();
                    Debug.Log("UIManager: Created missing ScoreManager before Start (dup path).");
                }
                var scoreCandidate = FindScoreObjectIn(panelRoot) ?? GameObject.Find("ScoreBoard") ?? GameObject.Find("ScoreText");
                if (scoreCandidate != null && scoreReparented == null)
                {
                    GameObject target = gameplayUIRoot != null ? gameplayUIRoot : (FindGameplayCanvas()?.gameObject);
                    if (target != null)
                    {
                        // If a designer-provided gameplay copy exists, reuse it; otherwise create a new one
                        var existing = GameObject.Find(scoreCandidate.name + "_Gameplay");
                        if (existing != null)
                        {
                            existing.SetActive(true);
                            scoreReparented = existing;
                            scoreReparentedCreatedByScript = false;
                            Debug.Log($"UIManager: Reused existing gameplay '{existing.name}' instead of creating a new duplicate.");
                            var sm = Object.FindFirstObjectByType<ScoreManager>();
                            if (sm != null)
                            {
                                var tmpText = existing.GetComponentInChildren<TMP_Text>(true);
                                if (tmpText != null) sm.SetTextComponent(tmpText);
                                else sm.FindAndAssignText();
                            }
                        }
                        else
                        {
                            var duplicate = Instantiate(scoreCandidate, target.transform, false);
                            duplicate.name = scoreCandidate.name + "_Gameplay";
                            duplicate.SetActive(true);
                            scoreReparented = duplicate;
                            scoreReparentedCreatedByScript = true;
                            Debug.Log($"UIManager: Duplicated '{scoreCandidate.name}' into '{target.name}' as '{duplicate.name}' so it remains during gameplay.");

                            var sm = Object.FindFirstObjectByType<ScoreManager>();
                            if (sm != null)
                            {
                                var tmpText = duplicate.GetComponentInChildren<TMP_Text>(true);
                                if (tmpText != null)
                                    sm.SetTextComponent(tmpText);
                                else
                                    sm.FindAndAssignText();
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("UIManager: No gameplay UI target found to duplicate ScoreBoard into. ScoreBoard may be hidden when menu closes.");
                    }
                }

                panelToHide.gameObject.SetActive(false);
            }
            else
            {
                var mainMenu = GameObject.Find("MainMenu") ?? GameObject.Find("Main Menu");
                if (mainMenu != null)
                {
                    // handle same as above if we found a main menu root by name
                    // Ensure ScoreManager exists before creating the gameplay copy
                    if (Object.FindFirstObjectByType<ScoreManager>() == null)
                    {
                        var smGo = new GameObject("ScoreManager");
                        smGo.AddComponent<ScoreManager>();
                        Debug.Log("UIManager: Created missing ScoreManager before duplicating from mainMenu.");
                    }

                    var scoreCandidate = FindScoreObjectIn(mainMenu) ?? GameObject.Find("ScoreBoard") ?? GameObject.Find("ScoreText");
                    if (scoreCandidate != null && scoreReparented == null)
                    {
                        GameObject target = gameplayUIRoot != null ? gameplayUIRoot : (FindGameplayCanvas()?.gameObject);
                        if (target != null)
                        {
                            // If a designer-provided gameplay copy exists, reuse it; otherwise create a new one
                            var existing = GameObject.Find(scoreCandidate.name + "_Gameplay");
                            if (existing != null)
                            {
                                existing.SetActive(true);
                                scoreReparented = existing;
                                scoreReparentedCreatedByScript = false;
                                Debug.Log($"UIManager: Reused existing gameplay '{existing.name}' instead of creating a new duplicate.");
                                var sm = Object.FindFirstObjectByType<ScoreManager>();
                                if (sm != null)
                                {
                                    var tmpText = existing.GetComponentInChildren<TMP_Text>(true);
                                    if (tmpText != null) sm.SetTextComponent(tmpText);
                                    else sm.FindAndAssignText();
                                }
                            }
                            else
                            {
                                var duplicate = Instantiate(scoreCandidate, target.transform, false);
                                duplicate.name = scoreCandidate.name + "_Gameplay";
                                duplicate.SetActive(true);
                                scoreReparented = duplicate;
                                scoreReparentedCreatedByScript = true;
                                Debug.Log($"UIManager: Duplicated '{scoreCandidate.name}' into '{target.name}' as '{duplicate.name}' so it remains during gameplay.");

                                var sm = Object.FindFirstObjectByType<ScoreManager>();
                                if (sm != null)
                                {
                                    var tmpText = duplicate.GetComponentInChildren<TMP_Text>(true);
                                    if (tmpText != null)
                                        sm.SetTextComponent(tmpText);
                                    else
                                        sm.FindAndAssignText();
                                }
                            }
                        }
                    }

                    mainMenu.SetActive(false);
                    panelRoot = mainMenu;
                }
                else
                {
                    // As a last resort, hide the start button itself
                    startButton.gameObject.SetActive(false);
                }
            }

            // Ensure start button text is hidden so no stray 'Start' label remains on screen
            var startText = startButton.GetComponentInChildren<TMP_Text>(true);
            if (startText != null)
                startText.gameObject.SetActive(false);
        }

        // Resume time and tell spawner to begin
        Time.timeScale = 1f;
        var spawner = Object.FindFirstObjectByType<SpawnManager>();
        Debug.Log($"UIManager: OnStartClicked -> isGameStarted={isGameStarted}, timeScale={Time.timeScale}, spawnerFound={(spawner!=null)}");
        if (spawner != null)
        {
            spawner.StartSpawning();
            Debug.Log("UIManager: OnStartClicked -> StartSpawning called.");
        }

        Debug.Log("UIManager: OnStartClicked — gameplay started.");
    }

    // Call this from your GameManager when the player loses.
    public void SetGameOver(bool value)
    {
        isGameOver = value;
        if (isGameOver)
        {
            // Make sure Settings is closed when the game ends so it doesn't conflict
            var settings = Object.FindFirstObjectByType<SettingsUI>();
            if (settings != null)
                settings.CloseSettings();

            // Stop spawning obstacles so nothing else appears after death
            var spawner = Object.FindFirstObjectByType<SpawnManager>();
            if (spawner != null)
                spawner.StopSpawning();

            // Start the delayed show so the death animation can play without being frozen by Time.timeScale=0
            StopAllCoroutines();
            StartCoroutine(ShowLoseAfterDelay());
        }
    }

    // Helper: find a GameObject that looks like the score UI under the given root (searches for 'score' or TMP text)
    private GameObject FindScoreObjectIn(GameObject root)
    {
        if (root == null) return null;
        // Look for a child with 'score' in its name or any TMP_Text component
        var transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t.name.ToLower().Contains("score"))
                return t.gameObject;
            var tmp = t.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                return tmp.gameObject;
        }
        return null;
    }

    // Helper: Find a Canvas suitable for gameplay UI. Prefer a named 'GameplayUI' or an active Canvas in scene.
    private Canvas FindGameplayCanvas()
    {
        // Look for a GameObject named 'GameplayUI' or 'UIRoot' that has a Canvas
        var candidate = GameObject.Find("GameplayUI") ?? GameObject.Find("UIRoot") ?? GameObject.Find("GameUI");
        if (candidate != null)
        {
            var c = candidate.GetComponentInChildren<Canvas>(true);
            if (c != null)
                return c;
        }

        // Fallback: find any active Canvas in the scene (prefer root-level)
        var canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.isActiveAndEnabled)
                return c;
        }

        // As a last resort, return the first canvas found
        if (canvases.Length > 0)
            return canvases[0];

        return null;
    }

    private IEnumerator ShowLoseAfterDelay()
    {
        // Wait in real time so the delay isn't affected by Time.timeScale
        yield return new WaitForSecondsRealtime(losePanelDelay);
        ShowLose();
    }

public void ShowLose(bool pauseGame = true)
    {
        if (!isGameOver)
        {
            Debug.LogWarning("UIManager.ShowLose called but isGameOver is false. Call SetGameOver(true) to signal game over.");
            return;
        }

        if (losePanel != null)
            losePanel.SetActive(true);

        if (pauseGame)
            Time.timeScale = 0f;
    }
public void OnRestartClicked()
    {
        // Restore time and audio in case they were paused
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Set flag so the newly loaded scene will auto-start (avoids showing Main Menu after reload)
        autoStartNextScene = true;

        // Reload the current scene
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }
public void OnMainMenuClicked()
    {
    // Exit the game. In the editor stop play mode, in builds quit the application.
    Time.timeScale = 1f;
#if UNITY_EDITOR
    EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    // Called by the Exit button on the lose panel. Show the main menu and reset game state instead of quitting.
    public void OnExitClicked()
    {
        // Restore audio
        AudioListener.pause = false;

        // Stop spawning and reset gameplay flags
        var spawner = Object.FindFirstObjectByType<SpawnManager>();
        if (spawner != null)
            spawner.StopSpawning();

        // Hide lose panel
        if (losePanel != null)
            losePanel.SetActive(false);

        // Reset flags so the Start button can be used again
        isGameStarted = false;
        isGameOver = false;

        // Pause the game while on the menu
        Time.timeScale = 0f;

        // Show main menu UI if present (search scene roots including inactive objects)
        var mainMenu = FindMainMenuObject() ?? GameObject.Find("MainMenu") ?? GameObject.Find("Main Menu");
        if (mainMenu != null)
        {
            mainMenu.SetActive(true);
        }
        else
        {
            Debug.LogWarning("UIManager: Could not find a Main Menu object to activate. Make sure it exists and its name contains 'Main' or 'Menu'.");
        }

        // Ensure Start button is visible/active so player can begin again
        if (startButton != null)
            startButton.gameObject.SetActive(true);

        // Destroy any existing obstacles so the next run starts clean
        if (!string.IsNullOrEmpty(obstacleTag))
            DestroyAllWithTag(obstacleTag);

        // Destroy pickup/collectible objects if configured
        if (!string.IsNullOrEmpty(pickupTag))
            DestroyAllWithTag(pickupTag);
        else
            Debug.Log("UIManager: 'pickupTag' is not configured — skipping pickup cleanup.");

        // Reset player state (if any)
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
            player.ResetToMenuState();

        // Restore or remove the gameplay ScoreBoard copy (if present) and ensure main-menu ScoreBoard is visible
        var sm = Object.FindFirstObjectByType<ScoreManager>();
        if (scoreReparented != null)
        {
            // If we had reparented the original, restore it; otherwise decide based on whether the duplicate was created by the script
            if (scoreOriginalParent != null && scoreReparented.transform.parent == scoreOriginalParent)
            {
                // Old reparenting case: restore to original parent and leave active
                scoreReparented.transform.SetParent(scoreOriginalParent, false);
                scoreReparented.SetActive(true);
                Debug.Log($"UIManager: Restored '{scoreReparented.name}' to its original parent '{scoreOriginalParent.name}'.");
            }
            else
            {
                // Duplicate-case: if it was created at runtime, destroy it; if it was a designer-provided object, just deactivate it
                if (scoreReparentedCreatedByScript)
                {
                    Destroy(scoreReparented);
                    Debug.Log("UIManager: Destroyed gameplay ScoreBoard duplicate created at runtime.");
                }
                else
                {
                    scoreReparented.SetActive(false);
                    Debug.Log($"UIManager: Deactivated pre-existing gameplay ScoreBoard '{scoreReparented.name}' (kept in scene).");
                }
            }

            scoreReparented = null;
            scoreOriginalParent = null;
            scoreReparentedCreatedByScript = false;

            // Ensure ScoreManager references the main-menu text again
            if (sm != null)
                sm.FindAndAssignText();
        }

        // Make sure the ScoreBoard in the Main Menu (if any) is visible again
        var scoreGO = GameObject.Find("ScoreBoard") ?? GameObject.Find("ScoreText");
        if (scoreGO != null)
        {
            scoreGO.SetActive(true);
            Debug.Log($"UIManager: Ensured main-menu ScoreBoard ('{scoreGO.name}') is active on exit.");
        }

        // Reset the ScoreManager to clear any stale score and ensure it's wired to the main-menu text
        if (sm != null)
        {
            sm.ResetScore();
            sm.FindAndAssignText();
            Debug.Log("UIManager: Reset ScoreManager score on exit.");
        }

        // Restore Start button label if it was hidden earlier
        if (startButton != null)
        {
            var startText = startButton.GetComponentInChildren<TMP_Text>(true);
            if (startText != null)
                startText.gameObject.SetActive(true);
        }

        Debug.Log("UIManager: OnExitClicked — returned to Main Menu and reset game state.");
    }

    private void DestroyAllWithTag(string tag)
    {
        // Avoid calling FindGameObjectsWithTag if the tag is not defined (Editor-only check)
        if (!TagExists(tag))
        {
            Debug.LogWarning($"UIManager: Tag '{tag}' does not exist in Tags and Layers. Skipping cleanup for this tag.");
            return;
        }

        try
        {
            var found = GameObject.FindGameObjectsWithTag(tag);
            foreach (var o in found)
            {
                Destroy(o);
            }
        }
        catch (UnityException ex)
        {
            // Fallback: if something unexpected happens at runtime, log it but don't crash
            Debug.LogWarning($"UIManager: Exception while cleaning up tag '{tag}': {ex.Message}");
        }
    }

    private bool TagExists(string tag)
    {
#if UNITY_EDITOR
        // Use editor internal API to check defined tags
        var tags = UnityEditorInternal.InternalEditorUtility.tags;
        if (tags != null)
            return System.Array.IndexOf(tags, tag) >= 0;
        return false;
#else
        // At runtime we can't reliably query the tag list, assume true and keep the try/catch in DestroyAllWithTag
        return true;
#endif
    }

    // Searches scene root GameObjects and their children (including inactive) for a GameObject that looks like the Main Menu
    private GameObject FindMainMenuObject()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                var n = t.name.ToLower();
                if ((n.Contains("main") && n.Contains("menu")) || n.Contains("mainmenu") || n == "menu" || n.Contains("menu") )
                {
                    return t.gameObject;
                }
            }
        }
        return null;
    }
}