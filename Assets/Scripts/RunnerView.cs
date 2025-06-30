using System;
using Unity.VisualScripting;
using UnityEngine;

public class RunnerView : MonoBehaviour
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
        Debug.Log("Jump");
        _rigidbody.AddForce(Vector3.up * _jumpVelocity);
        // var jumpPosition = new Vector3(transform.position.x, transform.position.y +1f, transform.position.z);
        // transform.position = Vector3.MoveTowards(transform.position, jumpPosition, _jumpVelocity);
    }
}
