using System;
using UnityEngine;

public class EntryPoint : MonoBehaviour
{
    
    private PlayerControllersManger playerControllersManger;


    private void Awake()
    {
        playerControllersManger = new PlayerControllersManger();
    }
}
