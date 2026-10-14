using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var port = 27001;
            var ep = new IPEndPoint(IPAddress.Any, port);

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReceivedFiles");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(ep);
                socket.Listen(10);

                Console.Clear();
                Console.WriteLine("==================================================");
                Console.WriteLine("                   FILE SERVER                    ");
                Console.WriteLine("==================================================");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Server işə salındı...");
                Console.ResetColor();
                Console.WriteLine($"Port: {port}");
                Console.WriteLine("Gələn fayllar qəbul edilir.");
                Console.WriteLine();

                while (true)
                {
                    var client = await socket.AcceptAsync();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            while (true)
                            {
                                var lenBuffer = new byte[4];
                                int readBytes = await ReceiveAllAsync(client, lenBuffer, 4);
                                if (readBytes == 0) break;
                                int fileNameLen = BitConverter.ToInt32(lenBuffer, 0);

                                var fileNameBuffer = new byte[fileNameLen];
                                await ReceiveAllAsync(client, fileNameBuffer, fileNameLen);
                                var fileName = Encoding.UTF8.GetString(fileNameBuffer);

                                if (fileName == "EXIT") break;

                                var fileLenBuffer = new byte[8];
                                await ReceiveAllAsync(client, fileLenBuffer, 8);
                                long fileLen = BitConverter.ToInt64(fileLenBuffer, 0);

                                var fileBuffer = new byte[fileLen];
                                await ReceiveAllAsync(client, fileBuffer, (int)fileLen);

                                string savePath = Path.Combine(folderPath, fileName);
                                await File.WriteAllBytesAsync(savePath, fileBuffer);

                                string sizeStr = FormatSize(fileLen);
                                string timeStr = DateTime.Now.ToString("HH:mm:ss");

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write("[QƏBUL EDİLDİ] ");
                                Console.ResetColor();
                                Console.WriteLine($"{fileName,-15} ({sizeStr})    {timeStr}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Xəta: {ex.Message}");
                            Console.ResetColor();
                        }
                        finally
                        {
                            client.Dispose();
                        }
                    });
                }
            }
        }

        private static async Task<int> ReceiveAllAsync(Socket socket, byte[] buffer, int totalSize)
        {
            int totalReceived = 0;
            while (totalReceived < totalSize)
            {
                int received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, totalReceived, totalSize - totalReceived), SocketFlags.None);
                if (received == 0) return 0;
                totalReceived += received;
            }
            return totalReceived;
        }

        private static string FormatSize(long bytes)
        {
            double kb = bytes / 1024.0;
            if (kb < 1024)
                return $"{kb:F1} KB";
            double mb = kb / 1024.0;
            return $"{mb:F1} MB";
        }
    }
}