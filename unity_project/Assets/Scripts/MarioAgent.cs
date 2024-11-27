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
    public int maxBarrels = 3;

    [Header("Rewards")]
    public float winReward = 2.0f;
    public float climbingReward = 0.5f;
    public float jumpBarrelReward = 0.07f;
    public float moveHorizontally = 0.2f;
    public float baseZoneReward = 0.5f;
    public float reenterZoneReward = -0.5f;
    public float ClimbingDownReward = -0.1f;
    public float jumpTooMuchReward = -0.05f;
    public float idleReward = -0.2f;
    public float dieReward = -1.0f;
    public float fallOffReward = -1.0f;

    [Header("Penalty Settings")]
    public float maxLoopsIdle = 350;
    public float IdleYDistance = 1.8f;
    public float IdleXDistance = 5.5f;
    public float maxJumps = 5;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float jumpForce = 7f;
    public float slipperyFactor = 0.3f;
    public float climbSpeed = 2f;
    public float gravityScale = 1.5f;
    public Vector2 spawnPoint = new Vector2(-4.64f, -6.22f);

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Collider2D marioCollider;
    private SpriteRenderer spriteRenderer;

    private Collider2D ladderCollider;
    private Collider2D[] floorColliders = new Collider2D[0];
    private float closestLadder = 100;

    private bool isGrounded;
    private float moveInput;
    private bool isClimbing = false;
    private bool isTeleporting = false;
    private bool isQuiting = false;
    private float lastPositionY = -6.22f;
    private float lastPositionX = -4.64f;
    private int loopIdle = 0;
    // private int jumpsCount = 0;
    private float highestPosY = -10f;
    private float nextZoneDistance = 100f;

    // private int initialMoves = 0;
    // private float maxInitialMoves = 50;
    // private int initialClimbMoves = 0;
    // private float maxInitialClimbMoves = 20;

    private float lastCheckpointX; // updates every time it moves horizontally to a new checkpoint
    // visited zones <name, <visited, additional_reward>
    private Dictionary<string, Tuple<bool, float>> visitedZones;

    // private float world_bottom = -10f;
    private float maxDistance = 12.5f;
    private float maxSpeed = 8.5f;

    public override void Initialize() {
        rb = GetComponent<Rigidbody2D>();
        marioCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        isTeleporting = true;
        transform.position = new Vector3(spawnPoint.x, spawnPoint.y, transform.position.z);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Barrier"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Player"), true);
    }

    public void OnApplicationQuit() {
        isQuiting = true;
        Debug.Log("Reward: " + GetCumulativeReward());
    }

    public override void OnEpisodeBegin() {
        foreach (Transform barrel in barrelContainer) {
            if (barrel) Destroy(barrel.gameObject);
        }
        // Reset the agent to its initial state
        ResetAgent();
    }
    
    public override void CollectObservations(VectorSensor sensor) {
        // Data that agent needs to evaluate the environment
        Vector2 MarioPosition = transform ? transform.position : Vector2.zero;
        sensor.AddObservation(MarioPosition / maxDistance); // 2 obs
        sensor.AddObservation(rb.linearVelocity / maxSpeed); // 2 obs
        sensor.AddObservation(isGrounded ? 1 : 0); // 1 obs
        sensor.AddObservation(isClimbing ? 1 : 0); // 1 obs

        CollectClosestZoneObservation(sensor); // 1 obs
        CollectLadderObservations(sensor); // 1 obs
        CollectBarrierObservations(sensor); // 2 obs
        CollectBarrelObservations(sensor); // 6 obs
        // total: 15 obs
    }

    public override void OnActionReceived(ActionBuffers actions) {
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        // if (initialMoves < maxInitialMoves) {
        //     MoveRight();
        //     initialMoves++;
        //     return;
        // }
        // if (ladderCollider && Mathf.Abs(transform.position.x - ladderCollider.transform.position.x) < 0.5f && initialClimbMoves < maxInitialClimbMoves) {
        //     LadderUp(); // Prioritize climbing when near a ladder
        //     initialClimbMoves++;
        //     return;
        // }

        CheckIdle();

        int horizontalAction = actions.DiscreteActions[0];
        int verticalAction = actions.DiscreteActions[1];

        if (UnityEngine.Random.Range(0f, 1f) < 0.05f) { // 5% of the time, take a random action
            horizontalAction = UnityEngine.Random.Range(0, 3);
            verticalAction = UnityEngine.Random.Range(0, 4);
        }

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

        if (transform.position.y > highestPosY) {
            highestPosY = transform.position.y;
            float base_highscore_reward = 0.02f;
            // scale the reward based on the distance from the princess
            float progress = Mathf.Clamp(1f - ((princessTransform.position.y - transform.position.y) / maxDistance), 0f, 1f);
            base_highscore_reward = base_highscore_reward * progress;
            AddCustomReward(base_highscore_reward);
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
            isTeleporting = true;
            transform.position = new Vector3(4, 4, transform.position.z); // teleport to top
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
        // AddCustomReward(jumpTooMuchReward);
        // Debug.Log("[Penalty] Jump");
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        // jumpsCount++;
        // if (jumpsCount > maxJumps) {
        //     AddCustomReward(jumpTooMuchReward);
        //     jumpsCount = 0;
        // }
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
        isTeleporting = true;
        AddCustomReward(winReward);
        Debug.Log("[Reward] Win");

        Debug.Log("Reward: " + GetCumulativeReward());
        EndEpisode();
    }

    private void Die() {
        isTeleporting = true;

        float progress = Mathf.Clamp(1f - (Vector2.Distance(transform.position, princessTransform.position) / maxDistance), 0f, 1f);
        float scaledDiePenalty = dieReward * (1f - progress); // Smaller penalty if further from the princess
        AddCustomReward(scaledDiePenalty);
        Debug.Log("[Penalty] Die");

        Debug.Log("Reward: " + GetCumulativeReward());
        EndEpisode();
    }

    private void CheckIdle() {
        // if the absolute value of the difference between the current and last position is less than 0.01
        if (Mathf.Abs(transform.position.x - lastPositionX) < IdleXDistance &&
            Mathf.Abs(transform.position.y - lastPositionY) < IdleYDistance) {
            loopIdle++;
        } else {
            lastPositionY = transform.position.y;
            lastPositionX = transform.position.x;
            loopIdle = 0;
        }

        if (loopIdle > maxLoopsIdle) {
            AddCustomReward(idleReward);
            Debug.Log("[Penalty] Idle");

            lastPositionY = transform.position.y;
            lastPositionX = transform.position.x;
            loopIdle = 0;
        }

        if (Mathf.Abs(transform.position.x - lastCheckpointX) > IdleXDistance) {
            AddCustomReward(moveHorizontally);
            Debug.Log("[Reward] Move Horizontally");
            lastCheckpointX = transform.position.x;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Ladder")) {
            isClimbing = true;
            ladderCollider = other;
            rb.gravityScale = 0f;  // Disable gravity while climbing
            rb.linearVelocity = Vector2.zero;

            // if (other.transform.position.y >= highestPosY) {
            //     AddCustomReward(climbingReward / 2);
            //     Debug.Log("[Reward] Touch Ladder");
            // }

            IgnoreFloorCollisions();
        } else if (other.CompareTag("WinningArea")) {
            HandleWin();
        } else if (other.CompareTag("Barrel") && Mathf.Abs(transform.position.z - other.transform.position.z) < 0.1f) {
            Die();
        } else if (other.CompareTag("JumpBarrelTrigger") && !isTeleporting && marioCollider.bounds.min.y > other.bounds.min.y -0.3f) {
            AddCustomReward(jumpBarrelReward);
            Debug.Log("[Reward] Jump Barrel");
        }
        CheckZones(other);
    }

    // Called when the player exits a ladder trigger
    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Ladder")) {

            float marioTopBound = marioCollider.bounds.max.y;
            float ladderTopBound = ladderCollider.bounds.max.y;
            if (marioTopBound > ladderTopBound && ladderTopBound > highestPosY) {
                AddCustomReward(climbingReward);
                Debug.Log("[Reward] Climbing Up");
            } else if (marioTopBound < ladderTopBound && ladderTopBound < highestPosY) {
                AddCustomReward(ClimbingDownReward);
                Debug.Log("[Penalty] Climbing Down");
            }

            isClimbing = false;
            ladderCollider = null;
            rb.gravityScale = gravityScale;  // Re-enable gravity when not climbing

            IgnoreFloorCollisions();
        }
    }

    private void OnBecameInvisible() {
        if (isTeleporting) return;
        if (isQuiting) return;
        AddCustomReward(fallOffReward);
        Debug.Log("[Penalty] Fall Off");
        Die();
    }

    private void CheckZones(Collider2D other) {
        if (visitedZones.ContainsKey(other.name)) {
            if (visitedZones[other.name].Item1) {
                AddCustomReward((reenterZoneReward - visitedZones[other.name].Item2));
                visitedZones[other.name] = new Tuple<bool, float>(false, visitedZones[other.name].Item2);
                Debug.Log("[Penalty] Reenter " + other.name);
            } else {
                AddCustomReward(baseZoneReward + visitedZones[other.name].Item2);
                visitedZones[other.name] = new Tuple<bool, float>(true, visitedZones[other.name].Item2);
                Debug.Log("[Reward] " + other.name);
            }
        }
    }

    private void CollectClosestZoneObservation(VectorSensor sensor) {
        float closestDistanceY = 100f;
        foreach (KeyValuePair<string, Tuple<bool, float>> zone in visitedZones) {
            if (zone.Value.Item1) continue;
            GameObject zoneObject = GameObject.Find(zone.Key);
            float distanceY = Mathf.Abs(zoneObject.transform.position.y - transform.position.y);
            if (distanceY < closestDistanceY) {
                closestDistanceY = distanceY;
                nextZoneDistance = Vector2.Distance(zoneObject.transform.position, transform.position) / maxDistance;
            }
        }
        sensor.AddObservation(nextZoneDistance);
    }

    private void CollectLadderObservations(VectorSensor sensor) {
        if (!ladderContainer) return;
        float closestDistanceY = 100f;
        foreach (Transform ladder in ladderContainer) {
            if (!ladder) continue;
            float ladderDistanceY = Mathf.Abs(ladder.position.y - transform.position.y);
            float topOfLadder = ladder.position.y + 0.5f;

            if (ladderDistanceY <= closestDistanceY && topOfLadder > transform.position.y) {
                closestLadder = Mathf.Abs(ladder.position.x - transform.position.x);
                closestDistanceY = ladderDistanceY;
            }
        }
        sensor.AddObservation(closestLadder / maxDistance);
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
            float relativePosition = barrel.position.x - transform.position.x;
            sensor.AddObservation(relativePosition / maxDistance);

            Rigidbody2D barrel_rb = barrel.GetComponent<Rigidbody2D>();
            if (barrel_rb) {
                sensor.AddObservation(barrel_rb.linearVelocity.x / maxSpeed);
            }
        }

        for (int i = barrels.Count; i < maxBarrels; i++) {
            sensor.AddObservation(-1.0f);
            sensor.AddObservation(0f);
        }
    }

    private void CollectBarrierObservations(VectorSensor sensor) {
        if (!barrierContainer) return;

        foreach (Transform barrier in barrierContainer) {
            if (!barrier) continue;
            float barrier_x = barrier.position.x;
            sensor.AddObservation((barrier_x - transform.position.x) / maxDistance);
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

    private void ResetAgent() {
        transform.position = new Vector3(spawnPoint.x, spawnPoint.y, transform.position.z);
        // jumpsCount = 0;
        loopIdle = 0;
        lastPositionY = transform.position.y;
        lastPositionX = transform.position.x;
        lastCheckpointX = transform.position.x;
        isTeleporting = false;
        highestPosY = -10f;
        visitedZones = new Dictionary<string, Tuple<bool, float>> {
            {"Zone2", new Tuple<bool, float>(false, 0)},
            {"Zone3", new Tuple<bool, float>(false, 0.1f)},
            {"Zone4", new Tuple<bool, float>(false, 0.2f)},
            {"Zone5", new Tuple<bool, float>(false, 0.3f)},
            {"Zone6", new Tuple<bool, float>(false, 0.4f)}
        };
    }

    private void AddCustomReward(float reward) {
        // Add a reward to the agent relative to the next ladder
        
        float newReward = Mathf.Clamp(reward, -1f, 1f);
        if (reward < 0) {
            newReward *= (1f - (closestLadder / maxDistance));
        }
        
        AddReward(reward);
    }

    private void IgnoreFloorCollisions() {
        if (floorColliders.Length == 0) {
            floorColliders = Physics2D.OverlapCircleAll(transform.position, groundCheckDistance*8, groundLayer);
            foreach (var floorCollider in floorColliders) {
                if (floorCollider) {
                    Physics2D.IgnoreCollision(marioCollider, floorCollider, true);
                }
            }
        } else {
            foreach (var floorCollider in floorColliders) {
                if (floorCollider) {
                    Physics2D.IgnoreCollision(marioCollider, floorCollider, false);
                }
            }
            floorColliders = new Collider2D[0];
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, groundCheckDistance*8);
    }
}
