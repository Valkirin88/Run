using Mirror;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFollowPlayer : NetworkBehaviour
{
    [SerializeField] 
    private CinemachineCamera _virtualCamera;

    public void Update()
    {
        if (NetworkClient.localPlayer != null)
        {
            // Назначаем камере себя как цель (только для локального игрока)
            _virtualCamera.Follow = NetworkClient.localPlayer.transform;
            _virtualCamera.LookAt = NetworkClient.localPlayer.transform;
        }
    }
}