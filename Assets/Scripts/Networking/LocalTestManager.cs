using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Упрощенный менеджер для локального тестирования в Unity Editor
/// Без Unity Relay - только для проверки работы matchmaking
/// </summary>
public class LocalTestManager : NetworkManager
{
    [Header("Game Settings")]
    [SerializeField] private int maxPlayers = 2;
    
    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playersCountText;
    
    // Для тестирования в Unity Editor - используем PlayerPrefs вместо static
    private bool isSearchingForHost = false;
    
    // Ключи для PlayerPrefs (shared между экземплярами)
    private const string HOST_ACTIVE_KEY = "MultiplayerTest_HasActiveHost";
    private const string HOST_IP_KEY = "MultiplayerTest_HostIP";
    private const string HOST_PORT_KEY = "MultiplayerTest_HostPort";

    public override void Start()
    {
        base.Start();
        SetupUI();
        UpdateUI();
        UpdateStatus("Готово к игре!");
        
        // Очищаем старые данные при первом запуске
        if (!HasActiveHost())
        {
            ClearHostData();
        }
        
        Debug.Log($"🚀 СТАРТ ЛОКАЛЬНОГО МЕНЕДЖЕРА. Активный хост: {HasActiveHost()}");
    }

    private void SetupUI()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
            Debug.Log("🔗 Кнопка старт подключена к LocalTestManager");
        }
        else
        {
            Debug.LogError("❌ Start button НЕ НАЗНАЧЕН в Inspector!");
        }
    }

    public void OnStartButtonClick()
    {
        Debug.Log("🚀 КНОПКА СТАРТ НАЖАТА В ЛОКАЛЬНОМ РЕЖИМЕ!");
        
        if (NetworkClient.isConnected || NetworkServer.active)
        {
            Debug.Log("Уже подключен - отключаемся");
            Disconnect();
            return;
        }

        // Пытаемся найти хоста или стать хостом
        StartCoroutine(TryFindHostOrBecomeHostLocal());
    }

    private IEnumerator TryFindHostOrBecomeHostLocal()
    {
        UpdateStatus("Поиск игры...");
        isSearchingForHost = true;

        bool hasHost = HasActiveHost();
        string hostIP = GetHostIP();
        int hostPort = GetHostPort();
        
        Debug.Log($"🔍 ПОИСК ХОСТА: hasActiveHost = {hasHost}, hostIP = '{hostIP}', port = {hostPort}");

        // Проверяем есть ли активный хост
        if (hasHost && !string.IsNullOrEmpty(hostIP))
        {
            Debug.Log($"🎯 НАЙДЕН ЛОКАЛЬНЫЙ ХОСТ: {hostIP}:{hostPort}");
            ConnectToLocalHost(hostIP, hostPort);
            isSearchingForHost = false;
            yield break;
        }

        // Ждем 2 секунды на поиск
        yield return new WaitForSeconds(2f);
        
        if (isSearchingForHost && !NetworkClient.isConnected)
        {
            Debug.Log("🎮 НЕ НАЙДЕН ХОСТ - СТАНОВИМСЯ ХОСТОМ");
            CreateLocalHost();
        }
        
        isSearchingForHost = false;
    }

    private void CreateLocalHost()
    {
        try
        {
            UpdateStatus("Создание локальной игры...");
            
            // Найдем свободный порт
            int port = FindAvailablePort();
            
            // Устанавливаем localhost
            networkAddress = "localhost";
            
            // Используем найденный порт
            var transport = GetComponent<kcp2k.KcpTransport>();
            if (transport != null)
            {
                transport.Port = (ushort)port;
                Debug.Log($"🎮 СОЗДАЕМ ХОСТ НА ПОРТУ: {port}");
            }
            
            // Сохраняем информацию о хосте для других экземпляров
            SetHostData("localhost", port);
            
            // Запускаем хост
            StartHost();
            
            Debug.Log("🎉 ЛОКАЛЬНЫЙ ХОСТ СОЗДАН УСПЕШНО!");
        }
        catch (System.Exception ex)
        {
            UpdateStatus($"Ошибка создания хоста: {ex.Message}");
            Debug.LogError($"❌ Ошибка создания локального хоста: {ex.Message}");
        }
    }

    private void ConnectToLocalHost(string ip, int port)
    {
        try
        {
            UpdateStatus("Подключение к локальной игре...");
            
            // Устанавливаем адрес хоста
            networkAddress = ip;
            
            // Используем порт хоста
            var transport = GetComponent<kcp2k.KcpTransport>();
            if (transport != null)
            {
                transport.Port = (ushort)port;
                Debug.Log($"🔌 ПОДКЛЮЧАЕМСЯ К ХОСТУ: {ip}:{port}");
            }
            
            // Подключаемся как клиент
            StartClient();
            
            Debug.Log($"🎯 ПОДКЛЮЧЕНИЕ К ЛОКАЛЬНОМУ ХОСТУ: {ip}:{port}");
        }
        catch (System.Exception ex)
        {
            UpdateStatus($"Ошибка подключения: {ex.Message}");
            Debug.LogError($"❌ Ошибка подключения к локальному хосту: {ex.Message}");
        }
    }

    private void Disconnect()
    {
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
    }

    // События Mirror
    public override void OnStartHost()
    {
        UpdateStatus($"Ожидание 2-го игрока... (1/{maxPlayers})");
        UpdateUI();
        Debug.Log("🎮 ХОСТ ЗАПУЩЕН - ОЖИДАНИЕ ИГРОКОВ");
    }

    public override void OnClientConnect()
    {
        UpdateStatus("Подключен к игре!");
        UpdateUI();
        Debug.Log("🎯 КЛИЕНТ ПОДКЛЮЧЕН К ХОСТУ");
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (NetworkServer.connections.Count > maxPlayers)
        {
            conn.Disconnect();
            return;
        }

        UpdatePlayersCount();
        Debug.Log($"🎉 ИГРОК ПОДКЛЮЧИЛСЯ! Всего: {NetworkServer.connections.Count}");
        
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
        Debug.Log($"😞 ИГРОК ОТКЛЮЧИЛСЯ. Всего: {NetworkServer.connections.Count - 1}");
    }

    public override void OnStopHost()
    {
        UpdateStatus("Игра остановлена");
        UpdateUI();
        
        // Очищаем информацию о хосте
        ClearHostData();
        
        Debug.Log("🛑 ХОСТ ОСТАНОВЛЕН - ОЧИЩЕН СТАТУС");
    }

    public override void OnStopClient()
    {
        UpdateStatus("Отключен от игры");
        UpdateUI();
        Debug.Log("🔌 КЛИЕНТ ОТКЛЮЧЕН");
    }

    public override void OnClientError(TransportError error, string reason)
    {
        UpdateStatus($"Ошибка подключения: {reason}");
        Debug.LogError($"❌ ОШИБКА КЛИЕНТА: {error} - {reason}");
        
        if (isSearchingForHost)
        {
            // Если ошибка при поиске - становимся хостом
            isSearchingForHost = false;
            CreateLocalHost();
        }
    }

    private void StartGameForAll()
    {
        UpdateStatus("Игра началась! Оба игрока подключились!");
        Debug.Log("🎉 ИГРА НАЧАЛАСЬ - ОБА ИГРОКА В СБОРЕ!");
        
        // Уведомляем UI
        var gameUI = FindObjectOfType<SimpleGameUI>();
        if (gameUI != null)
        {
            gameUI.OnGameStarted();
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
                buttonText.text = isActive ? "Покинуть игру" : "Старт (Локально)";
            }
        }
    }

    private void UpdateStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
        Debug.Log($"📊 СТАТУС: {status}");
    }

    private void UpdatePlayersCount()
    {
        if (playersCountText != null && NetworkServer.active)
        {
            int currentPlayers = NetworkServer.connections.Count;
            playersCountText.text = $"Игроки: {currentPlayers}/{maxPlayers}";
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
        if (NetworkServer.active)
        {
            UpdatePlayersCount();
        }
    }

    // Методы для работы с PlayerPrefs (shared между экземплярами)
    private bool HasActiveHost()
    {
        return PlayerPrefs.GetInt(HOST_ACTIVE_KEY, 0) == 1;
    }

    private string GetHostIP()
    {
        return PlayerPrefs.GetString(HOST_IP_KEY, "");
    }

    private int GetHostPort()
    {
        return PlayerPrefs.GetInt(HOST_PORT_KEY, 7777);
    }

    private void SetHostData(string ip, int port)
    {
        PlayerPrefs.SetInt(HOST_ACTIVE_KEY, 1);
        PlayerPrefs.SetString(HOST_IP_KEY, ip);
        PlayerPrefs.SetInt(HOST_PORT_KEY, port);
        PlayerPrefs.Save();
        Debug.Log($"💾 СОХРАНЕНЫ ДАННЫЕ ХОСТА: {ip}:{port}");
    }

    private void ClearHostData()
    {
        PlayerPrefs.SetInt(HOST_ACTIVE_KEY, 0);
        PlayerPrefs.SetString(HOST_IP_KEY, "");
        PlayerPrefs.SetInt(HOST_PORT_KEY, 7777);
        PlayerPrefs.Save();
        Debug.Log("🗑️ ОЧИЩЕНЫ ДАННЫЕ ХОСТА");
    }

    private int FindAvailablePort()
    {
        // Начинаем с порта 7777 и ищем свободный
        for (int port = 7777; port < 7800; port++)
        {
            if (IsPortAvailable(port))
            {
                Debug.Log($"🔍 НАЙДЕН СВОБОДНЫЙ ПОРТ: {port}");
                return port;
            }
        }
        
        // Если не нашли, используем случайный
        int randomPort = UnityEngine.Random.Range(8000, 9000);
        Debug.Log($"🎲 ИСПОЛЬЗУЕМ СЛУЧАЙНЫЙ ПОРТ: {randomPort}");
        return randomPort;
    }

    private bool IsPortAvailable(int port)
    {
        try
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
