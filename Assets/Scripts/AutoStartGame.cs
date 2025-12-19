using Unity.Netcode;
using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Ожидает пока подключатся все игроки, затем включает их одновременно
/// Игроки спавнятся автоматически через NetworkManager, но отключены до старта
/// </summary>
public class AutoStartGame : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float countdownTime = 3f;
    
    [Header("UI - Waiting Screen")]
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private TMP_Text waitingStatusText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text countdownText;

    public event Action OnGameStarted;

    private NetworkVariable<int> netPlayerCount = new NetworkVariable<int>(0);
    private NetworkVariable<bool> netGameStarted = new NetworkVariable<bool>(false);
    private NetworkVariable<float> netCountdown = new NetworkVariable<float>(-1f);

    private List<ulong> connectedClients = new List<ulong>();
    private Dictionary<ulong, NetworkObject> playerObjects = new Dictionary<ulong, NetworkObject>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Подписываемся на изменения NetworkVariables (для клиентов)
        netPlayerCount.OnValueChanged += OnPlayerCountChanged;
        netGameStarted.OnValueChanged += OnGameStartedChanged;
        netCountdown.OnValueChanged += OnCountdownChanged;
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        ShowWaitingScreen();
        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        netPlayerCount.OnValueChanged -= OnPlayerCountChanged;
        netGameStarted.OnValueChanged -= OnGameStartedChanged;
        netCountdown.OnValueChanged -= OnCountdownChanged;
        
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        
        // Обратный отсчёт
        if (netCountdown.Value > 0 && !netGameStarted.Value)
        {
            netCountdown.Value -= Time.deltaTime;

            if (netCountdown.Value <= 0)
            {
                StartGameNow();
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // Защита от дублирования
        if (connectedClients.Contains(clientId))
            return;

        connectedClients.Add(clientId);
        netPlayerCount.Value = connectedClients.Count;
        
        Debug.Log($"✅ Игрок {clientId} подключился! Всего: {connectedClients.Count}/{minPlayersToStart}");

        // Ищем объект игрока (он появится через кадр)
        StartCoroutine(FindAndDisablePlayer(clientId));

        CheckStartConditions();
    }

    private System.Collections.IEnumerator FindAndDisablePlayer(ulong clientId)
    {
        // Ждём пока игрок заспавнится
        yield return new WaitForSeconds(0.1f);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                playerObjects[clientId] = client.PlayerObject;
                
                // Если игра ещё не началась - отключаем игрока
                if (!netGameStarted.Value)
                {
                    SetPlayerActive(client.PlayerObject, false);
                    Debug.Log($"⏸ Игрок {clientId} отключён до старта игры");
                }
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        connectedClients.Remove(clientId);
        playerObjects.Remove(clientId);
        netPlayerCount.Value = connectedClients.Count;
        
        Debug.Log($"❌ Игрок {clientId} отключился. Всего: {connectedClients.Count}");

        // Отменяем отсчёт если игроков мало
        if (netCountdown.Value > 0 && connectedClients.Count < minPlayersToStart)
        {
            netCountdown.Value = -1f;
            Debug.Log("⏸ Обратный отсчёт отменён");
        }
    }

    private void CheckStartConditions()
    {
        if (netGameStarted.Value) return;

        if (connectedClients.Count >= minPlayersToStart)
        {
            netCountdown.Value = countdownTime;
            Debug.Log($"⏱ Начинаем обратный отсчёт: {countdownTime} секунд!");
        }
    }

    private void StartGameNow()
    {
        if (netGameStarted.Value) return;
        
        netGameStarted.Value = true;
        Debug.Log($"🎮 ИГРА НАЧАЛАСЬ! Игроков: {connectedClients.Count}");

        // Включаем всех игроков одновременно
        foreach (var kvp in playerObjects)
        {
            if (kvp.Value != null)
            {
                SetPlayerActive(kvp.Value, true);
                Debug.Log($"✅ Игрок {kvp.Key} активирован!");
            }
        }
    }

    private void SetPlayerActive(NetworkObject playerObject, bool active)
    {
        if (playerObject == null) return;

        // Отключаем/включаем визуал и физику
        var renderers = playerObject.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = active;

        var colliders = playerObject.GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
            c.enabled = active;

        var rigidbody = playerObject.GetComponent<Rigidbody>();
        if (rigidbody != null)
            rigidbody.isKinematic = !active;

        // Отключаем контроллер
        var controller = playerObject.GetComponent<NetworkPlayerController>();
        if (controller != null)
            controller.enabled = active;
    }

    // Callbacks для NetworkVariables
    private void OnPlayerCountChanged(int oldValue, int newValue)
    {
        UpdateUI();
    }

    private void OnGameStartedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("🎮 Игра началась!");
            HideWaitingScreen();
            OnGameStarted?.Invoke();
        }
        UpdateUI();
    }

    private void OnCountdownChanged(float oldValue, float newValue)
    {
        UpdateUI();
    }

    // UI
    private void ShowWaitingScreen()
    {
        if (waitingPanel != null)
            waitingPanel.SetActive(true);
    }

    private void HideWaitingScreen()
    {
        if (waitingPanel != null)
            waitingPanel.SetActive(false);
    }

    private void UpdateUI()
    {
        if (waitingStatusText != null)
        {
            if (netGameStarted.Value)
                waitingStatusText.text = "Игра началась!";
            else if (netCountdown.Value > 0)
                waitingStatusText.text = "Игра скоро начнётся!";
            else if (netPlayerCount.Value < minPlayersToStart)
                waitingStatusText.text = "Ожидание игроков...";
            else
                waitingStatusText.text = "Готово!";
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"Игроков: {netPlayerCount.Value}/{minPlayersToStart}";
        }

        if (countdownText != null)
        {
            if (netCountdown.Value > 0)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = Mathf.CeilToInt(netCountdown.Value).ToString();
            }
            else
            {
                countdownText.gameObject.SetActive(false);
            }
        }
    }

    public bool IsGameStarted() => netGameStarted.Value;
    public int GetConnectedPlayers() => netPlayerCount.Value;
}
