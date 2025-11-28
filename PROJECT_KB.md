**Project Knowledge Base — Checkers**

- **Repo root**: `Checkers/`

**Shared DTOs**:
- **PlayerDto**: client/server player representation
  - `ConnectionId` : string
  - `Name` : string
  - `IsWhite` : bool

- **MoveDto**: single move
  - `Sr` : int (source row)
  - `Sc` : int (source col)
  - `Tr` : int (target row)
  - `Tc` : int (target col)
  - `Capture` : bool

- **PieceDto** (present in shared, used for single-piece description)
  - `IsWhite` : bool
  - `IsKing` : bool

- **GameStateDto**: full game state exchanged over SignalR
  - `GameId` : string
  - `PlayerWhite` : `PlayerDto`
  - `PlayerBlack` : `PlayerDto` (may be null until join)
  - `Board` : `List<string>` — 64 elements, each: `""` (empty), `"w"`/`"b"` (man), `"W"`/`"B"` (king)
  - `WhiteTurn` : bool
  - `IsFinished` : bool
  - `Winner` : string (player id/name)
  - `Moves` : `List<MoveDto>`

Notes: there is an older commented variant of MoveDto in `GameDtos.cs` that included `PlayerId` and `Time` — ignore unless resurrected.

**Server models (EF Core)**:
- `User` (`Checkers.Server.Models.User`)
  - `Id` (PK) : int
  - `Username` : string

- `Match` (`Checkers.Server.Models.Match`)
  - `Id` (PK) : int
  - `Player1Id` : int? (nullable)
  - `Player2Id` : int? (nullable)
  - `WinnerId` : int? (nullable)
  - `MovesJson` : string (JSON array stored as text, default "[]")
  - `DatePlayed` : DateTime

- `AppDbContext` exposes `DbSet<User> Users` and `DbSet<Match> Matches`.

**SignalR Hub**: `Checkers.Server.Hubs.GameHub`
- Methods callable by clients (signatures inferred):
  - `CreateGame(string gameId, PlayerDto player)`
    - Behavior: Adds caller to group `gameId`; sets caller `ConnectionId` and `IsWhite=true`; creates initial `GameStateDto` with board from `CreateInitialBoard()` and broadcasts `GameState` to the group.
    - Client should send: `{"gameId":"...","player":{...}}` (in practice via SignalR invocation with two args).

  - `JoinGame(string gameId, PlayerDto player)`
    - Behavior: Adds caller to group; sets `ConnectionId` and `IsWhite=false`; broadcasts `PlayerJoined` to group with `PlayerDto`.

  - `SyncState(string gameId, GameStateDto state)`
    - Behavior: Broadcasts `GameState` to the group. Used for full-state synchronization.

  - `MakeMove(string gameId, MoveDto move)`
    - Behavior: Sends `OpponentMove` to others in group.
    - Note: Server doesn't validate move (game logic is client-side); it simply relays moves.

- Hub helper: `CreateInitialBoard()` returns List<string> of length 64 with `"b"` on top playable squares and `"w"` on bottom playable squares.

**Client-side**:
- `Checkers.Client.GameEngine`
  - Uses `PieceType[,] Board` (8x8) with enum `PieceType` values: `None`, `White`, `Black`, `WhiteKing`, `BlackKing`.
  - `ValidateMove(sr,sc,tr,tc)` and `MakeMove(...)` implement move rules and capture logic. Returns `MoveResult` with `IsValid`, `IsCapture`, `CapturedPiece`, `BecameKing`.

- `SignalRService.cs` currently empty — expected to hold SignalR client connection and methods to call hub (`CreateGame`, `JoinGame`, `SyncState`, `MakeMove`) and to subscribe to events: `GameState`, `PlayerJoined`, `OpponentMove`.

**API / Controllers**:
- No game-specific HTTP controllers found. Only `WeatherForecastController` exists (template). Real-time operations are via SignalR Hub; persistence via `AppDbContext` should be invoked wherever server records matches (currently no explicit controller methods present in repo to save matches).

**Data flow & recommended request shapes**:
- SignalR method `CreateGame`
  - Invocation: `CreateGame(gameId: string, player: PlayerDto)`
  - Example PlayerDto JSON: `{ "ConnectionId": "", "Name": "Alice", "IsWhite": true }` (server sets `ConnectionId` and `IsWhite` server-side)

