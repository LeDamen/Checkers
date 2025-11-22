using Checkers.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Checkers.Client
{
    public partial class MainWindow : Window
    {
        private HubConnection _connection;
        private GameStateDto _state;
        private string _gameId;
        private PlayerDto _me;

        private Button[,] _cells = new Button[8, 8];

        public MainWindow()
        {
            InitializeComponent();
            InitBoardGrid();
            InitSignalR();
        }

        private void InitBoardGrid()
        {
            BoardGrid.Children.Clear();
            BoardGrid.RowDefinitions.Clear();
            BoardGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < 8; i++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition());
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var btn = new Button
                    {
                        Tag = (r, c),
                        Margin = new Thickness(0),
                        FontSize = 20
                    };
                    btn.Click += Cell_Click;
                    Grid.SetRow(btn, r);
                    Grid.SetColumn(btn, c);
                    BoardGrid.Children.Add(btn);
                    _cells[r, c] = btn;
                }
            }
        }




        private async void InitSignalR()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7150/gamehub")
                .WithAutomaticReconnect()
                .Build();

            _connection.On<GameStateDto>("GameState", state =>
            {
                _state = state;
                Dispatcher.Invoke(RenderBoard);
            });

            _connection.On<MoveDto>("OpponentMove", move =>
            {
                ApplyMoveLocal(move);
                Dispatcher.Invoke(RenderBoard);
            });

            try
            {
                await _connection.StartAsync();
                MessageBox.Show("Соединение установлено");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка соединения: " + ex.Message);
            }
        }

        // ---------------- GAME MANAGEMENT ----------------

        private async void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            _gameId = TbGameId.Text;
            _me = new PlayerDto(_connection.ConnectionId, "Player", true);

            await _connection.InvokeAsync("CreateGame", _gameId, _me);
        }

        private async void BtnJoin_Click(object sender, RoutedEventArgs e)
        {
            _gameId = TbGameId.Text;
            _me = new PlayerDto(_connection.ConnectionId, "Player", false);

            await _connection.InvokeAsync("JoinGame", _gameId, _me);
        }

        // ---------------- BOARD ----------------

        private void RenderBoard()
        {
            // Защита: если состояние пустое — нарисуем пустую доску (или стартовую)
            if (_cells == null)
                return;

            // Если _state отсутствует — покажем стартовую расстановку
            List<string> boardList = null;
            if (_state?.Board != null)
                boardList = _state.Board;
            else
                boardList = CreateInitialBoard(); // локальная функция (ниже)

            // ensure length
            if (boardList == null || boardList.Count != 64)
            {
                AppendLog("RenderBoard: некорректный boardList");
                return;
            }

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var btn = _cells[r, c];
                    bool dark = (r + c) % 2 == 1;
                    btn.Background = dark ? Brushes.SaddleBrown : Brushes.BurlyWood;
                    btn.BorderThickness = new Thickness(1);
                    btn.BorderBrush = Brushes.Black;

                    string cell = boardList[r * 8 + c] ?? "";
                    btn.Content = cell switch
                    {
                        "w" => "●",
                        "b" => "●",
                        "W" => "♔",
                        "B" => "♔",
                        _ => ""
                    };

                    btn.FontSize = 32;
                    btn.Foreground = cell.ToLower() == "w" ? Brushes.White : Brushes.Black;

                }
            }
        }

        private void AppendLog(string v)
        {
            throw new NotImplementedException();
        }

        private List<string> CreateInitialBoard()
        {
            var board = Enumerable.Repeat("", 64).ToList();
            // Черные сверху (обычная расстановка по чёрным клеткам)
            for (int i = 0; i < 24; i++)
                board[i] = ((i / 8 + i) % 2 == 1) ? "b" : "";
            // Белые снизу
            for (int i = 40; i < 64; i++)
                board[i] = ((i / 8 + i) % 2 == 1) ? "w" : "";
            return board;
        }




        // ---------------- MOVE HANDLING ----------------

        private (int r, int c)? _selected;

        private async void Cell_Click(object sender, RoutedEventArgs e)
        {
            if (_state == null) return;

            var btn = (Button)sender;

            int index = BoardGrid.Children.IndexOf(btn);
            int r = index / 8;
            int c = index % 8;

            if (_selected == null)
            {
                // Выбор фигуры
                if (_state.Board[r * 8 + c] == "")
                    return;

                _selected = (r, c);
                btn.BorderBrush = Brushes.Red;
                btn.BorderThickness = new Thickness(3);
            }
            else
            {
                var (sr, sc) = _selected.Value;

                _selected = null;
                foreach (var b in _cells)
                {
                    b.BorderThickness = new Thickness(1);
                }

                int dr = r - sr;
                int dc = c - sc;
                bool isCapture = Math.Abs(dr) == 2 && Math.Abs(dc) == 2;

                var move = new MoveDto(sr, sc, r, c, isCapture);

                ApplyMoveLocal(move);
                RenderBoard();

                // --- проверяем, есть ли ещё рубка ---
                if (isCapture && HasMoreCaptures(r, c, "W"))
                {
                    _selected = (r, c); // оставляем шашку выбранной
                    _cells[r, c].BorderBrush = Brushes.Red;
                    _cells[r, c].BorderThickness = new Thickness(3);
                    return; // не заканчиваем ход!
                }

                // --- ход завершён ---
                _selected = null;
                foreach (var b in _cells)
                    b.BorderThickness = new Thickness(1);

                // отправляем на сервер
                if (_gameId != null)
                {
                    await _connection.InvokeAsync("MakeMove", _gameId, move);
                }

            }
        }

        private void ApplyMoveLocal(MoveDto move)
        {
            var board = _state.Board;
            int sr = move.Sr;
            int sc = move.Sc;
            int tr = move.Tr;
            int tc = move.Tc;

            int si = sr * 8 + sc;
            int ti = tr * 8 + tc;

            string piece = board[si];
            if (piece == "") return;

            bool isWhite = piece == "w" || piece == "W";
            bool isKing = piece == "W" || piece == "B";



            // ---------- ОЧЕРЁДНОСТЬ ХОДОВ ----------
            if (isWhite != _state.WhiteTurn)
                return;

            // ---------- ЦЕЛЕВАЯ КЛЕТКА НЕ ДОЛЖНА БЫТЬ ЗАНЯТА ----------
            if (board[ti] != "")
                return;

            int dr = tr - sr;
            int dc = tc - sc;


            // ---------- ХОДЫ ДАМКИ ----------
            if (isKing)
            {
                int adr = Math.Abs(dr);
                int adc = Math.Abs(dc);

                // Дамка должна двигаться по диагонали
                if (adr != adc) return;

                int stepR = dr > 0 ? 1 : -1;
                int stepC = dc > 0 ? 1 : -1;

                int r = sr + stepR;
                int c = sc + stepC;

                int enemyCount = 0;
                int capturedR = -1, capturedC = -1;

                while (r != tr && c != tc)
                {
                    string mid = board[r * 8 + c];

                    if (mid != "")
                    {
                        // Своя фигура
                        if ((isWhite && (mid == "w" || mid == "W")) ||
                            (!isWhite && (mid == "b" || mid == "B")))
                            return;

                        enemyCount++;
                        capturedR = r;
                        capturedC = c;

                        if (enemyCount > 1) return;
                    }

                    r += stepR;
                    c += stepC;
                }

                // Цель должна быть пустой
                if (board[ti] != "") return;

                // Если была рубка — убрать врага
                if (enemyCount == 1)
                {
                    board[capturedR * 8 + capturedC] = "";
                }

                // Сделать ход дамкой
                board[ti] = piece;
                board[si] = "";

                // Проверка многоходовой рубки дамкой
                if (enemyCount == 1 && HasMoreKingCaptures(tr, tc, piece))
                {
                    return; // дамка продолжает рубку, очередь НЕ меняем
                }

                // Очередь меняется
                _state.WhiteTurn = !_state.WhiteTurn;
                return;
            }


            // ---------- ПРОСТОЙ ХОД ----------
            if (Math.Abs(dr) == 1 && Math.Abs(dc) == 1)
            {
                if (!isKing)
                {
                    if (piece == "w" && dr != -1) return;
                    if (piece == "b" && dr != 1) return;
                }

                board[ti] = piece;
                board[si] = "";

                TryPromoteToKing(tr, ti);

                _state.WhiteTurn = !_state.WhiteTurn;
                return;
            }

            // ---------- РУБКА ----------
            if (Math.Abs(dr) == 2 && Math.Abs(dc) == 2)
            {
                int cr = sr + dr / 2;
                int cc = sc + dc / 2;
                int ci = cr * 8 + cc;

                string captured = board[ci];

                // должна быть вражеская
                if (captured == "" ||
                    (isWhite && (captured == "w" || captured == "W")) ||
                    (!isWhite && (captured == "b" || captured == "B")))
                    return;

                // клетка за врагом должна быть пустая
                if (board[ti] != "")
                    return;

                // Рубим
                board[ci] = "";
                board[ti] = piece;
                board[si] = "";

                TryPromoteToKing(tr, ti);

                // ---------- ПРЕВРАЩЕНИЕ В ДАМКУ ----------
                if (piece == "w" && tr == 0)
                    board[ti] = "W";

                if (piece == "b" && tr == 7)
                    board[ti] = "B";


                // ---------- ПРОВЕРКА МНОГОХОДОВОЙ РУБКИ ----------
                if (HasMoreCaptures(tr, tc, piece))
                {
                    // НЕ меняем ход — продолжаем рубку той же шашкой
                    return;
                }

                // Нет больше рубки — меняем ход
                _state.WhiteTurn = !_state.WhiteTurn;
                return;
            }
        }


        private void TryPromoteToKing(int row, int index)
        {
            string p = _state.Board[index];

            if (p == "w" && row == 0)
                _state.Board[index] = "W";

            if (p == "b" && row == 7)
                _state.Board[index] = "B";
        }



        private bool HasMoreKingCaptures(int r, int c, string piece)
        {
            bool isWhite = piece == "W";

            int[] dirs = { -1, 1 };

            foreach (int dr in dirs)
            {
                foreach (int dc in dirs)
                {
                    int rr = r + dr;
                    int cc = c + dc;

                    bool enemyFound = false;
                    int capturedR = -1, capturedC = -1;

                    while (rr >= 0 && rr < 8 && cc >= 0 && cc < 8)
                    {
                        string mid = _state.Board[rr * 8 + cc];

                        if (mid != "")
                        {
                            // Своя — остановка
                            if ((isWhite && (mid == "w" || mid == "W")) ||
                                (!isWhite && (mid == "b" || mid == "B")))
                                break;

                            // Уже встречали врага → нельзя
                            if (enemyFound) break;

                            enemyFound = true;
                            capturedR = rr;
                            capturedC = cc;
                        }
                        else
                        {
                            // Если был враг и нашли пустую клетку за ним → рубка возможна
                            if (enemyFound)
                                return true;
                        }

                        rr += dr;
                        cc += dc;
                    }
                }
            }

            return false;
        }



        private bool HasMoreCaptures(int r, int c, string piece)
        {
            bool isWhite = piece == "w" || piece == "W";

            int[,] dirs = {
        { 1, 1 }, { 1, -1 },
        { -1, 1 }, { -1, -1 }
    };

            for (int i = 0; i < 4; i++)
            {
                int dr = dirs[i, 0];
                int dc = dirs[i, 1];

                int cr = r + dr;
                int cc = c + dc;
                int tr = r + dr * 2;
                int tc = c + dc * 2;

                if (cr < 0 || cr >= 8 || cc < 0 || cc >= 8 ||
                    tr < 0 || tr >= 8 || tc < 0 || tc >= 8)
                    continue;

                int ci = cr * 8 + cc;
                int ti = tr * 8 + tc;

                string captured = _state.Board[ci];

                if (captured == "") continue;
                if (_state.Board[ti] != "") continue;

                // враг?
                if (isWhite && (captured == "b" || captured == "B")) return true;
                if (!isWhite && (captured == "w" || captured == "W")) return true;
            }

            return false;
        }




        // ---------- ЕСЛИ ХОД НЕ ПОДХОДИТ ПОД ПРАВИЛА – ОТМЕНА ----------
    }
}

