
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server1
{
    internal class Program
    {
        class GameData
        {
            public string Gamer1 { get; set; } = "";
            public string Gamer2 { get; set; } = "";
            public int[][] Game { get; set; } =
            {
                new int[3],
                new int[3],
                new int[3]
            };
        }

        static UdpClient server = new UdpClient(27001);
        static IPEndPoint? player1;
        static IPEndPoint? player2;

        static string name1 = "";
        static string name2 = "";

        static int[,] game = new int[3, 3];
        static int turn = 1;
        static bool finished = false;

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine(" TIC TAC TOE  ");
            Console.WriteLine("Port: 27001");
            Console.WriteLine("İki oyuncunun qosulmasi gozlenilir...\n");

            while (true)
            {
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = server.Receive(ref remote);

                string message = Encoding.UTF8.GetString(data);
                string[] parts = message.Split('|');

                if (parts[0] == "JOIN")
                {
                    JoinPlayer(remote, parts);
                }
                else if (parts[0] == "MOVE")
                {
                    MakeMove(remote, parts);
                }
            }
        }

        static void JoinPlayer(IPEndPoint remote, string[] parts)
        {
            string name = parts.Length > 1
                ? parts[1].Trim()
                : "";

            if (name == "")
                return;

            if (player1 != null && player1.Equals(remote))
                return;

            if (player2 != null && player2.Equals(remote))
                return;

            if (player1 == null)
            {
                player1 = remote;
                name1 = name;

                Console.WriteLine(name1 + " qosuldu: X");
                Console.WriteLine("İkinci oyuncu gozlenilir...");
            }
            else if (player2 == null)
            {
                player2 = remote;
                name2 = name;

                Console.WriteLine(name2 + " qosuldu: O");
                Console.WriteLine("\nOyun basladi...");

                SendState();
            }
        }

        static void MakeMove(IPEndPoint remote, string[] parts)
        {
            if (player1 == null || player2 == null || finished)
                return;

            if (parts.Length < 3)
                return;

            int symbol;

            if (remote.Equals(player1))
                symbol = 1;
            else if (remote.Equals(player2))
                symbol = -1;
            else
                return;

            if (symbol != turn)
                return;

            if (!int.TryParse(parts[1], out int row) ||
                !int.TryParse(parts[2], out int col))
                return;

            //////////////////////
            row--;
            col--;

            if (row < 0 || row > 2 || col < 0 || col > 2)
            {
                SendState();
                return;
            }

            if (game[row, col] != 0)
            {
                SendState();
                return;
            }

            game[row, col] = symbol;

            Console.WriteLine(
                $"{(symbol == 1 ? name1 : name2)}: " +
                $"{row + 1}, {col + 1}");

            int winner = CheckWinner();

            if (winner != 0)
            {
                finished = true;
                Console.WriteLine(
                    "Winnerr: " + (winner == 1 ? name1 : name2));
            }
            else if (IsDraw())
            {
                finished = true;
                Console.WriteLine("Hec-hece...");
            }
            else
            {
                turn = -turn;
            }

            SaveGame();
            SendState();
        }

        static int CheckWinner()
        {
            for (int i = 0; i < 3; i++)
            {
                if (game[i, 0] != 0 &&
                    game[i, 0] == game[i, 1] &&
                    game[i, 1] == game[i, 2])
                    return game[i, 0];

                if (game[0, i] != 0 &&
                    game[0, i] == game[1, i] &&
                    game[1, i] == game[2, i])
                    return game[0, i];
            }

            if (game[0, 0] != 0 &&
                game[0, 0] == game[1, 1] &&
                game[1, 1] == game[2, 2])
                return game[0, 0];

            if (game[0, 2] != 0 &&
                game[0, 2] == game[1, 1] &&
                game[1, 1] == game[2, 0])
                return game[0, 2];

            return 0;
        }

        static bool IsDraw()
        {
            foreach (int cell in game)
            {
                if (cell == 0)
                    return false;
            }

            return true;
        }

        static void SendState()
        {
            if (player1 == null || player2 == null)
                return;

            int winner = CheckWinner();
            bool draw = finished && winner == 0;

            SendToPlayer(player1, 1, winner, draw);
            SendToPlayer(player2, -1, winner, draw);
        }

        static void SendToPlayer(
            IPEndPoint endpoint,
            int mySymbol,
            int winner,
            bool draw)
        {
            string cells = "";

            foreach (int cell in game)
                cells += cell + ",";

            cells = cells.TrimEnd(',');

            string message =
                $"STATE|{mySymbol}|{turn}|{winner}|{draw}|" +
                $"{name1}|{name2}|{cells}";

            byte[] data = Encoding.UTF8.GetBytes(message);
            server.Send(data, data.Length, endpoint);
        }

        static void SaveGame()
        {
            if (player1 == null || player2 == null)
                return;

            GameData data = new GameData
            {
                Gamer1 = name1,
                Gamer2 = name2,
                Game = new int[3][]
            };

            for (int i = 0; i < 3; i++)
            {
                data.Game[i] = new int[3];

                for (int j = 0; j < 3; j++)
                    data.Game[i][j] = game[i, j];
            }

            string safe1 = string.Concat(
                name1.Split(System.IO.Path.GetInvalidFileNameChars()));

            string safe2 = string.Concat(
                name2.Split(System.IO.Path.GetInvalidFileNameChars()));

            string fileName = $"{safe1}_{safe2}.json";

            string json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true });

            System.IO.File.WriteAllText(fileName, json);

            Console.WriteLine("Oyun yadda saxlanildi: " + fileName);
        }
    }
}
