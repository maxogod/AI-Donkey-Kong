using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float jumpForce = 1f;
    public float slipperyFactor = 0.3f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;

    private float moveInput;
    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        // Handle jumping
        if (jumpRequested && isGrounded)
        {
            Jump();
        }

        jumpRequested = false;
    }

    private void ProcessInput()
    {
        // Get horizontal movement
        moveInput = Input.GetAxisRaw("Horizontal");

        // Check if jump is requested
        if (Input.GetButtonDown("Jump"))
        {
            jumpRequested = true;
        }
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
}
