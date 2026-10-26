
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client1
{
    internal class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            
            string serverIP = "192.168.0.108";

            int port = 27001;

            using UdpClient client = new UdpClient();

            IPEndPoint serverEndPoint = new IPEndPoint(
                IPAddress.Parse(serverIP), port);

            Console.WriteLine(" TIC TAC TOE CLIENT ");
            Console.Write("Adinizi daxil edin: ");

            string name = Console.ReadLine()?.Trim() ?? "";

            if (name == "")
            {
                Console.WriteLine("Ad bos ola bilmez...");
                return;
            }

            byte[] joinData = Encoding.UTF8.GetBytes("JOIN|" + name);

            client.Send(joinData, joinData.Length, serverEndPoint);

            Console.WriteLine("Servere qosulma sorgusu gonderildi...");
            Console.WriteLine("İkinci oyuncunun qosulmasi gozlenilir...\n");

            while (true)
            {
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref remote);

                string message = Encoding.UTF8.GetString(data);
                string[] parts = message.Split('|'); 

                if (parts.Length < 8 || parts[0] != "STATE")
                    continue;

                int mySymbol = int.Parse(parts[1]);
                int turn = int.Parse(parts[2]);
                int winner = int.Parse(parts[3]);
                bool draw = bool.Parse(parts[4]);

                string gamer1 = parts[5];
                string gamer2 = parts[6];

                int[] cells = Array.ConvertAll(
                    parts[7].Split(','),
                    int.Parse);

                Console.Clear();

                Console.WriteLine(" TIC TAC TOE ");
                Console.WriteLine($"{gamer1} (X)  VS  {gamer2} (O)");
                Console.WriteLine(
                    "Sizin simvolunuz: " +
                    (mySymbol == 1 ? "X" : "O"));

                Console.WriteLine();

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int value = cells[i * 3 + j];

                        string symbol = value == 1 ? "X"
                            : value == -1 ? "O"
                            : (i * 3 + j + 1).ToString();

                        Console.Write(" " + symbol + " ");

                        if (j < 2)
                            Console.Write("|");
                    }

                    Console.WriteLine();

                    if (i < 2)
                        Console.WriteLine("---+---+---");
                }

                Console.WriteLine();

                if (winner != 0)
                {
                    Console.WriteLine(
                        "Qalib: " + (winner == 1 ? gamer1 : gamer2));

                    Console.WriteLine(
                        winner == mySymbol
                            ? "Tebrikler siz qalib geldiniz :D"
                            : "Bu dəfə rəqibiniz qalib gəldi  :(");//*****

                    break;
                }

                if (draw)
                {
                    Console.WriteLine("Oyun hec hece bitdi.....");
                    break;
                }

                if (turn == mySymbol)
                {
                    Console.WriteLine("Sizin siranizdir...");

                    Console.Write("Setir (1-3): ");
                    string? rowText = Console.ReadLine();

                    Console.Write("Sutun (1-3): ");
                    string? colText = Console.ReadLine();

                    if (!int.TryParse(rowText, out int row) ||
                        !int.TryParse(colText, out int col))
                    {
                        Console.WriteLine("Yanlis giris...");
                        continue;
                    }

                    string move = $"MOVE|{row}|{col}";
                    byte[] moveData = Encoding.UTF8.GetBytes(move);

                    client.Send(
                        moveData,
                        moveData.Length,
                        serverEndPoint);
                }
                else
                {
                    Console.WriteLine(
                        "Reqibiniz gozlenilir...");
                }
            }

            Console.WriteLine("\n....Oyunu bitirmek ucun Enter basin....");
            Console.ReadLine();
        }
    }
}
