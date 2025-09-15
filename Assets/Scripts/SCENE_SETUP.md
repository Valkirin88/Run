# 🎮 Настройка сцены для автоматического matchmaking

## 📋 Компоненты которые должны быть на сцене

### 1. 🌐 **GameObject "NetworkManager"**
```
Компоненты:
├── SimpleRelayManager (Script)
└── kcp2k Transport (или другой Mirror транспорт)

Настройки SimpleRelayManager:
- Max Players: 2
- Player Prefab: [ваш префаб игрока]
- Start Button: [ссылка на кнопку UI]
- Status Text: [ссылка на текст статуса]
- Players Count Text: [ссылка на счетчик игроков]
- Game Info Text: [ссылка на информацию об игре]
```

### 2. 🖼️ **Canvas "UI"**
```
Компоненты:
├── Canvas
├── Canvas Scaler
├── Graphic Raycaster
└── SimpleGameUI (Script)

Дочерние объекты:
├── StartButton (Button с TextMeshPro - UGUI)
├── StatusText (TextMeshPro - UGUI)
├── PlayersCountText (TextMeshPro - UGUI)
└── GameInfoText (TextMeshPro - UGUI)
```

### 3. 🎮 **Player Prefab (в Assets)**
```
Компоненты игрока:
├── Network Identity
├── Network Transform
├── RunnerController (ваш скрипт управления)
├── RunnerView (ваш скрипт отображения)
└── CameraFollowPlayer (уже есть)

Настройки Network Identity:
✅ Server Only: false
✅ Local Player Authority: true
```

## 🔧 Пошаговая настройка

### Шаг 1: Создать NetworkManager
```
1. Create Empty GameObject → "NetworkManager"
2. Add Component → SimpleRelayManager
3. Add Component → kcp2k Transport (или Mirror Transport)
4. В Inspector настроить:
   - Max Players: 4
   - Network Address: localhost (по умолчанию)
   - Player Prefab: [перетащить префаб игрока]
```

### Шаг 2: Создать UI
```
1. Create → UI → Canvas
2. Add Component к Canvas → SimpleGameUI
3. Создать дочерние UI элементы:

StartButton:
- Create → UI → Button - TextMeshPro
- Text: "Старт"
- OnClick: не настраивать (SimpleRelayManager сделает сам)

StatusText:
- Create → UI → Text - TextMeshPro
- Text: "Готово к игре!"
- Font Size: 24

PlayersCountText:
- Create → UI → Text - TextMeshPro
- Text: "Игроки: 0/2"
- Font Size: 18

GameInfoText:
- Create → UI → Text - TextMeshPro
- Text: "Нажмите 'Старт' для поиска игры"
- Font Size: 16
```

### Шаг 3: Связать компоненты
```
В SimpleRelayManager:
- Start Button → перетащить кнопку StartButton
- Status Text → перетащить StatusText
- Players Count Text → перетащить PlayersCountText

В SimpleGameUI:
- Status Text → перетащить StatusText  
- Players Count Text → перетащить PlayersCountText
- Game Info Text → перетащить GameInfoText
```

### Шаг 4: Настроить Player Prefab
```
1. Создать префаб игрока из вашего RunnerController
2. Добавить компоненты:
   - Network Identity (обязательно!)
   - Network Transform (для синхронизации позиции)
3. В Network Identity:
   ✅ Local Player Authority: true
4. Сохранить как префаб
5. Перетащить в Player Prefab в NetworkManager
```

## 📱 Структура сцены

```
Scene "MultiplayerGame"
├── 🌐 NetworkManager
│   ├── SimpleRelayManager
│   └── kcp2k Transport
├── 🖼️ Canvas
│   ├── SimpleGameUI
│   ├── StartButton
│   ├── StatusText
│   ├── PlayersCountText
│   └── GameInfoText
├── 🎮 Main Camera
├── 🌍 Ground (ваши игровые объекты)
└── 💡 Lighting, etc.
```

## ⚙️ Настройки Unity Services

### Project Settings:
```
Services:
✅ Initialize Unity Services on Startup
📝 Project ID: [ваш Unity Project ID]
```

### Package Manager:
```
Установленные пакеты:
✅ Unity Relay
✅ Unity Authentication  
✅ Unity Services Core
✅ Mirror Networking
```

## 🎯 Финальная проверка

### Checklist перед запуском:
```
✅ NetworkManager с SimpleRelayManager на сцене
✅ UI Canvas с SimpleGameUI настроен
✅ Все UI элементы связаны в Inspector
✅ Player Prefab с Network Identity создан
✅ Player Prefab назначен в NetworkManager
✅ Unity Services настроены с Project ID
✅ Mirror и Relay пакеты установлены
```

## 🚀 Тестирование

### Запуск в редакторе:
```
1. Play в Unity Editor
2. Нажать "Старт" 
3. Должен появиться статус "Ожидание игроков... (1/4)"
4. Собрать APK и протестировать с друзьями
```

### Сборка APK:
```
Build Settings:
✅ Platform: Android
✅ Scenes: добавить вашу сцену

Player Settings:
✅ Internet Access: Require
✅ Company Name: [ваше имя]
✅ Product Name: [название игры]
```

**После этой настройки игра будет готова к автоматическому matchmaking!** 🎉
