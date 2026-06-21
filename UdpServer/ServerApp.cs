using System.Net;
using UdpCommon;

namespace UdpServer;

public sealed class ServerApp : IDisposable
{
    private readonly UdpSession _session;

    private readonly object _clientEndPointLock = new object();

    private IPEndPoint? _clientEndPoint = null;

    public ServerApp(int localPort)
    {
        _session = new UdpSession(localPort);
    }

    public async Task RunAsync(CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine($"Сервер запущен на порту {_session.LocalPort}");
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
                ReceivedUdpPacket received = await _session.ReceiveAsync(cancellationToken);

                SetClientEndPoint(received.RemoteEndPoint);

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

        switch (packet.Type)
        {
            case PacketType.Text:
                HandleText(packet, received.RemoteEndPoint);
                break;
            
            case PacketType.Ping:
                await HandlePingAsync(packet, received.RemoteEndPoint);
                return;

            case PacketType.Pong:
                HandlePong(packet);
                break;
            
            default:
                Console.WriteLine();
                Console.WriteLine($"Получен пакет типа {packet.Type}, номер {packet.SequenceNumber}");
                Console.Write("> ");
                break;
        }
    }

    private void HandleText(UdpPacket packet, IPEndPoint remoteEndPoint)
    {
        PrintPacketOrderInfo(packet);

        string message = UdpSession.DecodeText(packet);

        Console.WriteLine();
        Console.WriteLine(
            $"Клиент {remoteEndPoint.Address}:{remoteEndPoint.Port} " +
            $"[{packet.SequenceNumber}]: {message}"
        );
        Console.Write("> ");
    }

    private async Task HandlePingAsync(UdpPacket pingPacket, IPEndPoint remoteEndPoint)
    {
        await _session.SendPongAsync(pingPacket, remoteEndPoint);

        Console.WriteLine();
        Console.WriteLine($"Получен Ping #{pingPacket.SequenceNumber}");
        Console.WriteLine($"Отправлен Pong #{pingPacket.SequenceNumber}");
        Console.Write("> ");
    }

    private void HandlePong(UdpPacket pongPacket)
    {
        bool found = _session.TryCompletePong(
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

    private async Task SendTextAsync(string message, IPEndPoint clientEndPoint)
    {
        int sentBytes = await _session.SendTextAsync(message, clientEndPoint);

        Console.WriteLine($"Отправлен Text, байт: {sentBytes}");
    }

    private async Task SendPingAsync(IPEndPoint clientEndPoint)
    {
        int sentBytes = await _session.SendPingAsync(clientEndPoint);

        Console.WriteLine($"Отправлен Ping, байт: {sentBytes}");
    }

    private async Task SendPingSeveralTimesAsync(IPEndPoint remoteEndPoint, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await SendPingAsync(remoteEndPoint);
            await Task.Delay(500);
        }
    }

    private void PrintPacketOrderInfo(UdpPacket packet)
    {
        PacketOrderResult result = _session.CheckPacketOrder(packet);

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

    private void SetClientEndPoint(IPEndPoint clientEndPoint)
    {
        lock (_clientEndPointLock)
        {
            _clientEndPoint = clientEndPoint;
        }
    }

    private IPEndPoint? GetClientEndPoint()
    {
        lock (_clientEndPointLock)
        {
            return _clientEndPoint;
        }
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}