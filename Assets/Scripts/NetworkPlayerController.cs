using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] private NetworkRunnerView _runnerView;
    
    private InputController _inputController;
    private RunnerController _runnerController;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            _inputController = new InputController();
            _runnerController = new RunnerController(_runnerView, _inputController);
            Debug.Log("✅ Игрок заспавнен и готов к игре!");
        }
    }

    private void Update()
    {
        if (IsOwner && _inputController != null)
        {
            _inputController.Update();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner && _runnerController != null)
        {
            _runnerController.Dispose();
        }
    }
}

