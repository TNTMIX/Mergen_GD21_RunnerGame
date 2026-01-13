using UnityEngine;
using UnityEngine.InputSystem.XR;

public class MoveLeft : MonoBehaviour
{
    public float speed = 5f;
    private PlayerController playerControllerScript;
    private UIManager uiManager;
    private float leftBound = -15f;
    private bool hasBeenScored = false;

    void Start()
    {
        // Try to find the PlayerController robustly to avoid NullReferenceExceptions
        playerControllerScript = Object.FindFirstObjectByType<PlayerController>();
        if (playerControllerScript == null)
        {
            Debug.LogWarning("MoveLeft: PlayerController not found by FindFirstObjectByType; looking for GameObject named 'Player' as fallback.");
            var player = GameObject.Find("Player");
            if (player != null)
                playerControllerScript = player.GetComponent<PlayerController>();
        }

        // Cache UIManager so we don't search every frame
        uiManager = Object.FindFirstObjectByType<UIManager>();
    }
    void Update()
    {
        // Only move obstacles/backgrounds while the game has started and the player is not dead
        bool started = uiManager == null || uiManager.IsGameStarted;

        // Ensure PlayerController reference exists to avoid NullReferenceExceptions
        if (playerControllerScript == null)
            playerControllerScript = Object.FindFirstObjectByType<PlayerController>();

        bool playerIsAlive = playerControllerScript == null || !playerControllerScript.gameOver;

        if (started && playerIsAlive)
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);

            // If this is an obstacle that passed the player's X while the player was in the air, award one point
            if (gameObject.CompareTag("Obstacle") && !hasBeenScored)
            {
                if (playerControllerScript != null)
                {
                    // Only score if player was airborne while the obstacle passed the player's X and player is still alive
                    float playerX = playerControllerScript.transform.position.x;
                    if (transform.position.x < playerX && !playerControllerScript.isOnGround && !playerControllerScript.gameOver)
                    {
                        var sm = ScoreManager.Instance;
                        if (sm != null)
                            sm.AddScore(1);
                        else
                            Debug.LogWarning("MoveLeft: ScoreManager not found when trying to add score.");

                        hasBeenScored = true;
                    }
                }
            }
        }
        if (transform.position.x < leftBound && gameObject.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }
}