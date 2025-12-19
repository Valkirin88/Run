using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] private NetworkRunnerView _runnerView;
    
    private InputController _inputController;
    private RunnerController _runnerController;
    private bool _controlsEnabled = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            _inputController = new InputController();
            _runnerController = new RunnerController(_runnerView, _inputController);
            
            // Подписываемся на старт игры
            if (AutoStartGame.Instance != null)
            {
                AutoStartGame.Instance.OnGameStarted += EnableControls;
                
                // Если игра уже началась - сразу включаем управление
                if (AutoStartGame.Instance.IsGameStarted())
                {
                    EnableControls();
                }
                else
                {
                    Debug.Log("⏸ Управление отключено - ожидание игроков...");
                }
            }
            else
            {
                // Если нет AutoStartGame - управление сразу активно
                EnableControls();
            }
            
            Debug.Log("Local player spawned and initialized");
        }
        else
        {
            Debug.Log("Remote player spawned");
        }
    }

    private void EnableControls()
    {
        _controlsEnabled = true;
        Debug.Log("✅ Управление включено!");
    }

    private void Update()
    {
        // Управление работает только если игра началась
        if (IsOwner && _inputController != null && _controlsEnabled)
        {
            _inputController.Update();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner)
        {
            if (AutoStartGame.Instance != null)
            {
                AutoStartGame.Instance.OnGameStarted -= EnableControls;
            }
            
            if (_runnerController != null)
            {
                _runnerController.Dispose();
            }
        }
    }
}

