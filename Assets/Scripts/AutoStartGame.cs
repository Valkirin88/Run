using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Автоматически запускает игру когда подключится минимум игроков
/// Блокирует управление до начала игры
/// </summary>
public class AutoStartGame : NetworkBehaviour
{
    public static AutoStartGame Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int minPlayersToStart = 2; // Минимум игроков для старта
    [SerializeField] private float countdownTime = 3f; // Обратный отсчёт перед стартом
    
    [Header("UI - Waiting Screen")]
    [SerializeField] private GameObject waitingPanel; // Панель ожидания
    [SerializeField] private TMP_Text waitingStatusText; // Текст статуса
    [SerializeField] private TMP_Text playerCountText; // Счётчик игроков
    [SerializeField] private TMP_Text countdownText; // Текст обратного отсчёта

    // События
    public event Action OnGameStarted;
    public event Action<int> OnPlayerCountChanged;

    private NetworkVariable<int> networkedPlayerCount = new NetworkVariable<int>(0);
    private NetworkVariable<bool> networkedGameStarted = new NetworkVariable<bool>(false);
    private NetworkVariable<float> networkedCountdown = new NetworkVariable<float>(-1f);

    private bool localGameStarted = false;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Подписываемся на изменения NetworkVariables
        networkedPlayerCount.OnValueChanged += OnPlayerCountChangedCallback;
        networkedGameStarted.OnValueChanged += OnGameStartedCallback;
        networkedCountdown.OnValueChanged += OnCountdownChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // Хост = первый игрок
            networkedPlayerCount.Value = 1;
        }

        // Показываем экран ожидания
        ShowWaitingScreen();
        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        networkedPlayerCount.OnValueChanged -= OnPlayerCountChangedCallback;
        networkedGameStarted.OnValueChanged -= OnGameStartedCallback;
        networkedCountdown.OnValueChanged -= OnCountdownChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void Update()
    {
        // Обновляем обратный отсчёт на сервере
        if (IsServer && networkedCountdown.Value > 0)
        {
            networkedCountdown.Value -= Time.deltaTime;

            if (networkedCountdown.Value <= 0)
            {
                StartGameNow();
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        networkedPlayerCount.Value++;
        Debug.Log($"✅ Игрок {clientId} подключился! Всего: {networkedPlayerCount.Value}/{minPlayersToStart}");

        // Проверяем достаточно ли игроков для старта
        CheckStartConditions();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        networkedPlayerCount.Value--;
        Debug.Log($"❌ Игрок {clientId} отключился. Всего: {networkedPlayerCount.Value}");

        // Если обратный отсчёт шёл и игроков стало мало - отменяем
        if (networkedCountdown.Value > 0 && networkedPlayerCount.Value < minPlayersToStart)
        {
            networkedCountdown.Value = -1f;
            Debug.Log("⏸ Обратный отсчёт отменён - недостаточно игроков");
        }
    }

    private void CheckStartConditions()
    {
        if (networkedGameStarted.Value) return;

        if (networkedPlayerCount.Value >= minPlayersToStart)
        {
            // Запускаем обратный отсчёт
            networkedCountdown.Value = countdownTime;
            Debug.Log($"⏱ Начинаем обратный отсчёт: {countdownTime} секунд!");
        }
    }

    private void StartGameNow()
    {
        if (networkedGameStarted.Value) return;

        networkedGameStarted.Value = true;
        Debug.Log($"🎮 ИГРА НАЧАЛАСЬ! Игроков: {networkedPlayerCount.Value}");
    }

    // Callbacks для NetworkVariables
    private void OnPlayerCountChangedCallback(int oldValue, int newValue)
    {
        Debug.Log($"Игроков: {newValue}/{minPlayersToStart}");
        OnPlayerCountChanged?.Invoke(newValue);
        UpdateUI();
    }

    private void OnGameStartedCallback(bool oldValue, bool newValue)
    {
        if (newValue && !localGameStarted)
        {
            localGameStarted = true;
            Debug.Log("🎮 Игра началась для этого клиента!");
            
            HideWaitingScreen();
            OnGameStarted?.Invoke();
        }
    }

    private void OnCountdownChanged(float oldValue, float newValue)
    {
        UpdateUI();
    }

    // UI методы
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
        // Статус
        if (waitingStatusText != null)
        {
            if (networkedGameStarted.Value)
            {
                waitingStatusText.text = "Игра началась!";
            }
            else if (networkedCountdown.Value > 0)
            {
                waitingStatusText.text = "Игра скоро начнётся!";
            }
            else if (networkedPlayerCount.Value < minPlayersToStart)
            {
                waitingStatusText.text = "Ожидание игроков...";
            }
            else
            {
                waitingStatusText.text = "Готово!";
            }
        }

        // Счётчик игроков
        if (playerCountText != null)
        {
            playerCountText.text = $"Игроков: {networkedPlayerCount.Value}/{minPlayersToStart}";
        }

        // Обратный отсчёт
        if (countdownText != null)
        {
            if (networkedCountdown.Value > 0)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = Mathf.CeilToInt(networkedCountdown.Value).ToString();
            }
            else
            {
                countdownText.gameObject.SetActive(false);
            }
        }
    }

    // Публичные методы
    public bool IsGameStarted() => networkedGameStarted.Value;
    public int GetConnectedPlayers() => networkedPlayerCount.Value;
    public int GetMinPlayersRequired() => minPlayersToStart;
    public bool IsWaitingForPlayers() => !networkedGameStarted.Value && networkedPlayerCount.Value < minPlayersToStart;
}


