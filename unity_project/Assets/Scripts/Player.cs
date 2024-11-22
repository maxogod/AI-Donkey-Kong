using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float jumpForce = 0.5f;
    public float slipperyFactor = 0.3f;
    public float climbSpeed = 2f;  // Speed at which the player climbs the ladder
    public float gravityScale = 2.5f;  // Gravity scale of the player
    public Vector3 spawnPoint = new Vector3(-4.64f, -6.22f, 0);

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    private Collider2D ladderCollider;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private bool isGrounded;
    private float moveInput;
    private bool jumpRequested;
    private bool isClimbing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        transform.position = spawnPoint;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Barrier"), true);
    }

    private void Update()
    {
        // Get input values
        ProcessInput();

        // Check if the player is grounded using a raycast
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    private void FixedUpdate()
    {
        Move();
        
        if (isClimbing) {
            ClimbLadder();
        }
            // Handle jumping
        if (jumpRequested && isGrounded) {
            Jump();
        }

        jumpRequested = false;
    }

    private void ProcessInput()
    {
        // Get horizontal movement
        moveInput = Input.GetAxisRaw("Horizontal");

        // Check if jump is requested
        if (Input.GetButtonDown("Jump")) {
            jumpRequested = true;
        }

        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Return)) {
            transform.position = new Vector3(4, 4, 0); // teleport to top
        }
        #endif
    }

    private void Move()
    {
        // Smoothly interpolate to the target velocity
        Vector2 targetVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, slipperyFactor);

        // Flip sprite if moving left
        if (moveInput < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (moveInput > 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // Called when the player enters a ladder trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))  // Check if the object is a ladder
        {
            isClimbing = true;
            ladderCollider = other;
            rb.gravityScale = 0f;  // Disable gravity while climbing

            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Ground"), true);
        } else if (other.CompareTag("WinningArea")) {
            HandleWin();
        }
    }

    // Called when the player exits a ladder trigger
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))  // Check if the object is a ladder
        {
            isClimbing = false;
            ladderCollider = null;
            rb.gravityScale = gravityScale;  // Re-enable gravity when not climbing

            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Ground"), false);
        }
    }

    private void OnBecameInvisible() {
        Die();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Barrel")) {
            HandleBarrelCollision(collision);
        }
    }

    private void HandleBarrelCollision(Collision2D collision) {
        Die();
    }

    private void HandleWin() {
        transform.position = spawnPoint;
    }

    private void Die() {
        transform.position = spawnPoint;
    }

    private void ClimbLadder()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, verticalInput * climbSpeed);
    }
}
