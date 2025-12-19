using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Lobby = Unity.Services.Lobbies.Models.Lobby;

/// <summary>
/// Автоматический matchmaking: просто нажимаем "Играть" и система находит игру
/// Без кодов! Как в обычных онлайн играх
/// </summary>
public class AutoMatchmaking : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkRelayManager relayManager;

    [Header("Settings")]
    [SerializeField] private int maxPlayersPerLobby = 4;
    [SerializeField] private float lobbyUpdateInterval = 1.5f;
    [SerializeField] private string gameMode = "FreeForAll";

    public event Action<string> OnStatusChanged;
    public event Action<int> OnPlayerCountChanged;

    private Lobby currentLobby;
    private bool isSearching = false;
    private float lastLobbyUpdate;

    private async void Start()
    {
        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            // Ждем инициализации если она уже идет
            while (UnityServices.State == ServicesInitializationState.Initializing)
            {
                await Task.Delay(100);
            }

            // Инициализируем если еще не инициализировано
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("Unity Services initialized");
            }

            // Проверяем авторизацию
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"Already signed in as: {AuthenticationService.Instance.PlayerId}");
                UpdateStatus("Готов к игре");
                return;
            }

            // Ждем если уже идет процесс входа
            int attempts = 0;
            while (!AuthenticationService.Instance.IsSignedIn && attempts < 30)
            {
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    break;
                }
                catch (AuthenticationException)
                {
                    // Возможно уже идет вход - подождем
                    await Task.Delay(200);
                    attempts++;
                    
                    if (AuthenticationService.Instance.IsSignedIn)
                        break;
                }
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"Signed in as: {AuthenticationService.Instance.PlayerId}");
                UpdateStatus("Готов к игре");
            }
            else
            {
                UpdateStatus("Ошибка авторизации");
            }
        }
        catch (Exception e)
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"Already signed in as: {AuthenticationService.Instance.PlayerId}");
                UpdateStatus("Готов к игре");
                return;
            }
            Debug.LogError($"Failed to initialize Unity Services: {e}");
            UpdateStatus("Ошибка инициализации");
        }
    }

    /// <summary>
    /// Главный метод - начать поиск игры (Quick Match)
    /// </summary>
    public async void StartQuickMatch()
    {
        if (isSearching) return;

        isSearching = true;
        UpdateStatus("Поиск игры...");

        try
        {
            // Сначала пробуем найти существующее лобби
            Lobby foundLobby = await FindAvailableLobby();

            if (foundLobby != null)
            {
                // Нашли лобби - подключаемся
                await JoinLobby(foundLobby.Id);
            }
            else
            {
                // Не нашли - создаем свое
                await CreateLobby();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Quick match failed: {e}");
            UpdateStatus("Ошибка поиска");
            isSearching = false;
        }
    }

    private async Task<Lobby> FindAvailableLobby()
    {
        try
        {
            // Небольшая задержка чтобы лобби успело создаться
            await Task.Delay(500);
            
            // Настраиваем фильтры для поиска
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = 10, // Проверяем первые 10 лобби
                Filters = new List<QueryFilter>
                {
                    // Ищем только лобби с свободными местами
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            Debug.Log("Ищу доступные лобби...");
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            Debug.Log($"Найдено лобби: {queryResponse.Results.Count}");
            
            if (queryResponse.Results.Count > 0)
            {
                // Берем первое доступное лобби
                var lobby = queryResponse.Results[0];
                Debug.Log($"✅ Найдено лобби: {lobby.Name}, игроков: {lobby.Players.Count}/{lobby.MaxPlayers}");
                return lobby;
            }

            Debug.Log("❌ Нет доступных лобби - создаю новое");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error finding lobby: {e}");
            return null;
        }
    }

    private async Task CreateLobby()
    {
        try
        {
            UpdateStatus("Создание игры...");
            Debug.Log("🎮 Создаю новое лобби...");

            // Настройки лобби
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false, // Публичное лобби
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
                }
            };

            // Создаем лобби
            string lobbyName = $"Game_{UnityEngine.Random.Range(1000, 9999)}";
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayersPerLobby, options);
            
            Debug.Log($"✅ Лобби создано: {currentLobby.Name} (ID: {currentLobby.Id})");

            Debug.Log($"Лобби создано: {currentLobby.Id}");

            // Создаем Relay allocation
            string joinCode = await relayManager.CreateRelay();

            if (!string.IsNullOrEmpty(joinCode))
            {
                // Сохраняем Join Code в лобби
                await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                    }
                });

                UpdateStatus("Ожидание игроков...");
                StartLobbyHeartbeat();
                StartLobbyPolling();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating lobby: {e}");
            UpdateStatus("Ошибка создания игры");
            isSearching = false;
        }
    }

    private async Task JoinLobby(string lobbyId)
    {
        try
        {
            UpdateStatus("Подключение...");
            Debug.Log($"🔗 Подключаюсь к лобби: {lobbyId}");

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log($"✅ Подключился к лобби: {currentLobby.Name} (ID: {currentLobby.Id})");
            Debug.Log($"📊 Игроков в лобби: {currentLobby.Players.Count}/{currentLobby.MaxPlayers}");

            // Ждем пока RelayJoinCode появится в данных лобби
            int attempts = 0;
            while (!currentLobby.Data.ContainsKey("RelayJoinCode") && attempts < 10)
            {
                Debug.Log($"⏳ Ожидаю RelayJoinCode... попытка {attempts + 1}");
                await Task.Delay(500);
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                attempts++;
            }

            // Получаем Relay Join Code из лобби
            if (currentLobby.Data.TryGetValue("RelayJoinCode", out DataObject relayCodeObj))
            {
                string relayJoinCode = relayCodeObj.Value;
                Debug.Log($"🔑 Получен RelayJoinCode: {relayJoinCode}");
                
                bool success = await relayManager.JoinRelay(relayJoinCode);

                if (success)
                {
                    Debug.Log("✅ Успешно подключился к Relay!");
                    UpdateStatus("Подключено!");
                    StartLobbyPolling();
                }
                else
                {
                    Debug.LogError("❌ Ошибка подключения к Relay");
                    UpdateStatus("Ошибка подключения к Relay");
                    await LeaveLobby();
                }
            }
            else
            {
                Debug.LogError("❌ RelayJoinCode не найден в данных лобби!");
                UpdateStatus("Ошибка: нет кода подключения");
                await LeaveLobby();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error joining lobby: {e}");
            UpdateStatus("Ошибка подключения");
            isSearching = false;
        }
    }

    private async void StartLobbyHeartbeat()
    {
        // Отправляем heartbeat каждые 15 секунд чтобы лобби не закрылось
        while (currentLobby != null)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                await Task.Delay(15000);
            }
            catch (Exception e)
            {
                Debug.LogError($"Heartbeat failed: {e}");
                break;
            }
        }
    }

    private async void StartLobbyPolling()
    {
        // Периодически обновляем информацию о лобби
        while (currentLobby != null && isSearching)
        {
            await Task.Delay((int)(lobbyUpdateInterval * 1000));

            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                OnPlayerCountChanged?.Invoke(currentLobby.Players.Count);

                Debug.Log($"Игроков в лобби: {currentLobby.Players.Count}/{currentLobby.MaxPlayers}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Lobby polling failed: {e}");
                break;
            }
        }
    }

    public async Task LeaveLobby()
    {
        if (currentLobby == null) return;

        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            Debug.Log("Покинул лобби");

            currentLobby = null;
            isSearching = false;
            UpdateStatus("Отключен");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error leaving lobby: {e}");
        }
    }

    private void UpdateStatus(string status)
    {
        Debug.Log($"Status: {status}");
        OnStatusChanged?.Invoke(status);
    }

    public bool IsSearching() => isSearching;
    public Lobby GetCurrentLobby() => currentLobby;

    private void OnDestroy()
    {
        if (currentLobby != null)
        {
            _ = LeaveLobby(); // Fire and forget - объект уничтожается
        }
    }

    private void OnApplicationQuit()
    {
        if (currentLobby != null)
        {
            _ = LeaveLobby(); // Fire and forget - приложение закрывается
        }
    }
}

