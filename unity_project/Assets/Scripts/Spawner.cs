using UnityEngine;

public class Spawner : MonoBehaviour {
    public Transform barrelContainer;
    public GameObject prefabToSpawn;
    public float minSpawnTime = 8f;
    public float maxSpawnTime = 16f;

    private void Start() {
        Spawn();
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("JumpBarrelTrigger"), LayerMask.NameToLayer("Ladder"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("KillBarrelTrigger"), LayerMask.NameToLayer("Ladder"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Barrel"), true);
    }

    private void Spawn() {
        GameObject newBarrel = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);

        newBarrel.transform.SetParent(barrelContainer);

        Invoke(nameof(Spawn), Random.Range(minSpawnTime, maxSpawnTime));
    }
}
