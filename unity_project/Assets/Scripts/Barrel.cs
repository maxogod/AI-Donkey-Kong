using UnityEngine;

public class Barrel : MonoBehaviour {
    public float spawnForce = 3f;
    public float groundCheckRadius = 1f;
    public int probabilityOfFalling = 4;
    public int maxLoopsIdle = 100;
    public LayerMask groundLayer;
    
    public Transform groundCheck;

    private Rigidbody2D rb;

    private Vector2 direction = Vector2.right;
    private bool isFalling = false;
    private int loopsIdle = 0;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(Vector2.right * spawnForce, ForceMode2D.Impulse);
    }

    private void Update() {
        if (rb.linearVelocity.x == 0) {
            loopsIdle++;
        }

        if (loopsIdle > maxLoopsIdle) {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Barrel")) {
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible() {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Ladder")) return;

        // probability of falling  1/4
        if (Random.Range(0, 4) != 0) {
            return;
        }
        
        Collider2D[] groundColliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);

        foreach (var groundCollider in groundColliders) {
            if (groundCollider != null) {
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), groundCollider, true);
            }
        }
        
        isFalling = true;
        if (rb.linearVelocity.x < 0) {
            direction = Vector2.right;
        } else {
            direction = Vector2.left;
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (!other.CompareTag("Ladder")) return;
        
        if (isFalling) {
            rb.AddForce(direction * spawnForce * 2, ForceMode2D.Impulse);
            isFalling = false;
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
