using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Простой UI для автоматического matchmaking
/// Одна кнопка "Играть" - и всё!
/// </summary>
public class MatchmakingUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playerCountText;

    private void Start()
    {
        // Setup buttons
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
            cancelButton.gameObject.SetActive(false);
        }

        // Subscribe to events
        if (AutoMatchmaking.Instance != null)
        {
            AutoMatchmaking.Instance.OnStatusChanged += OnStatusChanged;
            AutoMatchmaking.Instance.OnPlayerCountChanged += OnPlayerCountChanged;
        }

        UpdateUI("Готов к игре", 0);
    }

    private void OnDestroy()
    {
        if (AutoMatchmaking.Instance != null)
        {
            AutoMatchmaking.Instance.OnStatusChanged -= OnStatusChanged;
            AutoMatchmaking.Instance.OnPlayerCountChanged -= OnPlayerCountChanged;
        }
    }

    private void OnPlayClicked()
    {
        if (AutoMatchmaking.Instance != null)
        {
            AutoMatchmaking.Instance.StartQuickMatch();
            
            // Show cancel button during search
            if (playButton != null)
                playButton.gameObject.SetActive(false);
            
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(true);
        }
    }

    private async void OnCancelClicked()
    {
        if (AutoMatchmaking.Instance != null)
        {
            await AutoMatchmaking.Instance.LeaveLobby();
            
            // Show play button again
            if (playButton != null)
                playButton.gameObject.SetActive(true);
            
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(false);
                
            UpdateUI("Готов к игре", 0);
        }
    }

    private void OnStatusChanged(string status)
    {
        if (statusText != null)
            statusText.text = status;

        // Hide menu when game starts
        if (status.Contains("Подключено") || status.Contains("игра"))
        {
            Invoke(nameof(HideMenu), 2f);
        }
    }

    private void OnPlayerCountChanged(int count)
    {
        if (playerCountText != null)
        {
            int maxPlayers = 4; // Can get from AutoMatchmaking if needed
            playerCountText.text = $"Игроков: {count}/{maxPlayers}";
        }
    }

    private void UpdateUI(string status, int playerCount)
    {
        if (statusText != null)
            statusText.text = status;

        if (playerCountText != null)
            playerCountText.text = $"Игроков: {playerCount}/4";
    }

    private void HideMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }
}

