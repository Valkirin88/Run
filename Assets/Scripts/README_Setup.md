# 🌍 Настройка глобальной многопользовательской игры

Эта инструкция поможет настроить игру для подключения игроков **из любой точки мира** через Unity Relay.

## 📋 Требования

### Unity Packages (обязательно установить):
```
1. Mirror Networking (Asset Store или GitHub)
2. Unity Relay (Package Manager)
3. Unity Authentication (Package Manager)  
4. Unity Services Core (Package Manager)
5. Unity Transport (Package Manager)
```

### Unity Services настройка:
1. **Создайте Unity проект в Unity Dashboard**
2. **Получите Project ID** в Project Settings → Services
3. **Активируйте Relay Service** в Unity Dashboard

## 🛠 Настройка проекта

### 1. Установка пакетов
```
Window → Package Manager:
- Unity Registry → Authentication (Install)
- Unity Registry → Relay (Install)  
- Unity Registry → Services Core (Install)
- Unity Registry → Netcode for GameObjects (Install)
```

### 2. Настройка Unity Services
```csharp
Project Settings → Services:
- Project ID: [ваш project ID]
- Environment: Production
```

### 3. Настройка компонентов

#### Создайте GameObject "NetworkManager":
```csharp
Компоненты:
- RelayNetworkManager (вместо обычного NetworkManager)
- GlobalGameManager
- SimpleMatchmaker
- GameLobby
```

#### Настройте UI:
```csharp
Создайте Canvas с панелями:
- MainMenuPanel
- ConnectionModePanel  
- MatchmakingPanel
- LobbyPanel
```

## 🎮 Как это работает

### Для игроков:

#### 🌐 **Интернет-режим** (рекомендуется):
```
1. Игрок 1: "Создать комнату" → получает код (например: ABC123)
2. Игрок 2-4: вводят код ABC123 → подключаются из любой точки мира
3. Все готовы → игра начинается
```

#### 🏠 **Локальная сеть**:
```
1. Все устройства в одной Wi-Fi сети
2. Игрок 1: "Создать комнату" → становится хостом  
3. Остальные: автоматически находят и подключаются
```

## 📱 Сборка APK

### 1. Build Settings:
```
Platform: Android
- Minimum API Level: 21+
- Target API Level: 30+
- Scripting Backend: IL2CPP
- API Compatibility: .NET Standard 2.1
```

### 2. Player Settings:
```
Internet Access: Require
- Permissions: 
  ✅ Internet
  ✅ Network State
  ✅ Access Network State
```

### 3. Unity Services:
```
✅ Initialize Unity Services on Startup
✅ Analytics disabled (опционально)
✅ Crash Reporting disabled (опционально)
```

## 🔧 Настройка Relay лимитов

### Unity Dashboard → Relay:
```
- Concurrent Allocations: 100 (бесплатно)
- Max Players per Allocation: 4
- Bandwidth: 3GB/месяц (бесплатно)
```

## 💡 Альтернативные решения

Если Unity Relay недоступен:

### 1. **Photon PUN2/Fusion**:
```csharp
- 20 CCU бесплатно
- Легкая интеграция
- Готовый matchmaking
```

### 2. **Mirror + Dedicated Server**:
```csharp
- Арендуете VPS сервер
- Устанавливаете Linux build игры
- Игроки подключаются к серверу
```

### 3. **Steam Networking**:
```csharp
- Для игр в Steam
- P2P через Steam infrastructure
- Бесплатно для Steam игр
```

## 🚀 Пример готового кода

### Главный скрипт запуска:
```csharp
public class GameLauncher : MonoBehaviour
{
    private void Start()
    {
        // Автоматическая настройка
        var globalManager = FindObjectOfType<GlobalGameManager>();
        globalManager.ShowConnectionModeSelection();
    }
    
    public void QuickPlay()
    {
        // Быстрая игра - автоматически выбирает лучший режим
        FindObjectOfType<GlobalGameManager>().QuickPlay();
    }
}
```

### UI кнопки:
```csharp
// Главное меню
"Быстрая игра" → globalManager.QuickPlay()
"Создать комнату" → globalManager.CreateRoom()  
"Присоединиться" → globalManager.ShowMatchmaking()

// В игре
"Покинуть игру" → globalManager.LeaveCurrentSession()
```

## 🎯 Итоговый результат

После настройки ваша игра будет поддерживать:

✅ **Глобальные подключения** через Unity Relay  
✅ **Локальные подключения** в Wi-Fi сети  
✅ **Коды комнат** для легкого подключения друзей  
✅ **Автоматический matchmaking**  
✅ **До 4 игроков одновременно**  
✅ **Работу на Android/iOS/PC**  

Игроки смогут играть друг с другом **из любой точки мира** просто обменявшись 6-значным кодом комнаты! 🌍
