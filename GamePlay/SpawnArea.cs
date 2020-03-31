using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    public float areaSizeX;
    public float areaSizeZ;
    public float avoidWallRange = 1f;
    public int findAttemps = 20;
    public LayerMask wallMask;

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3.up * 1f), new Vector3(areaSizeX, 2f, areaSizeZ));
    }

    public Vector3 GetSpawnPosition()
    {
        Vector3 pos = transform.position + new Vector3(Random.Range(-areaSizeX / 2f, areaSizeX / 2f), 0, Random.Range(-areaSizeZ / 2f, areaSizeZ / 2f));
        for (int i = 0; i < findAttemps; ++i)
        {
            var colliders = Physics.OverlapSphere(pos, avoidWallRange, wallMask);
            if (colliders.Length == 0)
                return pos;
            pos = transform.position + new Vector3(Random.Range(-areaSizeX / 2f, areaSizeX / 2f), 0, Random.Range(-areaSizeZ / 2f, areaSizeZ / 2f));
        }
        return pos;
    }
}
