using Microsoft.AspNetCore.SignalR;
using Checkers.Shared.Models;
using System.Linq;
using System.Collections.Generic;

namespace Checkers.Server.Hubs
{
    public class GameHub : Hub
    {
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
    }
}
