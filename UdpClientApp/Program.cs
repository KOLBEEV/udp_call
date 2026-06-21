using System.Net;
using System.Net.Sockets;
using System.Text;
using UdpCommon;

string serverIp = "127.0.0.1";
const int serverPort = 5000;
const int clientPort = 5001;

uint sequenceNumber = 0;

using CancellationTokenSource cancellationTokenSource = new();
using UdpClient udpClient = new UdpClient(clientPort);

IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

Console.WriteLine("UDP-клиент запущен");
Console.WriteLine();

Task receiveTask = ReceiveLoopAsync(cancellationTokenSource.Token);
Task sendTask = Task.Run(() => SendLoopAsync(cancellationTokenSource.Token));

await Task.WhenAll(receiveTask, sendTask);

async Task ReceiveLoopAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            UdpReceiveResult result = await udpClient.ReceiveAsync(cancellationToken);

            UdpPacket packet = PacketSerializer.Deserialize(result.Buffer);

            if (packet.Type == PacketType.Text)
            {
                string message = Encoding.UTF8.GetString(packet.Payload);

                Console.WriteLine();
                Console.WriteLine($"Сервер [{packet.SequenceNumber}]: {message}");
                Console.Write("> ");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"Получен пакет типа {packet.Type}");
                Console.Write("> ");
            }
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception exception)
        {
            Console.WriteLine();
            Console.WriteLine($"Ошибка приёма пакета: {exception.Message}");
            Console.Write("> ");
        }
    }
}

async Task SendLoopAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        Console.Write("> ");

        string? message = Console.ReadLine();

        if (message is null)
            continue;
        
        if (message == "/exit")
        {
            cancellationTokenSource.Cancel();
            break;
        }
        
        byte[] payload = Encoding.UTF8.GetBytes(message);

        uint currentSequenceNumber = sequenceNumber;
        sequenceNumber++;

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        UdpPacket packet = new UdpPacket(
            PacketType.Text,
            currentSequenceNumber,
            timestamp,
            payload
        );

        byte[] bytes = PacketSerializer.Serialize(packet);

        int sentBytes = await udpClient.SendAsync(
            bytes,
            bytes.Length,
            serverEndPoint
        );

        Console.WriteLine($"Отправлен пакет #{packet.SequenceNumber}, байт: {sentBytes}");
    }
}
