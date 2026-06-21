using System.Net;
using System.Net.Sockets;
using System.Text;
using UdpCommon;

const int serverPort = 5000;

uint sequenceNumber = 0;

using CancellationTokenSource cancellationTokenSource = new();
using UdpClient udpServer = new UdpClient(serverPort);

IPEndPoint? clientEndPoint = null;

Console.WriteLine($"UDP-сервер запущен на порту {serverPort}");
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
            UdpReceiveResult result = await udpServer.ReceiveAsync(cancellationToken);

            clientEndPoint = result.RemoteEndPoint;

            UdpPacket packet = PacketSerializer.Deserialize(result.Buffer);

            if (packet.Type == PacketType.Text)
            {
                string message = Encoding.UTF8.GetString(packet.Payload);

                Console.WriteLine();
                Console.WriteLine(
                    $"Клиент {result.RemoteEndPoint.Address}:{result.RemoteEndPoint.Port} " +
                    $"[{packet.SequenceNumber}]: {message}"
                );
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
        Console.WriteLine("> ");

        string? message = Console.ReadLine();

        if (message is null)
            continue;
        
        if (message == "/exit")
        {
            cancellationTokenSource.Cancel();
            break;
        }

        IPEndPoint? endPoint = clientEndPoint;
        
        if (endPoint is null)
        {
            Console.WriteLine("Нет адреса клиента");
            continue;
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

        int sentBytes = await udpServer.SendAsync(
            bytes,
            bytes.Length,
            endPoint
        );

        Console.WriteLine($"Отправлен пакет #{packet.SequenceNumber}, байт: {sentBytes}");
    }
}
