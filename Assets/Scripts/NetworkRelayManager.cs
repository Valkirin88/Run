using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Управление подключением через Unity Relay
/// </summary>
public class NetworkRelayManager : MonoBehaviour
{
    [SerializeField] private int maxPlayers = 4;

    private string currentJoinCode;

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Relay created with join code: {currentJoinCode}");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Подписываемся на события подключения клиентов
            NetworkManager.Singleton.OnClientConnectedCallback += OnHostClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnHostClientDisconnected;

            bool started = NetworkManager.Singleton.StartHost();
            Debug.Log($"StartHost() = {started}");
            
            if (!started)
            {
                Debug.LogError("Failed to start host!");
                return null;
            }

            Debug.Log($"✅ Хост запущен! LocalClientId: {NetworkManager.Singleton.LocalClientId}");
            return currentJoinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create relay: {e}");
            return null;
        }
    }

    private void OnHostClientConnected(ulong clientId)
    {
        Debug.Log($"🔗 [HOST] Клиент подключился: {clientId}");
    }

    private void OnHostClientDisconnected(ulong clientId)
    {
        Debug.Log($"❌ [HOST] Клиент отключился: {clientId}");
    }

    public async Task<bool> JoinRelay(string joinCode, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Debug.Log($"🔄 Попытка подключения {attempt}/{maxRetries}...");
            
            try
            {
                // Небольшая задержка перед попыткой (даём хосту время подготовиться)
                if (attempt > 1)
                {
                    await Task.Delay(1000);
                }

                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log($"Joined relay with code: {joinCode}");

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetClientRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    allocation.HostConnectionData
                );

                // Подписываемся на события для отладки (только один раз)
                if (attempt == 1)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                }

                bool started = NetworkManager.Singleton.StartClient();
                Debug.Log($"StartClient() = {started}");
                
                if (!started)
                {
                    Debug.LogWarning($"StartClient() вернул false, попытка {attempt}");
                    continue;
                }

                // Ждём подключения (до 15 секунд)
                float timeout = 15f;
                while (!NetworkManager.Singleton.IsConnectedClient && timeout > 0)
                {
                    await Task.Delay(100);
                    timeout -= 0.1f;
                }

                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    Debug.Log($"✅ Клиент подключен! ClientId: {NetworkManager.Singleton.LocalClientId}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"⏱ Таймаут подключения, попытка {attempt}");
                    
                    // Отключаемся перед следующей попыткой
                    if (NetworkManager.Singleton.IsListening)
                    {
                        NetworkManager.Singleton.Shutdown();
                        await Task.Delay(500);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Ошибка подключения (попытка {attempt}): {e.Message}");
            }
        }

        Debug.LogError($"❌ Не удалось подключиться после {maxRetries} попыток");
        return false;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"🔗 OnClientConnected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"❌ OnClientDisconnected: {clientId}");
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Disconnected from network");
        }
    }

    public string GetCurrentJoinCode() => currentJoinCode;
}

