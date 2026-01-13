using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; } = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        // Do not persist across scenes by default so Restart/Exit resets score
    }

    private void Start()
    {
        // Try to auto-assign a text object if inspector wasn't wired
        if (scoreText == null)
        {
            var go = GameObject.Find("ScoreBoard") ?? GameObject.Find("ScoreText");
            if (go != null)
            {
                scoreText = go.GetComponentInChildren<TMP_Text>(true);
                if (scoreText != null)
                    Debug.Log($"ScoreManager: Auto-assigned scoreText from '{go.name}'");
            }
        }

        if (scoreText == null)
        {
            // Last resort: find any TMP_Text in the scene
            scoreText = Object.FindFirstObjectByType<TMP_Text>();
            if (scoreText != null)
                Debug.Log($"ScoreManager: Auto-assigned scoreText using FindFirstObjectByType -> '{scoreText.name}'");
        }

        UpdateUI();
    }

    public void AddScore(int amount = 1)
    {
        Score += amount;
        UpdateUI();
        Debug.Log($"ScoreManager: Score increased by {amount} -> {Score}");
    }

    public void ResetScore()
    {
        Score = 0;
        UpdateUI();
        Debug.Log("ScoreManager: Score reset to 0.");
    }

    // Attempt to find and assign the score UI text (useful if the scoreboard was inactive at Start and became active later)
    public void FindAndAssignText()
    {
        if (scoreText != null)
        {
            UpdateUI();
            return;
        }

        var go = GameObject.Find("ScoreBoard") ?? GameObject.Find("ScoreText");
        if (go != null)
        {
            scoreText = go.GetComponentInChildren<TMP_Text>(true);
            if (scoreText != null)
            {
                Debug.Log($"ScoreManager: FindAndAssignText -> assigned from '{go.name}'.");
                UpdateUI();
                return;
            }
        }

        scoreText = Object.FindFirstObjectByType<TMP_Text>();
        if (scoreText != null)
        {
            Debug.Log($"ScoreManager: FindAndAssignText -> assigned using FindFirstObjectByType -> '{scoreText.name}'.");
        }

        UpdateUI();
    }

    // Directly set the TMP text component to use for score display (useful when cloning a runtime copy)
    public void SetTextComponent(TMP_Text newText)
    {
        if (newText == null)
        {
            Debug.LogWarning("ScoreManager.SetTextComponent called with null.");
            return;
        }
        scoreText = newText;
        UpdateUI();
        Debug.Log($"ScoreManager: SetTextComponent -> assigned to '{newText.name}'.");
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = Score.ToString();
    }
}
