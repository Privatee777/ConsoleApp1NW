using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var ipAddress = IPAddress.Parse("192.168.100.149");
            var port = 27001;
            var ep = new IPEndPoint(ipAddress, port);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    await socket.ConnectAsync(ep);

                    if (socket.Connected)
                    {
                        while (true)
                        {
                            Console.Clear();
                            Console.WriteLine("==================================================");
                            Console.WriteLine("                   FILE CLIENT                    ");
                            Console.WriteLine("==================================================");
                            Console.WriteLine();
                            Console.WriteLine("1 - Fayl göndər (şəkil/səs)");
                            Console.WriteLine("0 - Çıxış");
                            Console.WriteLine("==================================================");
                            Console.Write("Seçim edin: ");

                            var choice = Console.ReadLine();

                            if (choice == "0")
                            {
                                var exitBytes = Encoding.UTF8.GetBytes("EXIT");
                                await SendAllAsync(socket, exitBytes);
                                break;
                            }
                            else if (choice == "1")
                            {
                                Console.Write("\nFaylın yolunu daxil edin: ");
                                var filePath = Console.ReadLine()?.Trim('"');

                                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Fayl tapılmadı! Yenidən cəhd edin.");
                                    Console.ResetColor();
                                    Thread.Sleep(1500);
                                    continue;
                                }

                                var fileInfo = new FileInfo(filePath);
                                var fileName = fileInfo.Name;
                                var fileBytes = await File.ReadAllBytesAsync(filePath);

                                Console.WriteLine("Göndərilir...");

                                var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                                var fileNameLenBytes = BitConverter.GetBytes(fileNameBytes.Length);
                                await SendAllAsync(socket, fileNameLenBytes);
                                await SendAllAsync(socket, fileNameBytes);

                                var fileLenBytes = BitConverter.GetBytes((long)fileBytes.Length);
                                await SendAllAsync(socket, fileLenBytes);

                                await SendAllAsync(socket, fileBytes);

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Fayl uğurla göndərildi!");
                                Console.ResetColor();

                                Console.WriteLine("\nDavam etmək üçün bir düyməyə basın...");
                                Console.ReadKey();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Xəta: {ex.Message}");
                    Console.ResetColor();
                    Console.ReadKey();
                }
            }
        }

        private static async Task SendAllAsync(Socket socket, byte[] data)
        {
            int totalSent = 0;
            while (totalSent < data.Length)
            {
                int sent = await socket.SendAsync(new ArraySegment<byte>(data, totalSent, data.Length - totalSent), SocketFlags.None);
                if (sent == 0) throw new Exception("Bağlantı kəsildi.");
                totalSent += sent;
            }
        }
    }
}