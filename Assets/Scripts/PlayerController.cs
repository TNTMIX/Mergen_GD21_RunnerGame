using UnityEngine;
using UnityEngine.EventSystems;
public class PlayerController : MonoBehaviour
{
    private Rigidbody playerRb;
    private Animator playerAnim;
    private AudioSource playerAudio;
    public float jumpForce = 25f;
    public float gravityModifier = 15f;
    public bool isOnGround = true;
    public bool gameOver = false;
    public bool isReloading;
    public bool settings = false;
    public ParticleSystem explosionParticle;
    public ParticleSystem dirtParticle;
    public ParticleSystem dustParticle;
    public ParticleSystem muzzleFlash;
    public AudioClip jumpSound;
    public AudioClip crashSound;
    public AudioClip pickupSound;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public SettingsUI settingsUI;
    private UIManager UIManager;
    private bool gameOverNotified = false;
    private Vector3 startPosition;
    private Quaternion startRotation;
    void Start()
    {
        playerAnim = GetComponent<Animator>();
        playerRb = GetComponent<Rigidbody>();
        playerAudio = GetComponent<AudioSource>();
        // Store starting transform so we can reset cleanly when returning to menu
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Set gravity deterministically so it doesn't multiply on each scene reload
        Physics.gravity = new Vector3(0f, -9.81f * gravityModifier, 0f);
        UIManager = Object.FindFirstObjectByType<UIManager>();
    }

    void Update()
    {
        // Keyboard fallback for jump so the player can try jumping even if input bindings are misconfigured
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnJump();
        }

        // Mouse click: ignore clicks over UI so UI buttons (e.g., Quit) can be clicked without also causing a jump
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return; // click was on UI, don't jump

            OnJump();
        }

        // Touch fallback: check touches began and ignore ones over UI
        if (Input.touchCount > 0)
        {
            foreach (var t in Input.touches)
            {
                if (t.phase != TouchPhase.Began)
                    continue;

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                    continue; // touch was on UI, ignore

                OnJump();
                break; // only trigger one jump per frame
            }
        }
    }

    public void OnJump()
    {    
        // Ensure UIManager reference and auto-start gameplay if the player attempts to jump before pressing Start
        if (UIManager == null) UIManager = Object.FindFirstObjectByType<UIManager>();
        if (UIManager != null && !UIManager.IsGameStarted)
        {
            Debug.Log("PlayerController: Jump attempted before gameplay started — auto-starting game.");
            UIManager.OnStartClicked();
        }

        // If game is over, don't allow jumps
        if (gameOver)
        {
            Debug.Log("PlayerController: Cannot jump — gameOver is true.");
            return;
        }

        if (isOnGround)
        {
            playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isOnGround = false;
            playerAnim.SetTrigger("Jump_trig");
            dirtParticle.Stop();
            playerAudio.PlayOneShot(jumpSound, 1.0f);
            Debug.Log("PlayerController: Jump executed.");
        }
    }
    public void OnFire()
    {
        if (UIManager == null) UIManager = Object.FindFirstObjectByType<UIManager>();
        if (UIManager != null && !UIManager.IsGameStarted)
            return;

        playerAnim.SetTrigger("Handgun_Shoot");
        muzzleFlash.Play();
        playerAudio.PlayOneShot(fireSound);
    }
    public void OnReload()
    {
        if (UIManager == null) UIManager = Object.FindFirstObjectByType<UIManager>();
        if (UIManager != null && !UIManager.IsGameStarted)
            return;

        playerAnim.SetTrigger("Handgun_Reload");
        playerAudio.PlayOneShot(reloadSound);
    }
    public void OnSettings()
    {
        if (settings == true)
        {
            settings = false;
            settingsUI.CloseSettings();
        }
        else
        {
            // Prevent opening settings if the player is already dead
            if (gameOver)
            {
                Debug.Log("PlayerController: Cannot open settings when game is over.");
                return;
            }

            settings = true;
            settingsUI.OpenSettings();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isOnGround = true;
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            gameOver = true;
            playerAudio.PlayOneShot(crashSound, 1.0f);
        }
        if (collision.gameObject.CompareTag("Ground"))
        {
            isOnGround = true;
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("Game Over");
            gameOver = true;
            playerAnim.SetBool("Death_b", true);
            playerAnim.SetInteger("DeathType_int", 1);
        }
        if (collision.gameObject.CompareTag("Ground"))
        {
            isOnGround = true;
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("PlayerController: Collision with Obstacle detected.");
            gameOver = true;
            if (playerAnim != null)
            {
                Debug.Log("PlayerController: Setting death animation parameters.");
                playerAnim.SetBool("Death_b", true);
                playerAnim.SetInteger("DeathType_int", 1);
            }
            else
            {
                Debug.LogWarning("PlayerController: Animator is null — cannot play death animation.");
            }
        }
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (explosionParticle != null)
                explosionParticle.Play();
            else
                Debug.LogWarning("PlayerController: explosionParticle is null.");
        }
        if (collision.gameObject.CompareTag("Ground"))
        {
            dirtParticle.Play();
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            dirtParticle.Stop();
        }
        if (collision.gameObject.CompareTag("Ground"))
        {
            dustParticle.Play();
        }
        if (collision.gameObject.CompareTag("Ground"))
        {
            isOnGround = true;
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            playerAnim.SetBool("Death_b", true);
            playerAnim.SetInteger("DeathType_int", 1);
        }

        // Notify UI Manager once when game over occurs so the lose panel can show after the death animation
        if (gameOver && !gameOverNotified)
        {
            gameOverNotified = true;
            if (UIManager == null) UIManager = Object.FindFirstObjectByType<UIManager>();
            if (UIManager != null)
                UIManager.SetGameOver(true);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Object"))
        {
            playerAudio.PlayOneShot(pickupSound, 1.0f);
        }
    }

    // Reset player state when returning to Main Menu so animations/flags don't remain in the death state
    public void ResetToMenuState()
    {
        gameOver = false;
        gameOverNotified = false;
        isOnGround = true;
        settings = false;
        isReloading = false;

        // Reset position and rotation
        transform.position = startPosition;
        transform.rotation = startRotation;

        // Reset physics
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // Reset animator to default state
        if (playerAnim != null)
        {
            playerAnim.Rebind();
            playerAnim.Update(0f);
            playerAnim.SetBool("Death_b", false);
            playerAnim.SetInteger("DeathType_int", 0);
        }

        // Stop and restart particles into their default states
        if (explosionParticle != null && explosionParticle.isPlaying)
            explosionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (muzzleFlash != null && muzzleFlash.isPlaying)
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (dirtParticle != null && !dirtParticle.isPlaying)
            dirtParticle.Play();
        if (dustParticle != null && !dustParticle.isPlaying)
            dustParticle.Play();

        // Restore timescale and audio in case anything left them changed
        Time.timeScale = 0f; // remain paused until Main Menu Start is clicked
        AudioListener.pause = false;

        Debug.Log("PlayerController: ResetToMenuState called — player state reset for Main Menu.");
    }
}