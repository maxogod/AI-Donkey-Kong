using UnityEngine;

public class Spawner : MonoBehaviour {
    public Transform barrelContainer;
    public Transform barrelSpawnPoint;
    public GameObject prefabToSpawn;
    public float minSpawnTime = 3f;
    public float maxSpawnTime = 6f;
    public bool makeInvisible = false;

    private void Start() {
        Spawn();
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("JumpBarrelTrigger"), LayerMask.NameToLayer("Ladder"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("KillBarrelTrigger"), LayerMask.NameToLayer("Ladder"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Barrel"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Barrel"), LayerMask.NameToLayer("Barrel"), true);
    }

    private void Spawn() {
        GameObject newBarrel = Instantiate(prefabToSpawn, barrelSpawnPoint.position, Quaternion.identity, barrelContainer);

        if (makeInvisible) {
            Renderer barrelRenderer = newBarrel.GetComponent<Renderer>();
            if (barrelRenderer != null) {
                barrelRenderer.enabled = false;
            }
        }

        Invoke(nameof(Spawn), Random.Range(minSpawnTime, maxSpawnTime));
    }
}
