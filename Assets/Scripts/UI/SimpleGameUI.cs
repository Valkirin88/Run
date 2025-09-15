using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/// <summary>
/// Простой UI для игры с Unity Relay
/// Только самое необходимое: кнопка старт, статус, код игры
/// </summary>
public class SimpleGameUI : MonoBehaviour
{
    [Header("Main UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playersCountText;
    
    [Header("Game Info")]
    [SerializeField] private TextMeshProUGUI gameInfoText;

    private SimpleRelayManager relayManager;

    private void Start()
    {
        relayManager = FindObjectOfType<SimpleRelayManager>();
        SetupUI();
    }

    private void SetupUI()
    {
        // Кнопки уже настроены в SimpleRelayManager
        // Здесь только дополнительные элементы
    }

    private void Update()
    {
        UpdateGameInfo();
    }

    private void UpdateGameInfo()
    {
        if (gameInfoText == null || relayManager == null) return;

        string info = "";
        
        if (relayManager.IsHost())
        {
            info = $"Вы хост\nИгроки: {relayManager.GetCurrentPlayerCount()}/2\nОжидание 2-го игрока...";
        }
        else if (relayManager.IsConnected())
        {
            info = "Подключен к игре\nВы клиент";
        }
        else
        {
            info = "Не подключен\nНажмите 'Старт' для поиска игры";
        }
        
        gameInfoText.text = info;
    }

    // Вызывается когда игра началась
    public void OnGameStarted()
    {
        if (statusText != null)
            statusText.text = "Игра началась! Оба игрока подключились!";
    }

    // Показать уведомление
    public void ShowNotification(string message)
    {
        if (statusText != null)
            statusText.text = message;
        
        // Можно добавить временное уведомление
        CancelInvoke(nameof(ClearNotification));
        Invoke(nameof(ClearNotification), 3f);
    }

    private void ClearNotification()
    {
        if (statusText != null && relayManager != null)
        {
            if (relayManager.IsConnected())
                statusText.text = "В игре";
            else
                statusText.text = "Готово к игре!";
        }
    }
}
