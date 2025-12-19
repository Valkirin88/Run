# Настройка сцены для мультиплеера

## 1. Настройка NetworkManager

1. Создайте пустой объект `NetworkManager`
2. Добавьте компоненты:
   - `NetworkManager`
   - `UnityTransport`
   - `NetworkRelayManager`
   - `AutoMatchmaking`
   - `SpawnPointManager`

### ⚠️ ВАЖНО - Настройки NetworkManager:
- **Player Prefab**: ОСТАВЬТЕ ПУСТЫМ! (спавн управляется AutoStartGame)
- **Network Prefabs List**: добавьте префаб игрока в список

---

## 2. Префаб игрока

На префаб игрока добавьте:
- `NetworkObject`
- `NetworkTransformSync` (или стандартный `NetworkTransform`)
- `NetworkPlayerController`
- `NetworkRunnerView`
- `PlayerAnimationSync` (если есть анимации)
- `Rigidbody`

---

## 3. UI - Панель матчмейкинга

Создайте Canvas с элементами:

```
Canvas
└── MatchmakingPanel
    ├── PlayButton (Button)
    ├── CancelButton (Button) 
    ├── StatusText (TextMeshPro)
    └── PlayerCountText (TextMeshPro)
```

Добавьте скрипт `MatchmakingUI` и назначьте элементы.

---

## 4. UI - Панель ожидания игроков

```
Canvas
└── WaitingPanel
    ├── StatusText (TextMeshPro) - "Ожидание игроков..."
    ├── PlayerCountText (TextMeshPro) - "Игроков: 0/2"
    └── CountdownText (TextMeshPro) - большой текст для цифр
```

---

## 5. AutoStartGame

Создайте объект `GameManager`:
1. Добавьте `NetworkObject`
2. Добавьте `AutoStartGame`

### Настройки AutoStartGame:
- **Min Players To Start**: 2
- **Countdown Time**: 3
- **Player Prefab**: перетащите сюда префаб игрока!
- Назначьте UI элементы из WaitingPanel

### Как это работает:
1. Игроки подключаются, но их префабы НЕ создаются
2. Когда подключились минимум 2 игрока → обратный отсчёт
3. После отсчёта → ВСЕ игроки спавнятся одновременно!

---

## 6. Точки спавна

### Вариант A - Заданные точки:
1. Создайте пустые объекты `SpawnPoint1`, `SpawnPoint2`, `SpawnPoint3`, `SpawnPoint4`
2. Расположите их в нужных местах
3. Перетащите в массив `Spawn Points` компонента `SpawnPointManager`

### Вариант B - Автоматические:
Оставьте массив `Spawn Points` пустым - игроки появятся по кругу.

---

## 7. Камера

1. Создайте `Cinemachine Camera`
2. Добавьте скрипт `CameraFollowPlayer`
3. Назначьте камеру в поле `Virtual Camera`

---

## 8. Земля

На все объекты земли добавьте компонент `Ground` (для определения приземления).

---

## 9. Unity Dashboard

1. Зайдите на https://cloud.unity.com
2. Создайте проект
3. Включите сервисы: **Relay**, **Lobby**, **Authentication**
4. В Unity: `Edit → Project Settings → Services` → привяжите проект

---

## Иерархия сцены

```
Scene
├── NetworkManager
│   ├── NetworkManager
│   ├── UnityTransport
│   ├── NetworkRelayManager
│   ├── AutoMatchmaking
│   └── SpawnPointManager
│
├── GameManager (с NetworkObject)
│   └── AutoStartGame
│
├── Canvas
│   ├── MatchmakingPanel
│   │   └── MatchmakingUI
│   └── WaitingPanel
│
├── CinemachineCamera
│   └── CameraFollowPlayer
│
├── SpawnPoint1
├── SpawnPoint2
├── SpawnPoint3
├── SpawnPoint4
│
├── Ground
│   └── Ground (скрипт)
│
└── Lights
```

---

## Тестирование

### В редакторе (Multiplayer Play Mode):
1. `Window → Multiplayer → Multiplayer Play Mode`
2. Включите 1 виртуального игрока
3. Нажмите Play - запустятся 2 экземпляра

### На устройствах:
1. Соберите APK
2. Установите на 2 устройства
3. Нажмите "Играть" на обоих - они найдут друг друга автоматически

