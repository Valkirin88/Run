using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Камера следует за локальным игроком
/// </summary>
public class CameraFollowPlayer : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _virtualCamera;

    public void SetTarget(Transform newTarget)
    {
        if (_virtualCamera != null && newTarget != null)
        {
            _virtualCamera.Follow = newTarget;
            _virtualCamera.LookAt = newTarget;
        }
    }
}
