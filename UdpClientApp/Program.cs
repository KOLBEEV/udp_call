using UdpClientApp;

string serverIp = "127.0.0.1";
const int serverPort = 5000;
const int clientPort = 5001;

using CancellationTokenSource cancellationTokenSource = new();

using ClientApp app = new ClientApp(
    clientPort,
    serverIp,
    serverPort
);

await app.RunAsync(cancellationTokenSource);
