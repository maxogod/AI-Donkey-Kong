using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float jumpForce = 2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
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

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
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
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Flip sprite if moving left
        if (moveInput < 0) {
            spriteRenderer.flipX = true;
        } else if (moveInput > 0) {
            spriteRenderer.flipX = false;
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }
}
