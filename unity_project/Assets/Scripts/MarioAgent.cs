using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public enum MarioActions : int {
    DoNothing = 0,

    // Horizontal branch
    MoveRight = 1,
    MoveLeft = 2,

    // Vertical branch
    Jump = 1,
    LadderUp = 2,
    LadderDown = 3
}

public class MarioAgent : Agent {

    [Header("Input Parameters")]
    // [SerializeField] private Transform queenTransform;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float jumpForce = 7f;
    public float slipperyFactor = 0.3f;
    public float climbSpeed = 2f;
    public float gravityScale = 1.5f;
    public Vector3 spawnPoint = new Vector3(-4.64f, -6.22f, 0);

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private Collider2D ladderCollider;
    private bool isGrounded;
    private float moveInput;
    private bool isClimbing = false;

    public override void Initialize() {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        transform.position = spawnPoint;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Barrier"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Player"), true);
    }

    public override void OnEpisodeBegin() {
        // Reset the agent to its initial state
        transform.position = spawnPoint;
    }
    
    public override void CollectObservations(VectorSensor sensor) {
        // Data that agent needs to evaluate the environment
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.position);
        // sensor.AddObservation(queenTransform.position);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        int horizontalAction = actions.DiscreteActions[0];
        switch (horizontalAction) {
            case (int) MarioActions.DoNothing:
                Move(0);
                break;
            case (int) MarioActions.MoveRight:
                MoveRight();
                break;
            case (int) MarioActions.MoveLeft:
                MoveLeft();
                break;
            default:
                Debug.LogError("Invalid action");
                break;
        }

        int verticalAction = actions.DiscreteActions[1];
        switch (verticalAction) {
            case (int) MarioActions.DoNothing:
                ClimbLadder(0);
                break;
            case (int) MarioActions.Jump:
                Jump();
                break;
            case (int) MarioActions.LadderUp:
                LadderUp();
                break;
            case (int) MarioActions.LadderDown:
                LadderDown();
                break;
            default:
                Debug.LogError("Invalid action");
                break;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = (int) MarioActions.DoNothing;

        if (Input.GetKey(KeyCode.RightArrow)) {
            discreteActions[0] = (int) MarioActions.MoveRight;
        } else if (Input.GetKey(KeyCode.LeftArrow)) {
            discreteActions[0] = (int) MarioActions.MoveLeft;
        }
        
        if (Input.GetKey(KeyCode.Space)) {
            discreteActions[1] = (int) MarioActions.Jump;
        } else if (Input.GetKey(KeyCode.UpArrow)) {
            discreteActions[1] = (int) MarioActions.LadderUp;
        } else if (Input.GetKey(KeyCode.DownArrow)) {
            discreteActions[1] = (int) MarioActions.LadderDown;
        }

        // Cheat code to teleport to the top
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Return)) {
            transform.position = new Vector3(4, 4, 0); // teleport to top
        }
        #endif
    }

    private void MoveRight() {
        spriteRenderer.flipX = false;
        Move(1);
    }

    private void MoveLeft() {
        spriteRenderer.flipX = true;
        Move(-1);
    }

    private void Move(int moveInput) {
        // smooth interpolation
        Vector2 targetVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, slipperyFactor);
    }

    private void Jump() {
        if (!isGrounded || isClimbing) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void LadderUp() {
        ClimbLadder(1);
    }

    private void LadderDown() {
        ClimbLadder(-1);
    }

    private void ClimbLadder(float verticalInput) {
        if (!isClimbing) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, verticalInput * climbSpeed);
    }

    private void HandleWin() {
        transform.position = spawnPoint;
        EndEpisode();
    }

    private void Die() {
        AddReward(-1.0f);
        EndEpisode();
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Ladder"))  // Check if the object is a ladder
        {
            isClimbing = true;
            ladderCollider = other;
            rb.gravityScale = 0f;  // Disable gravity while climbing
            rb.linearVelocity = Vector2.zero;

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Barrel")) {
            Die();
        }
    }

    private void OnBecameInvisible() {
        Die();
    }
}
