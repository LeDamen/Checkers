using Microsoft.AspNetCore.SignalR;
using Checkers.Shared.Models;
using Checkers.Server.Data;
using Checkers.Server.Models;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;


namespace Checkers.Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly AppDbContext _db;

        // connectionId -> userId
        private static readonly ConcurrentDictionary<string, int> ConnectionToUser = new();
        // userId -> connectionId
        private static readonly ConcurrentDictionary<int, string> UserToConnection = new();

        public GameHub(AppDbContext db)
        {
            _db = db;
        }

        // Регистрация простого игрока (создаёт запись в Users и возвращает id)
        public async Task<int> RegisterPlayer(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;

            var user = _db.Users.FirstOrDefault(u => u.Username == name);
            if (user == null)
            {
                user = new User { Username = name };
                _db.Users.Add(user);
                _db.SaveChanges();
            }

            ConnectionToUser[Context.ConnectionId] = user.Id;
            UserToConnection[user.Id] = Context.ConnectionId;

            await Clients.Caller.SendAsync("SessionRegistered", user.Id, user.Username);
            return user.Id;
        }

        // Восстановление сессии по userId
        public async Task RestoreSession(int userId)
        {
            var user = _db.Users.Find(userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("SessionRestoreFailed");
                return;
            }

            ConnectionToUser[Context.ConnectionId] = userId;
            UserToConnection[userId] = Context.ConnectionId;

            await Clients.Caller.SendAsync("SessionRestored", user.Id, user.Username);
        }
        // Создание игры
        public async Task CreateGame(string gameId, PlayerDto player)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

            // Хост — белый игрок
            player.ConnectionId = Context.ConnectionId;
            player.IsWhite = true;

            var state = new GameStateDto(
                gameId,
                player,                 // white
                null,                   // black еще нет
                CreateInitialBoard(),   // доска
                true,                   // ход белых
                false,                  // игра не закончилась
                null,                   // победителя нет
                new List<MoveDto>()     // список ходов пуст
            );

            await Clients.Group(gameId).SendAsync("GameState", state);
        }

        // Подключение второго игрока
        public async Task JoinGame(string gameId, PlayerDto player)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

            player.ConnectionId = Context.ConnectionId;
            player.IsWhite = false;

            await Clients.Group(gameId).SendAsync("PlayerJoined", player);
        }

        // Синхронизация состояния (когда клиент получает State, он пересылает обновлённый назад)
        public async Task SyncState(string gameId, GameStateDto state)
        {
            await Clients.Group(gameId).SendAsync("GameState", state);

            // Если игра закончена — сохраняем матч в базу
            if (state != null && state.IsFinished)
            {
                try
                {
                    var match = new Match
                    {
                        Player1Id = GetUserIdByConnection(state.PlayerWhite?.ConnectionId),
                        Player2Id = GetUserIdByConnection(state.PlayerBlack?.ConnectionId),
                        WinnerId = null,
                        MovesJson = JsonSerializer.Serialize(state.Moves ?? new List<MoveDto>()),
                        DatePlayed = DateTime.UtcNow
                    };

                    // Try to set WinnerId by matching winner name to users
                    if (!string.IsNullOrWhiteSpace(state.Winner))
                    {
                        var winnerUser = _db.Users.FirstOrDefault(u => u.Username == state.Winner);
                        if (winnerUser != null) match.WinnerId = winnerUser.Id;
                    }

                    _db.Matches.Add(match);
                    _db.SaveChanges();

                    await Clients.Caller.SendAsync("MatchSaved", match.Id);
                }
                catch
                {
                    // ignore persistence errors for now
                }
            }
        }

        // Ход игрока
        public async Task MakeMove(string gameId, MoveDto move)
        {
            await Clients.OthersInGroup(gameId).SendAsync("OpponentMove", move);
        }


        // Создание стандартной стартовой доски
        private static List<string> CreateInitialBoard()
        {
            // 8х8 = 64 клетки: "": пусто, "w"/"b": обычные шашки, "W"/"B": дамки
            var board = Enumerable.Repeat("", 64).ToList();

            // Черные (верх)
            for (int i = 0; i < 24; i++)
            {
                if ((i / 8 + i) % 2 == 1)
                    board[i] = "b";
            }

            // Белые (низ)
            for (int i = 40; i < 64; i++)
            {
                if ((i / 8 + i) % 2 == 1)
                    board[i] = "w";
            }

            return board;
        }

        private int? GetUserIdByConnection(string? connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId)) return null;
            if (ConnectionToUser.TryGetValue(connectionId, out var uid)) return uid;
            return null;
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            // remove mapping(s)
            var conn = Context.ConnectionId;
            if (ConnectionToUser.TryRemove(conn, out var uid))
            {
                UserToConnection.TryRemove(uid, out _);
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
