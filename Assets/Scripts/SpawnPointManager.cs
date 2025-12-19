using Unity.Netcode;
using UnityEngine;
using System.Collections;

/// <summary>
/// Управление точками спавна игроков
/// </summary>
public class SpawnPointManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private int nextSpawnIndex = 0;

    private void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("❌ SpawnPointManager: Spawn Points не назначены!");
            return;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        StartCoroutine(MovePlayerToSpawnPoint(clientId));
    }

    private IEnumerator MovePlayerToSpawnPoint(ulong clientId)
    {
        yield return new WaitForSeconds(0.1f);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null && spawnPoints.Length > 0)
            {
                int index = nextSpawnIndex % spawnPoints.Length;
                Vector3 spawnPos = spawnPoints[index].position;
                
                client.PlayerObject.transform.position = spawnPos;
                
                Debug.Log($"📍 Игрок {clientId} → SpawnPoint{index + 1}: {spawnPos}");
                nextSpawnIndex++;
            }
        }
    }

    public Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return Vector3.zero;
            
        return spawnPoints[index % spawnPoints.Length].position;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);
                Gizmos.DrawLine(spawnPoints[i].position, spawnPoints[i].position + Vector3.up * 2f);
            }
        }
    }
}
