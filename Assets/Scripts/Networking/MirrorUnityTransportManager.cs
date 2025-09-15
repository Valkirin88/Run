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
/// Менеджер для глобальных подключений через Unity Relay с Mirror + Unity Transport
/// Логика: первый нажавший "Старт" становится хостом, остальные подключаются как клиенты
/// ВАЖНО: Требует Mirror с Unity Transport поддержкой
/// </summary>
public class MirrorUnityTransportManager : NetworkManager
{
    [Header("Game Settings")]
    [SerializeField] private int maxPlayers = 2;
    
    [Header("UI References")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playersCountText;
    
    // Relay данные
    private Allocation hostAllocation;
    private string currentJoinCode;
    private bool isCreatingHost = false;
    
    // Ключи для PlayerPrefs (для тестирования в Editor)
    private const string RELAY_HOST_ACTIVE_KEY = "RELAY_HOST_ACTIVE";
    private const string RELAY_JOIN_CODE_KEY = "RELAY_JOIN_CODE";

    public override void Start()
    {
        base.Start();
        InitializeAsync();
    }

    /// <summary>
    /// Инициализация Unity Services
    /// </summary>
    private async void InitializeAsync()
    {
        try
        {
            UpdateStatus("Инициализация...");
            
            await UnityServices.InitializeAsync();
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("✅ Анонимная аутентификация успешна");
            }
            
            UpdateStatus("Нажмите СТАРТ для игры");
            startButton.onClick.AddListener(OnStartButtonPressed);
            
            Debug.Log("🎮 MirrorUnityTransportManager готов к работе");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Ошибка инициализации: {ex.Message}");
            Debug.LogError($"❌ Ошибка инициализации: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработка нажатия кнопки СТАРТ
    /// </summary>
    private async void OnStartButtonPressed()
    {
        startButton.interactable = false;
        
        try
        {
            #if UNITY_EDITOR
            // В Editor используем PlayerPrefs для тестирования
            if (HasActiveRelayHost())
            {
                // Есть активный хост, подключаемся как клиент
                string joinCode = GetRelayJoinCode();
                if (!string.IsNullOrEmpty(joinCode))
                {
                    Debug.Log($"🎯 Подключение к существующему хосту с кодом: {joinCode}");
                    JoinAsClientAsync(joinCode);
                    return;
                }
            }
            #endif
            
            // Пробуем найти существующий Relay хост
            // В реальной игре здесь был бы запрос к серверу матчмейкинга
            // Пока что просто создаем новый хост
            if (!isCreatingHost)
            {
                Debug.Log("🏠 Создание нового Relay хоста...");
                await CreateHostAsync();
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Ошибка: {ex.Message}");
            Debug.LogError($"❌ Ошибка при запуске: {ex.Message}");
            startButton.interactable = true;
        }
    }

    /// <summary>
    /// Создание Relay хоста
    /// </summary>
    private async Task CreateHostAsync()
    {
        if (isCreatingHost) return;
        isCreatingHost = true;
        
        try
        {
            UpdateStatus("Создание игры...");
            
            // Убеждаемся что предыдущие подключения закрыты
            if (NetworkServer.active || NetworkClient.isConnected)
            {
                Disconnect();
                await Task.Delay(1500); // Увеличена задержка
            }
            
            // Создаем Relay allocation
            hostAllocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);
            
            Debug.Log($"🎯 Unity Relay хост создан с кодом: {currentJoinCode}");
            
            // Настраиваем транспорт для Relay
            await SetupHostTransportAsync();
            
            #if UNITY_EDITOR
            // Сохраняем данные хоста для других экземпляров Editor
            SetRelayHostData(currentJoinCode);
            #endif
            
            // Дополнительная задержка перед запуском
            await Task.Delay(500);
            
            // Запускаем как хост
            StartHost();
            
            UpdateStatus($"Ожидание игроков... (1/{maxPlayers})");
            Debug.Log($"🏠 Unity Transport Relay хост запущен, код подключения: {currentJoinCode}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Ошибка создания игры: {ex.Message}");
            Debug.LogError($"❌ Ошибка создания Relay хоста: {ex.Message}");
            
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
                await Task.Delay(1000);
            }
            
            // Подключаемся к Relay
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            // Настраиваем транспорт для клиента
            await SetupClientTransportAsync(joinAllocation);
            
            // Дополнительная задержка перед запуском
            await Task.Delay(300);
            
            // Подключаемся как клиент
            StartClient();
            
            Debug.Log($"🎯 Подключение к Unity Transport Relay с кодом: {joinCode}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Ошибка подключения: {ex.Message}");
            Debug.LogError($"❌ Ошибка подключения к Relay: {ex.Message}");
        }
    }

    /// <summary>
    /// ВАЖНО: Этот метод требует Mirror с Unity Transport поддержкой!
    /// Если у вас стандартный Mirror с kcp2k, этот метод не будет работать.
    /// </summary>
    private async Task SetupHostTransportAsync()
    {
        Debug.LogWarning("⚠️ ВНИМАНИЕ: Этот метод требует Mirror с Unity Transport поддержкой!");
        Debug.LogWarning("⚠️ Если вы видите ошибки, значит Unity Transport для Mirror не установлен!");
        
        // TODO: Здесь должна быть настройка Unity Transport для Mirror
        // Пример для Netcode: transport.SetRelayServerData(...)
        // Для Mirror нужен аналогичный API или адаптер
        
        #if UNITY_EDITOR
        // В Editor используем обходной путь для тестирования
        Debug.Log("🛠️ Editor: симуляция настройки Unity Transport для хоста");
        await Task.Delay(100);
        #else
        Debug.LogError("❌ Настройка Unity Transport для Mirror в билде не реализована!");
        Debug.LogError("❌ Нужен Mirror с Unity Transport поддержкой или адаптер!");
        #endif
    }

    /// <summary>
    /// Настройка Unity Transport для клиента
    /// </summary>
    private async Task SetupClientTransportAsync(JoinAllocation joinAllocation)
    {
        Debug.LogWarning("⚠️ ВНИМАНИЕ: Этот метод требует Mirror с Unity Transport поддержкой!");
        
        #if UNITY_EDITOR
        // В Editor используем обходной путь для тестирования
        Debug.Log("🛠️ Editor: симуляция настройки Unity Transport для клиента");
        await Task.Delay(100);
        #else
        Debug.LogError("❌ Настройка Unity Transport для Mirror в билде не реализована!");
        Debug.LogError("❌ Нужен Mirror с Unity Transport поддержкой или адаптер!");
        #endif
    }

    #region Editor Testing Methods
    
    #if UNITY_EDITOR
    private bool HasActiveRelayHost()
    {
        return PlayerPrefs.HasKey(RELAY_HOST_ACTIVE_KEY) && PlayerPrefs.GetInt(RELAY_HOST_ACTIVE_KEY) == 1;
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
        Debug.Log($"💾 Editor: Сохранен код хоста: {joinCode}");
    }
    
    private void ClearRelayHostData()
    {
        PlayerPrefs.DeleteKey(RELAY_HOST_ACTIVE_KEY);
        PlayerPrefs.DeleteKey(RELAY_JOIN_CODE_KEY);
        PlayerPrefs.Save();
        Debug.Log("🗑️ Editor: Данные хоста очищены");
    }
    #endif
    
    #endregion

    #region Mirror Events

    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("🏠 Хост запущен");
        UpdatePlayersCount();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("👤 Клиент запущен");
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"👤 Игрок подключился: {conn.connectionId}");
        UpdatePlayersCount();
        
        if (NetworkServer.connections.Count >= maxPlayers)
        {
            UpdateStatus("Игра начинается!");
            StartGameForAll();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log($"👤 Игрок отключился: {conn.connectionId}");
        UpdatePlayersCount();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("✅ Успешно подключились к хосту");
        UpdateStatus("Подключено! Ожидание игроков...");
        
        // Проверяем статус игры через небольшую задержку
        StartCoroutine(CheckGameStatusDelayed());
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("❌ Отключились от хоста");
        UpdateStatus("Отключено");
        startButton.interactable = true;
    }

    public override void OnStopHost()
    {
        base.OnStopHost();
        Debug.Log("🏠 Хост остановлен");
        
        #if UNITY_EDITOR
        ClearRelayHostData();
        #endif
        
        UpdateStatus("Нажмите СТАРТ для игры");
        startButton.interactable = true;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("👤 Клиент остановлен");
        UpdateStatus("Нажмите СТАРТ для игры");
        startButton.interactable = true;
    }

    #endregion

    #region UI Updates

    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
        Debug.Log($"📱 Статус: {status}");
    }

