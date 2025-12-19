using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerControllersManger
{

    private List<NetworkPlayerController> _playerControllers;
    
    public PlayerControllersManger()
    {
        _playerControllers = new List<NetworkPlayerController>();
        FindAllPlayers();
    }

    private void FindAllPlayers()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = client.Key;
            NetworkObject playerObject = client.Value.PlayerObject;
    
            if (playerObject != null)
            {
                var controller = playerObject.GetComponent<NetworkPlayerController>();
                _playerControllers.Add(controller);
            }
        }
    }
}
