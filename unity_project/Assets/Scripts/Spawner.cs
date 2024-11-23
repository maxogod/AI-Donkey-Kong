using UnityEngine;

public class Spawner : MonoBehaviour {
    public Transform barrelContainer;
    public GameObject prefabToSpawn;
    public float minSpawnTime = 4f;
    public float maxSpawnTime = 8f;

    private void Start() {
        Spawn();
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("JumpBarrelTrigger"), LayerMask.NameToLayer("Ladder"), true);
    }

    private void Spawn() {
        GameObject newBarrel = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);

        newBarrel.transform.SetParent(barrelContainer);

        Invoke(nameof(Spawn), Random.Range(minSpawnTime, maxSpawnTime));
    }
}