- SignalR method `JoinGame`
  - Invocation: `JoinGame(gameId: string, player: PlayerDto)`
  - Example: `{ "ConnectionId": "", "Name": "Bob", "IsWhite": false }` (server sets `ConnectionId`)

- SignalR method `MakeMove`
  - Invocation: `MakeMove(gameId: string, move: MoveDto)`
  - Example MoveDto JSON: `{ "Sr":5, "Sc":0, "Tr":4, "Tc":1, "Capture":false }`

- SignalR events clients must handle:
  - `GameState` : payload `GameStateDto` — update UI board and players
  - `PlayerJoined` : payload `PlayerDto` — opponent info
  - `OpponentMove` : payload `MoveDto` — apply move locally (client should validate/accept or reject)

**Mapping notes / mismatches to handle**:
- Server `GameStateDto.Board` is `List<string>` of 64 values (1D). Client `GameEngine` uses `PieceType[8,8]` (2D). Implement mapping helpers:
  - Server -> Client: iterate index 0..63: row = idx / 8, col = idx % 8; map `"w"` -> `PieceType.White`, `"W"` -> `WhiteKing`, `"b"` -> `Black`, `"B"` -> `BlackKing`, `""` -> `None`.
  - Client -> Server: serialize 8x8 into 64-length list of strings using reverse mapping.

- MoveDto fields (shared) are compatible with client `MakeMove` parameters (sr,sc,tr,tc). Ensure `Capture` flag is set by the client when sending.

**Persistence**:
- `Match.MovesJson` expected to store JSON array of `MoveDto` (or compatible objects). When storing, serialize moves using shared `MoveDto` shape.
- No server-side code currently saves matches — implement endpoints or hub server-side persistence when match ends (suggestion below).

**Gaps / ToDo (recommendations)**:
- Implement `SignalRService` in client: connect, reconnect, call hub methods, subscribe to events, and provide events to UI.
- Add server-side validation or authoritative move application if you want server-trusted game state (to prevent cheating). Currently logic is client-side; server only relays.
- Add persistence hooks in Hub (e.g., on game finish save `Match` with `MovesJson` and winner).
- Create lightweight HTTP API to list past matches and users (using `AppDbContext`).
- Add mapping helpers between `List<string>` board and `PieceType[,]`.

**Quick examples**:
- Example `GameStateDto` (JSON):
  {
    "GameId":"game123",
    "PlayerWhite": { "ConnectionId":"id1", "Name":"Alice", "IsWhite":true },
    "PlayerBlack": { "ConnectionId":"id2", "Name":"Bob", "IsWhite":false },
    "Board":["","b","",...,""] ,
    "WhiteTurn": true,
    "IsFinished": false,
    "Winner": null,
    "Moves": []
  }

**Where to look in repo**:
- Shared DTOs: `Checkers.Shared/Models/` (`PlayerDto.cs`, `MoveDto.cs`, `PieceDto.cs`, `GameStateDto.cs`)
- Server models + DbContext: `Checkers.Server/Models/`, `Checkers.Server/Data/AppDbContext.cs`
- Hub: `Checkers.Server/Hubs/GameHub.cs`
- Client engine: `Checkers.Client/GameEngine.cs`
- Client SignalR service placeholder: `Checkers.Client/Services/SignalRService.cs`

**New backend additions (implemented)**:
- Swagger / OpenAPI is enabled; UI available at `/swagger` when the server runs.
- `SessionsController` (`POST api/sessions/register`, `POST api/sessions/restore`) — simple session register/restore using `User` table (SQLite).
- `MatchesController` (`GET api/matches`) — list saved matches persisted in SQLite.
- `GameHub` extended:
  - `RegisterPlayer(string name)` — SignalR method to register a player (creates `User` record) and maps `ConnectionId` to `userId`.
  - `RestoreSession(int userId)` — re-associate a reconnecting client connection with an existing user id.
  - On `SyncState`, if `state.IsFinished == true`, the hub now saves a `Match` record to the DB (`MovesJson`, player ids where resolvable, `DatePlayed`).

Notes: session mapping is kept in memory (concurrent dictionaries) to map `ConnectionId <-> userId`. This is simple but sufficient for reconnection handling within a single server instance. For multi-instance deployments, consider external session store (Redis) and centralized user->connection tracking.

---
Generated on (local analysis). Keep this file up-to-date when models change.