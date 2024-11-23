using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using System;

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
    [SerializeField] private Transform princessTransform;
    [SerializeField] private Transform ladderContainer;
    [SerializeField] private Transform barrierContainer;
    [SerializeField] private Transform barrelContainer;
    public int maxBarrels = 5;
    public int maxLadders = 3;

    [Header("Rewards")]
    public float winReward = 1.0f;
    public float clibingReward = 0.3f;
    public float jumpBarrelReward = 0.01f;
    public float moveHorizontally = 0.5f;
    public float groundPlayingReward = 0.05f;
    public float baseZoneReward = 0.3f;
    public float reenterZoneReward = -0.3f;
    public float ClimbingDownReward = -0.1f;
    public float jumpTooMuchReward = -0.5f;
    public float idleReward = -0.5f;
    public float dieReward = -1.0f;

    [Header("Penalty Settings")]
    public float maxLoopsIdle = 300;
    public float IdleYDistance = 1.8f;
    public float IdleXDistance = 5.0f;
    public float maxJumps = 5;
    public float maxGroundedLoops = 100;

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
    private float lastPositionY;
    private float lastPositionX;
    private int loopIdle = 0;
    private int loopsGrounded = 0;
    private int jumpsCount = 0;
    private float lastCheckpointX; // updates every time it moves horizontally to a new checkpoint
    // visited zones <name, <visited, additional_reward>
    private Dictionary<string, Tuple<bool, float>> visitedZones;

    private float world_bottom = -10f;

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
        jumpsCount = 0;
        loopIdle = 0;
        lastPositionY = transform.position.y;
        lastPositionX = transform.position.x;
        lastCheckpointX = transform.position.x;
        visitedZones = new Dictionary<string, Tuple<bool, float>> {
            {"Zone2", new Tuple<bool, float>(false, 0)},
            {"Zone3", new Tuple<bool, float>(false, 0.1f)},
            {"Zone4", new Tuple<bool, float>(false, 0.2f)},
            {"Zone5", new Tuple<bool, float>(false, 0.3f)},
            {"Zone6", new Tuple<bool, float>(false, 0.4f)}
        };
    }
    
    public override void CollectObservations(VectorSensor sensor) {
        // Data that agent needs to evaluate the environment
        Vector2 MarioPosition = transform ? transform.position : Vector2.zero;
        sensor.AddObservation(MarioPosition); // 2 obs
        sensor.AddObservation(rb.linearVelocity); // 2 obs
        sensor.AddObservation(isGrounded ? 1 : 0); // 1 obs
        sensor.AddObservation(isClimbing ? 1 : 0); // 1 obs

        Vector2 princessPosition = princessTransform ? princessTransform.position : Vector2.up*10;
        sensor.AddObservation(Vector2.Distance(MarioPosition, princessPosition)); // 1 obs

        CollectLadderObservations(sensor); // 6 obs
        CollectBarrierObservations(sensor); // 2 obs
        CollectBarrelObservations(sensor); // 15 obs
        // total: 30 obs
    }

    public override void OnActionReceived(ActionBuffers actions) {
        // Cumulative reward for the agent
        Debug.Log("Reward: " + GetCumulativeReward());
        
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        CheckGroundPlaying();

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

        if (ladderCollider && Mathf.Abs(transform.position.x - ladderCollider.transform.position.x) < 0.5f) {
            LadderUp(); // Prioritize climbing when near a ladder
            return;
        }


        CheckIdle();
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
        jumpsCount++;
        if (jumpsCount > maxJumps) {
            AddCustomReward(jumpTooMuchReward);
            jumpsCount = 0;
        }
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
        AddCustomReward(winReward);
        EndEpisode();
    }

    private void Die() {
        AddCustomReward(dieReward);
        EndEpisode();
    }

    private void CheckIdle() {
        // if the absolute value of the difference between the current and last position is less than 0.01
        if (Mathf.Abs(transform.position.y - lastPositionY) < IdleYDistance ||
            Mathf.Abs(transform.position.x - lastPositionX) < IdleXDistance) {
            loopIdle++;
        } else {
            loopIdle = 0;
        }

        if (loopIdle > maxLoopsIdle) {
            AddCustomReward(idleReward);
            loopIdle = 0;
        }

        lastPositionY = transform.position.y;
        lastPositionX = transform.position.x;

        if (Mathf.Abs(transform.position.x - lastCheckpointX) > IdleXDistance) {
            AddCustomReward(moveHorizontally);
            lastCheckpointX = transform.position.x;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Ladder")) {
            isClimbing = true;
            ladderCollider = other;
            rb.gravityScale = 0f;  // Disable gravity while climbing
            rb.linearVelocity = Vector2.zero;

            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Ground"), true);
        } else if (other.CompareTag("WinningArea")) {
            HandleWin();
        } else if (other.CompareTag("JumpBarrelTrigger")) {
            AddCustomReward(jumpBarrelReward);
        }
        CheckZones(other);
    }

    // Called when the player exits a ladder trigger
    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Ladder")) {
            if (transform.position.y > other.transform.position.y) {
                AddCustomReward(clibingReward);
            } else {
                AddCustomReward(ClimbingDownReward);
            }

            isClimbing = false;
            ladderCollider = null;
            rb.gravityScale = gravityScale;  // Re-enable gravity when not climbing

            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Ground"), false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Barrel")) {
            Die();
        }
    }

    private void OnBecameInvisible() {
        Die();
    }

    private void CheckZones(Collider2D other) {
        if (visitedZones.ContainsKey(other.name)) {
            Debug.Log(other.name + "Atus");
            if (visitedZones[other.name].Item1) {
                AddCustomReward(reenterZoneReward - visitedZones[other.name].Item2);
            } else {
                AddCustomReward(baseZoneReward + visitedZones[other.name].Item2);
            }
            visitedZones[other.name] = new Tuple<bool, float>(true, visitedZones[other.name].Item2);
        }
    }

    private void CheckGroundPlaying() {
        if (isGrounded) {
            loopsGrounded++;
        } else {
            loopsGrounded = 0;
        }

        if (loopsGrounded > maxGroundedLoops) {
            AddCustomReward(groundPlayingReward);
            if (groundPlayingReward < 0.2f) {
                groundPlayingReward += 0.05f;
                maxGroundedLoops += 50;
            }
            loopsGrounded = 0;
        }
    }

    private void CollectLadderObservations(VectorSensor sensor) {
        if (!ladderContainer) return;
        List<Transform> ladders = SortObservations(ladderContainer, sensor, (obj1, obj2) => {
            float distance1 = Mathf.Abs(obj1.position.y - transform.position.y);
            float distance2 = Mathf.Abs(obj2.position.y - transform.position.y);
            return distance1.CompareTo(distance2);
        });

        for (int i = 0; i < Mathf.Min(maxLadders, ladders.Count); i++) {
            Vector2 ladderPosition = ladders[i].position;
            sensor.AddObservation(ladderPosition - (Vector2)transform.position); // Relative position
        }
    }

    private void CollectBarrelObservations(VectorSensor sensor) {
        if (!barrelContainer) return;
        List<Transform> barrels = SortObservations(barrelContainer, sensor, (obj1, obj2) => {
            float distance1 = Mathf.Abs(obj1.position.x - transform.position.x);
            float distance2 = Mathf.Abs(obj2.position.x - transform.position.x);
            return distance1.CompareTo(distance2);
        });

        for (int i = 0; i < Mathf.Min(maxBarrels, barrels.Count); i++) {
            Transform barrel = barrels[i];
            if (!barrel) continue;
            Vector2 barrelPosition = barrel.position;
            sensor.AddObservation(barrelPosition);

            Rigidbody2D barrel_rb = barrel.GetComponent<Rigidbody2D>();
            if (barrel_rb) {
                sensor.AddObservation(barrel_rb.linearVelocity.x);
            }
        }

        for (int i = barrels.Count; i < maxBarrels; i++) {
            sensor.AddObservation(new Vector2(0, world_bottom));
            sensor.AddObservation(0f);
        }
    }

    private void CollectBarrierObservations(VectorSensor sensor) {
        if (!barrierContainer) return;

        foreach (Transform barrier in barrierContainer) {
            if (!barrier) continue;
            float barrier_x = barrier.position.x;
            sensor.AddObservation(barrier_x);
        }
    }

    private List<Transform> SortObservations(Transform objectContainer, VectorSensor sensor, Comparison<Transform> cmp = null) {
        List<Transform> objects = new List<Transform>();

        foreach (Transform obj in objectContainer) {
            if (!obj) continue;
            objects.Add(obj);
        }
        
        objects.Sort(cmp);

        return objects;
    }

    private void AddCustomReward(float reward) {
        // Add a reward to the agent (maybe relative to time playing eventually)
        AddReward(reward);
    }
}
