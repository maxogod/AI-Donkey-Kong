using UnityEngine;

public class Spawner : MonoBehaviour {
    public GameObject prefabToSpawn;
    public float minSpawnTime = 4f;
    public float maxSpawnTime = 8f;

    private void Start() {
        Spawn();
    }

    private void Spawn() {
        Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
        Invoke(nameof(Spawn), Random.Range(minSpawnTime, maxSpawnTime));
    }
}
