using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Управление точками спавна игроков
/// Добавьте на тот же объект где NetworkManager
/// </summary>
public class SpawnPointManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    
    [Header("Auto Generate (если нет точек)")]
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float spawnHeight = 1f;

    private int nextSpawnIndex = 0;

    private void Start()
    {
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
        // Только сервер перемещает игроков
        if (!NetworkManager.Singleton.IsServer) return;

        // Небольшая задержка чтобы игрок успел заспавниться
        StartCoroutine(MovePlayerToSpawnPoint(clientId));
    }

    private System.Collections.IEnumerator MovePlayerToSpawnPoint(ulong clientId)
    {
        // Ждем один кадр пока игрок заспавнится
        yield return null;

        // Находим игрока
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                Vector3 spawnPosition = GetSpawnPosition(nextSpawnIndex);
                
                // Перемещаем игрока
                client.PlayerObject.transform.position = spawnPosition;
                
                Debug.Log($"✅ Игрок {clientId} перемещен на позицию {spawnPosition}");
                
                nextSpawnIndex++;
            }
        }
    }

    private Vector3 GetSpawnPosition(int index)
    {
        // Если есть заданные точки спавна
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int pointIndex = index % spawnPoints.Length;
            return spawnPoints[pointIndex].position;
        }

        // Иначе генерируем по кругу
        int maxPlayers = 4;
        float angle = (360f / maxPlayers) * index;
        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * spawnRadius;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * spawnRadius;
        return new Vector3(x, spawnHeight, z);
    }

    // Для визуализации точек спавна в Editor
    private void OnDrawGizmos()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                    Gizmos.DrawLine(point.position, point.position + Vector3.up * 2f);
                }
            }
        }
        else
        {
            // Показываем автоматические точки
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = GetSpawnPosition(i);
                Gizmos.DrawWireSphere(pos, 0.5f);
            }
        }
    }
}

