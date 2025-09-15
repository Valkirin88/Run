using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using System;
using System.Collections;

/// <summary>
/// Простой менеджер для глобальных подключений через Unity Relay
/// Логика: первый нажавший "Старт" становится хостом, остальные подключаются как клиенты
/// </summary>
public class SimpleRelayManager : NetworkManager
{
    [Header("Game Settings")]
    [SerializeField] private int maxPlayers = 2;
    
    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playersCountText;
    

    private Allocation hostAllocation;
    private bool isInitialized = false;
    private bool isSearchingForHost = false;
    private bool isCreatingHost = false;
    
    // Для тестирования в Unity Editor - используем PlayerPrefs вместо static
    private const string RELAY_HOST_ACTIVE_KEY = "RelayTest_HasActiveHost";
    private const string RELAY_JOIN_CODE_KEY = "RelayTest_JoinCode";

    public override async void Start()
    {
        base.Start();
        await InitializeRelay();
        SetupUI();
        UpdateUI();
    }

    /// <summary>
    /// Инициализация Unity Relay
    /// </summary>
    private async Task InitializeRelay()
    {
        try
        {
            UpdateStatus("Инициализация Unity Services...");
            
            await UnityServices.InitializeAsync();
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            
            isInitialized = true;
            UpdateStatus("Готово к игре!");
            Debug.Log("Unity Relay инициализирован");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Ошибка инициализации: {ex.Message}");
            Debug.LogError($"Ошибка инициализации Unity Relay: {ex.Message}");
        }
    }

