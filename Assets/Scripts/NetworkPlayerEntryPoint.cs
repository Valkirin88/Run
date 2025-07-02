using System;
using Mirror;
using UnityEngine;

public class NetworkPlayerEntryPoint : NetworkBehaviour
{
   [SerializeField]
   private RunnerView _runnerView;
   
   
   private InputController _inputController;
   private RunnerController _runnerController;

   public override void OnStartLocalPlayer()
   {
       _inputController = new InputController();
       _runnerController = new RunnerController(_runnerView, _inputController);
   }

   private void Update()
   {
       if (isLocalPlayer)
        _inputController.Update();
   }

   private void OnDestroy()
   {
       if (isLocalPlayer) 
           _runnerController.Dispose();
   }
}
