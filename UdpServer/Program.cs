using UdpServer;

const int serverPort = 5000;

using CancellationTokenSource cancellationTokenSource = new();

using ServerApp app = new ServerApp(serverPort);

await app.RunAsync(cancellationTokenSource);
