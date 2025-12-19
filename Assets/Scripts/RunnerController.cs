using System;
using UnityEngine;

public class RunnerController : IDisposable
{
    private readonly NetworkRunnerView _runnerView;
    private readonly InputController _inputController;

    public RunnerController(NetworkRunnerView runnerView, InputController inputController)
    {
        _runnerView = runnerView;
        _inputController = inputController;

        _runnerView.OnGrounded += MakeGrounded;
        _runnerView.OnGroundLeft += MakeUnGrounded;
        
        _inputController.OnJump += Jump;
        _inputController.OnRunLeft += RunLeft;
        _inputController.OnRunRight += RunRight;
    }

    private void MakeGrounded()
    {
        _runnerView.IsGrounded = true;
        _runnerView.IsJumped = false;
    }

    private void MakeUnGrounded()
    {
        _runnerView.IsGrounded = false;
        _runnerView.IsJumped = true;
    }

    private void Jump()
    {
        if (_runnerView.IsGrounded)
        {
            Debug.Log("Jump controller");
            _runnerView.Jump();
            _runnerView.IsJumped = true;
            _runnerView.IsGrounded = false;
        }
    }

    private void RunLeft()
    {
        if(_runnerView.IsGrounded) 
            _runnerView.RunLeft();
    }

    private void RunRight()
    {
        if(_runnerView.IsGrounded) 
            _runnerView.RunRight();
    }

    public void Dispose()
    {
        _runnerView.OnGrounded -= MakeGrounded;
        _runnerView.OnGroundLeft -= MakeUnGrounded;
        
        _inputController.OnJump -= Jump;
        _inputController.OnRunLeft -= RunLeft;
        _inputController.OnRunRight -= RunRight;
    }
}
