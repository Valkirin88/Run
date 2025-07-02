using System;
using Mirror;
using UnityEngine;

public class RunnerView : NetworkBehaviour
{
    public event Action OnGrounded;
    public event Action OnGroundLeft;
    
    public bool IsGrounded;
    public bool IsJumped;
    
    [SerializeField]
    private Rigidbody _rigidbody;

    [SerializeField]
    private float _directVelocity;

    [SerializeField]
    private float _jumpVelocity;
    
    [SerializeField]
    private float _horizontalVelocity;

    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.GetComponent<Ground>())
        {
            Debug.Log("grounded");
            OnGrounded?.Invoke();
        }
    }

    private void OnCollisionExit(Collision other)
    {
        OnGroundLeft?.Invoke();
    }

    private void FixedUpdate()
    {
        _rigidbody.AddForce(Vector3.forward * _directVelocity, ForceMode.Acceleration);
    }

    public void RunRight()
    {
        _rigidbody.AddForce(Vector3.right * _horizontalVelocity, ForceMode.Acceleration);
    }

    public void RunLeft()
    {
        _rigidbody.AddForce(Vector3.left * _horizontalVelocity, ForceMode.Acceleration);
    }

    public void Jump()
    {
        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.y = 0f;
        _rigidbody.linearVelocity = velocity;
         
        _rigidbody.AddForce(Vector3.up * _jumpVelocity);
    }
}
