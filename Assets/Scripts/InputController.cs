using System;
using UnityEngine;

public class InputController
{
    public event Action OnJump;
    public event Action OnRunLeft;
    public event Action OnRunRight;
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            OnJump?.Invoke();
        }

        if (Input.GetKey(KeyCode.A))
        {
            OnRunLeft?.Invoke();
        }

        if (Input.GetKey(KeyCode.D))
        {
            OnRunRight?.Invoke();
        }
    }
    
}
