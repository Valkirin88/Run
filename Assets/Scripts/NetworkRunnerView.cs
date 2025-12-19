using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkRunnerView : NetworkBehaviour
{
    public event Action OnGrounded;
    public event Action OnGroundLeft;
    
    public bool IsGrounded;
    public bool IsJumped;
    
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private float _directVelocity = 5f;
    [SerializeField] private float _jumpVelocity = 10f;
    [SerializeField] private float _horizontalVelocity = 5f;
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private PlayerAnimationSync animationSync;
    

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
          
            // Enable camera follow for local player
            var camera = FindFirstObjectByType<CameraFollowPlayer>();
            if (camera != null)
            {
                camera.SetTarget(transform);
            }
        }
        
    }
    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.GetComponent<Ground>())
        {
            IsGrounded = true;
            IsJumped = false;
            OnGrounded?.Invoke();
            
            if (animationSync != null)
                animationSync.SetGrounded(true);
        }
    }

    private void OnCollisionExit(Collision other)
    {
        IsGrounded = false;
        OnGroundLeft?.Invoke();
        
        if (animationSync != null)
            animationSync.SetGrounded(false);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        
        _rigidbody.AddForce(Vector3.forward * _directVelocity, ForceMode.Acceleration);
    }

    public void RunRight()
    {
        if (!IsOwner) return;
        _rigidbody.AddForce(Vector3.right * _horizontalVelocity, ForceMode.Acceleration);
    }

    public void RunLeft()
    {
        if (!IsOwner) return;
        _rigidbody.AddForce(Vector3.left * _horizontalVelocity, ForceMode.Acceleration);
    }

    public void Jump()
    {
        if (!IsOwner) return;
        
        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.y = 0f;
        _rigidbody.linearVelocity = velocity;
         
        _rigidbody.AddForce(Vector3.up * _jumpVelocity, ForceMode.Impulse);
        
        IsJumped = true;
        if (animationSync != null)
            animationSync.SetJumping(true);
    }
}

