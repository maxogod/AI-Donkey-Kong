using UnityEngine;

public class WinningArea : MonoBehaviour {
    public GameObject effectPrefab;

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player")) {
            Instantiate(effectPrefab, other.transform.position + Vector3.up, Quaternion.identity);
        }
    }
}
