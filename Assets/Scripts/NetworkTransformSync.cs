using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Custom NetworkTransform для синхронизации позиции игрока с сервера на клиенты
/// Используется для плавной синхронизации Rigidbody объектов
/// </summary>
[DisallowMultipleComponent]
public class NetworkTransformSync : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        // Делаем owner authoritative для лучшей отзывчивости
        return false;
    }
}