    private void UpdatePlayersCount()
    {
        if (playersCountText != null)
        {
            int connectedPlayers = NetworkServer.active ? NetworkServer.connections.Count : 0;
            playersCountText.text = $"Игроки: {connectedPlayers}/{maxPlayers}";
        }
    }

    private void StartGameForAll()
    {
        Debug.Log("🎮 Начинаем игру для всех!");
        UpdateStatus("Игра началась!");
        
        // Здесь можно добавить логику начала игры
        // Например, загрузить игровую сцену или активировать игровые объекты
    }

    private IEnumerator CheckGameStatusDelayed()
    {
        yield return new WaitForSeconds(1f);
        
        if (NetworkClient.isConnected)
        {
            Debug.Log("🔍 Проверяем статус игры...");
            // Здесь можно добавить проверку статуса игры от сервера
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Отключение и очистка
    /// </summary>
    private async void Disconnect()
    {
        try
        {
            if (NetworkServer.active)
            {
                StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                StopClient();
            }
            
            // Даем время на корректное закрытие соединений
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Ошибка при отключении: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonPressed);
        }
        
        #if UNITY_EDITOR
        // Очищаем данные при уничтожении объекта в Editor
        if (NetworkServer.active)
        {
            ClearRelayHostData();
        }
        #endif
    }

    #endregion
}