    private void SetupUI()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClick);
    }

    /// <summary>
    /// Главная кнопка - логика "первый = хост"
    /// </summary>
    public void OnStartButtonClick()
    {
        if (!isInitialized)
        {
            UpdateStatus("Ожидание инициализации...");
            return;
        }

        if (NetworkClient.isConnected || NetworkServer.active)
        {
            // Уже подключен - отключаемся
            Disconnect();
            return;
        }

        // Пытаемся найти существующего хоста, если не находим - становимся хостом
        StartCoroutine(TryFindHostOrBecomeHost());
    }

    /// <summary>
    /// Попытка найти хоста, если не найден - становимся хостом
    /// </summary>
    private IEnumerator TryFindHostOrBecomeHost()
    {
        UpdateStatus("Поиск игры...");
        isSearchingForHost = true;

        // В Unity Editor используем симуляцию поиска
        #if UNITY_EDITOR
        bool hasHost = HasActiveRelayHost();
        string joinCode = GetRelayJoinCode();
        
        if (hasHost && !string.IsNullOrEmpty(joinCode))
        {
            Debug.Log($"🎯 НАЙДЕН АКТИВНЫЙ RELAY ХОСТ! Join код: {joinCode}");
            JoinAsClientAsync(joinCode);
            isSearchingForHost = false;
            yield break;
        }
        else
        {
            Debug.Log($"🔍 НЕТ АКТИВНОГО RELAY ХОСТА. hasActiveHost: {hasHost}, joinCode: '{joinCode}'");
        }
        #endif

        // Пытаемся найти доступную игру в течение 3 секунд
        yield return new WaitForSeconds(3f);
        
        if (isSearchingForHost && !NetworkClient.isConnected)
        {
            // Не нашли хоста - становимся хостом
            CreateHostAsync();
        }
        
        isSearchingForHost = false;
    }

    /// <summary>
    /// Создание Relay хоста
    /// </summary>
    private async void CreateHostAsync()
    {
        // Защита от повторных вызовов
        if (isCreatingHost)
        {
            Debug.Log("Хост уже создается, пропускаем повторный вызов");
            return;
        }
        
        isCreatingHost = true;
        
        try
        {
            UpdateStatus("Создание игры...");
            
            // Убеждаемся что предыдущие подключения закрыты
            if (NetworkServer.active || NetworkClient.isConnected)
            {
                Disconnect();
                await System.Threading.Tasks.Task.Delay(1500); // Увеличенное ожидание освобождения ресурсов
            }
            
            // Создаем Relay allocation для хоста (1 слот для второго игрока)
            hostAllocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            
            // Получаем join код для системы (не показываем пользователю)
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);
            
            // В Unity Editor сохраняем join код для других экземпляров
            #if UNITY_EDITOR
            SetRelayHostData(joinCode);
            Debug.Log($"🎮 RELAY ХОСТ СОЗДАН! Сохранен join код: {joinCode}");
            Debug.Log($"🔥 hasActiveHost = {HasActiveRelayHost()}, joinCode = '{joinCode}'");
            #endif
            
            // Настраиваем транспорт
            await SetupHostTransportAsync();
            
            // Дополнительная задержка перед запуском
            await System.Threading.Tasks.Task.Delay(500);
            
            // Запускаем хост
            StartHost();
            
            UpdateStatus("Ожидание игроков...");
            
            Debug.Log($"Relay хост создан. Ожидание игроков...");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Ошибка создания игры: {ex.Message}");
            Debug.LogError($"Ошибка создания Relay хоста: {ex.Message}");
            
            // При ошибке пробуем очистить состояние
            Disconnect();
        }
        finally
        {
            isCreatingHost = false;
        }
    }

    /// <summary>
    /// Подключение как клиент к Relay хосту
    /// </summary>
    private async void JoinAsClientAsync(string joinCode)
    {
        try
        {
            UpdateStatus($"Подключение к игре...");
            
            // Убеждаемся что предыдущие подключения закрыты
            if (NetworkServer.active || NetworkClient.isConnected)
            {
                Disconnect();
                await System.Threading.Tasks.Task.Delay(1000);
            }
            
            // Подключаемся к Relay
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            // Настраиваем транспорт для клиента
            await SetupClientTransportAsync(joinAllocation);
            
            // Дополнительная задержка перед запуском
            await System.Threading.Tasks.Task.Delay(300);
            
            // Подключаемся как клиент
            StartClient();
            
            Debug.Log($"Подключение к игре с кодом: {joinCode}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Ошибка подключения: {ex.Message}");
            Debug.LogError($"Ошибка подключения к Relay: {ex.Message}");
        }
    }

    /// <summary>
    /// Настройка транспорта для хоста
    /// </summary>
    private async System.Threading.Tasks.Task SetupHostTransportAsync()
    {
        Debug.LogWarning("⚠️ ВНИМАНИЕ: kcp2k транспорт НЕ ПОДДЕРЖИВАЕТ Unity Relay!");
        Debug.LogWarning("📖 Для Unity Relay используйте Unity Transport вместо kcp2k");
        
        // Для тестирования в Editor используем localhost
        #if UNITY_EDITOR
        var transport = GetComponent<kcp2k.KcpTransport>();
        if (transport != null)
        {
            // Принудительно останавливаем транспорт перед настройкой
            transport.ServerStop();
            transport.ClientDisconnect();
            
            // Увеличенная задержка для освобождения сокетов
            await System.Threading.Tasks.Task.Delay(300);
            
            // В Editor используем localhost вместо Relay
            networkAddress = "localhost";
            int port = UnityEngine.Random.Range(7000, 8000);
            transport.Port = (ushort)port;
            
            // Сохраняем данные хоста для клиентов
            PlayerPrefs.SetString("LocalTest_HostIP", "localhost");
            PlayerPrefs.SetInt("LocalTest_HostPort", port);
            PlayerPrefs.Save();
            
            Debug.Log($"🏠 EDITOR MODE: Хост на localhost:{port}");
        }
        #else
        // В продакшене этот код не будет работать с Relay
        Debug.LogError("❌ kcp2k НЕ РАБОТАЕТ с Unity Relay в продакшене!");
        Debug.LogError("🔧 Используйте Unity Transport для Relay или LocalTestManager для разработки");
        #endif
    }

    /// <summary>
    /// Настройка транспорта для клиента
    /// </summary>
    private async System.Threading.Tasks.Task SetupClientTransportAsync(JoinAllocation joinAllocation)
    {
        Debug.LogWarning("⚠️ ВНИМАНИЕ: kcp2k транспорт НЕ ПОДДЕРЖИВАЕТ Unity Relay!");
        
        // Для тестирования в Editor используем localhost
        #if UNITY_EDITOR
        var transport = GetComponent<kcp2k.KcpTransport>();
        if (transport != null)
        {
            // Принудительно останавливаем транспорт перед настройкой
            transport.ClientDisconnect();
            transport.ServerStop();
            
            // Увеличенная задержка для освобождения сокетов
            await System.Threading.Tasks.Task.Delay(200);
            
            // В Editor получаем данные хоста из PlayerPrefs
            string hostIP = PlayerPrefs.GetString("LocalTest_HostIP", "localhost");
            int hostPort = PlayerPrefs.GetInt("LocalTest_HostPort", 7777);
            
            networkAddress = hostIP;
            transport.Port = (ushort)hostPort;
            
            Debug.Log($"🔌 EDITOR MODE: Подключение к {hostIP}:{hostPort}");
        }
        #else
        // В продакшене этот код не будет работать с Relay
        Debug.LogError("❌ kcp2k НЕ РАБОТАЕТ с Unity Relay в продакшене!");
        #endif
    }

    /// <summary>
    /// Отключение от игры
    /// </summary>
    public async void Disconnect()
    {
        isSearchingForHost = false;
        isCreatingHost = false;
        
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            StopClient();
        }
        else if (NetworkServer.active)
        {
            StopServer();
        }
        
        // Очищаем все ресурсы
        hostAllocation = null;
        
        // Увеличенная задержка для освобождения сокетов Windows
        await System.Threading.Tasks.Task.Delay(1000);
    }

    // События Mirror
    public override void OnStartHost()
    {
        UpdateStatus($"Ожидание 2-го игрока... (1/{maxPlayers})");
        UpdateUI();
    }

    public override void OnClientConnect()
    {
        UpdateStatus("Подключен к игре!");
        UpdateUI();
        
        // Проверяем, не началась ли уже игра
        Invoke(nameof(CheckGameStatus), 1f);
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (NetworkServer.connections.Count > maxPlayers)
        {
            conn.Disconnect();
            return;
        }

        UpdatePlayersCount();
        Debug.Log($"Игрок подключился. Всего: {NetworkServer.connections.Count}");
        
        // Если собрались 2 игрока - начинаем игру
        if (NetworkServer.connections.Count >= maxPlayers)
        {
            StartGameForAll();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        UpdatePlayersCount();
        Debug.Log($"Игрок отключился. Всего: {NetworkServer.connections.Count - 1}");
    }

    public override void OnStopHost()
    {
        UpdateStatus("Игра остановлена");
        UpdateUI();
        hostAllocation = null;
        
        // В Unity Editor очищаем shared состояние
        #if UNITY_EDITOR
        ClearRelayHostData();
        Debug.Log("Editor: Очищены данные Relay хоста");
        #endif
        
        // Даем время на освобождение сокетов
        System.Threading.Thread.Sleep(100);
    }

    public override void OnStopClient()
    {
        UpdateStatus("Отключен от игры");
        UpdateUI();
        
        // Даем время на освобождение сокетов
        System.Threading.Thread.Sleep(100);
    }

    public override void OnClientError(TransportError error, string reason)
    {
        UpdateStatus($"Ошибка подключения: {reason}");
        
        if (isSearchingForHost)
        {
            // Если ошибка при поиске - становимся хостом  
            isSearchingForHost = false;
            CreateHostAsync();
        }
    }

    // Утилиты UI
    private void UpdateUI()
    {
        bool isActive = NetworkServer.active || NetworkClient.isConnected;
        
        if (startButton != null)
        {
            TextMeshProUGUI buttonText = startButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isActive ? "Покинуть игру" : "Старт";
            }
        }
    }

    private void UpdateStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
        Debug.Log($"Status: {status}");
    }

    private void UpdatePlayersCount()
    {
        if (playersCountText != null && NetworkServer.active)
        {
            int currentPlayers = NetworkServer.connections.Count;
            playersCountText.text = $"Игроки: {currentPlayers}/{maxPlayers}";
        }
    }

    /// <summary>
    /// Начать игру для всех игроков
    /// </summary>
    private void StartGameForAll()
    {
        UpdateStatus("Игра началась! Оба игрока подключились!");
        Debug.Log("Игра началась - оба игрока в сборе!");
        
        // Уведомляем UI о начале игры (только для хоста)
        var gameUI = FindObjectOfType<SimpleGameUI>();
        if (gameUI != null)
        {
            gameUI.OnGameStarted();
        }
        
        // Здесь можно добавить логику начала игры
        // Например, активировать игровые объекты, запустить таймер и т.д.
    }

    /// <summary>
    /// Проверка статуса игры для клиентов
    /// </summary>
    private void CheckGameStatus()
    {
        // Если мы клиент и в игре уже 4 игрока, значит игра началась
        if (NetworkClient.isConnected && !NetworkServer.active)
        {
            // Получаем количество игроков через Update метод хоста
            // Для простоты просто уведомляем UI
            var gameUI = FindObjectOfType<SimpleGameUI>();
            if (gameUI != null && GetCurrentPlayerCount() >= maxPlayers)
            {
                gameUI.OnGameStarted();
            }
        }
    }

    // Публичные методы
    public bool IsHost()
    {
        return NetworkServer.active;
    }

    public bool IsConnected()
    {
        return NetworkClient.isConnected || NetworkServer.active;
    }


    public int GetCurrentPlayerCount()
    {
        return NetworkServer.active ? NetworkServer.connections.Count : 0;
    }

    private void Update()
    {
        // Обновляем счетчик игроков
        if (NetworkServer.active)
        {
            UpdatePlayersCount();
        }
    }

    // Методы для работы с PlayerPrefs (shared между экземплярами Unity Editor)
    private bool HasActiveRelayHost()
    {
        return PlayerPrefs.GetInt(RELAY_HOST_ACTIVE_KEY, 0) == 1;
    }

    private string GetRelayJoinCode()
    {
        return PlayerPrefs.GetString(RELAY_JOIN_CODE_KEY, "");
    }

    private void SetRelayHostData(string joinCode)
    {
        PlayerPrefs.SetInt(RELAY_HOST_ACTIVE_KEY, 1);
        PlayerPrefs.SetString(RELAY_JOIN_CODE_KEY, joinCode);
        PlayerPrefs.Save();
        Debug.Log($"💾 СОХРАНЕН RELAY JOIN КОД: {joinCode}");
    }

    private void ClearRelayHostData()
    {
        PlayerPrefs.SetInt(RELAY_HOST_ACTIVE_KEY, 0);
        PlayerPrefs.SetString(RELAY_JOIN_CODE_KEY, "");
        PlayerPrefs.Save();
        Debug.Log("🗑️ ОЧИЩЕНЫ ДАННЫЕ RELAY ХОСТА");
    }
}
