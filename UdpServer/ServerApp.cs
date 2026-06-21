using System.Net;
using System.Text;
using UdpCommon;

namespace UdpServer;

public sealed class ServerApp : IDisposable
{
    private readonly UdpPeer _udpPeer;

    private readonly PacketFactory _packetFactory = new();
    private readonly PingTracker _pingTracker = new();
    private readonly PacketOrderTracker _packetOrderTracker = new();

    private IPEndPoint? _clientEndPoint = null;

    public ServerApp(int localPort)
    {
        _udpPeer = new UdpPeer(localPort);
    }

    public async Task RunAsync(CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"Сервер запущен на порту {_udpPeer.LocalPort}");
        Console.WriteLine("Сначала клиент должен отправить первое сообщение.");
        Console.WriteLine("После этого сервер сможет ему отвечать.");
        Console.WriteLine();
        Console.WriteLine("Команды:");
        Console.WriteLine("  обычный текст  - отправить сообщение клиенту");
        Console.WriteLine("  /ping          - измерить RTT до клиента");
        Console.WriteLine("  /ping5         - отправить 5 ping-пакетов");
        Console.WriteLine("  /exit          - выйти");
        Console.WriteLine();

        Task receiveTask = ReceiveLoopAsync(cancellationTokenSource.Token);
        Task inputTask = Task.Run(() => InputLoopAsync(cancellationTokenSource));

        await Task.WhenAll(receiveTask, inputTask);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                ReceivedUdpPacket received = await _udpPeer.ReceiveAsync(cancellationToken);

                _clientEndPoint = received.RemoteEndPoint;

                await HandleIncomingPacketAsync(received);
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

    private async Task InputLoopAsync(CancellationTokenSource cancellationTokenSource)
    {
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            Console.Write("> ");

            string? input = Console.ReadLine();

            if (input is null)
            {
                continue;
            }

            if (input == "/exit")
            {
                cancellationTokenSource.Cancel();
                break;
            }

            if (_clientEndPoint is null)
            {
                Console.WriteLine("Я ещё не знаю адрес клиента. Сначала клиент должен отправить сообщение.");
                continue;
            }

            if (input == "/ping")
            {
                await SendPingAsync(_clientEndPoint);
                continue;
            }

            if (input == "/ping5")
            {
                await SendPingSeveralTimesAsync(_clientEndPoint, 5);
                continue;
            }

            await SendTextAsync(input, _clientEndPoint);
        }
    }

    private async Task HandleIncomingPacketAsync(ReceivedUdpPacket received)
    {
        UdpPacket packet = received.Packet;

        PrintPacketOrderInfo(packet.SequenceNumber);

        if (packet.Type == PacketType.Text)
        {
            string message = Encoding.UTF8.GetString(packet.Payload);

            Console.WriteLine();
            Console.WriteLine(
                $"Клиент {received.RemoteEndPoint.Address}:{received.RemoteEndPoint.Port} " +
                $"[{packet.SequenceNumber}]: {message}"
            );
            Console.Write("> ");

            return;
        }

        if (packet.Type == PacketType.Ping)
        {
            await SendPongAsync(packet, received.RemoteEndPoint);
            return;
        }

        if (packet.Type == PacketType.Pong)
        {
            HandlePong(packet);
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Получен пакет типа {packet.Type}, номер {packet.SequenceNumber}");
        Console.Write("> ");
    }

    private async Task SendTextAsync(string message, IPEndPoint remoteEndPoint)
    {
        UdpPacket packet = _packetFactory.CreateText(message);

        int sentBytes = await _udpPeer.SendAsync(packet, remoteEndPoint);

        Console.WriteLine($"Отправлен Text #{packet.SequenceNumber}, байт: {sentBytes}");
    }

    private async Task SendPingAsync(IPEndPoint remoteEndPoint)
    {
        UdpPacket packet = _packetFactory.CreatePing();

        _pingTracker.RegisterPing(packet);

        int sentBytes = await _udpPeer.SendAsync(packet, remoteEndPoint);

        Console.WriteLine($"Отправлен Ping #{packet.SequenceNumber}, байт: {sentBytes}");
    }

    private async Task SendPingSeveralTimesAsync(IPEndPoint remoteEndPoint, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await SendPingAsync(remoteEndPoint);
            await Task.Delay(500);
        }
    }

    private async Task SendPongAsync(UdpPacket pingPacket, IPEndPoint remoteEndPoint)
    {
        UdpPacket pongPacket = _packetFactory.CreatePong(pingPacket);

        await _udpPeer.SendAsync(pongPacket, remoteEndPoint);

        Console.WriteLine();
        Console.WriteLine($"Получен Ping #{pingPacket.SequenceNumber}");
        Console.WriteLine($"Отправлен Pong #{pongPacket.SequenceNumber}");
        Console.Write("> ");
    }

    private void HandlePong(UdpPacket pongPacket)
    {
        bool found = _pingTracker.TryCompletePing(
            pongPacket,
            out long rttMilliseconds
        );

        Console.WriteLine();

        if (!found)
        {
            Console.WriteLine($"Получен неожиданный Pong #{pongPacket.SequenceNumber}");
            Console.Write("> ");
            return;
        }

        Console.WriteLine($"Получен Pong #{pongPacket.SequenceNumber}");
        Console.WriteLine($"RTT: {rttMilliseconds} мс");
        Console.WriteLine($"Примерная задержка в одну сторону: {rttMilliseconds / 2.0:F1} мс");
        Console.Write("> ");
    }

    private void PrintPacketOrderInfo(uint sequenceNumber)
    {
        PacketOrderResult result = _packetOrderTracker.Check(sequenceNumber);

        if (result.State == PacketOrderState.MissingPackets)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Возможно, потеряны пакеты. " +
                $"Ожидался #{result.ExpectedSequenceNumber}, " +
                $"пришёл #{result.CurrentSequenceNumber}. " +
                $"Пропущено: {result.MissingCount}"
            );
        }

        if (result.State == PacketOrderState.OutOfOrderOrDuplicate)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Пакет пришёл не по порядку или повторно. " +
                $"Последний был #{result.LastSequenceNumber}, " +
                $"пришёл #{result.CurrentSequenceNumber}"
            );
        }
    }

    public void Dispose()
    {
        _udpPeer.Dispose();
    }
}