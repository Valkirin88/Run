using System;
using UnityEngine;

public class EntryPoint : MonoBehaviour
{
   [SerializeField]
   private RunnerView _runnerView;
   
   private InputController _inputController;
   private RunnerController _runnerController;

   private void Awake()
   {
      _inputController = new InputController();
      _runnerController = new RunnerController(_runnerView, _inputController);
   }

   private void Update()
   {
      _inputController.Update();
   }
}
