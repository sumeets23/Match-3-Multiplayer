# Match-3 Multiplayer Game

<img width="255" height="565" alt="thumb" src="https://github.com/user-attachments/assets/e733175b-f9e9-43d4-89e3-12826950b016" />

Unity 2D match-3 game with Photon Fusion multiplayer support.

Players can create a room, share a room code, join the lobby, and start a timed match. The project combines classic tile-swapping puzzle gameplay with synchronized player identity, scores, and match flow across connected clients.

## Features
- Room creation and room-code join flow
- Multiplayer lobby with player list
- Host-controlled match duration
- Networked player display names and scores
- 9x9 match-3 board with collapse and refill
- Bonus tile creation for longer matches
- End-of-match winner screen
- UI audio and configurable volume

## Tech Stack
- Unity 2021.3.45f2
- C#
- Photon Fusion
- TextMesh Pro
- DOTween
- Udar SceneField helper

## Gameplay Flow
1. Open the main menu.
2. Enter a player name.
3. Create or join a room.
4. Wait in the lobby.
5. Host starts the game.
6. Swap adjacent tiles to create matches.
7. Earn score while the timer counts down.
8. View the winner when time expires.

## Main Scripts
- [`Assets/Scripts/SceneController.cs`](../Assets/Scripts/SceneController.cs)
- [`Assets/Scripts/ShapesManager.cs`](../Assets/Scripts/ShapesManager.cs)
- [`Assets/Scripts/ShapesArray.cs`](../Assets/Scripts/ShapesArray.cs)
- [`Assets/Scripts/Shape.cs`](../Assets/Scripts/Shape.cs)
- [`Assets/Scripts/Multiplayer/NetworkManagerFusion.cs`](../Assets/Scripts/Multiplayer/NetworkManagerFusion.cs)
- [`Assets/Scripts/Multiplayer/PlayerNetwork.cs`](../Assets/Scripts/Multiplayer/PlayerNetwork.cs)
- [`Assets/Scripts/Multiplayer/LobbyUI.cs`](../Assets/Scripts/Multiplayer/LobbyUI.cs)
- [`Assets/Scripts/Multiplayer/MainMenuUI.cs`](../Assets/Scripts/Multiplayer/MainMenuUI.cs)
- [`Assets/Scripts/Multiplayer/GameTimerManager.cs`](../Assets/Scripts/Multiplayer/GameTimerManager.cs)

## Project Notes
This repository includes the full Unity project source and imported package content. The game logic is split between local puzzle handling and networked session management so the multiplayer layer can coordinate rooms, player metadata, and the match timer while the board handles tile behavior and scoring.

## License
See the root [`LICENSE`](../LICENSE) file for license details.

