using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace WebAPI.Judging;

public sealed class JudgeBridgeBackgroundService : BackgroundService
{
    private readonly ILogger<JudgeBridgeBackgroundService> _logger;
    private readonly JudgeConnectionRegistry _registry;
    private readonly JudgeDispatchService _dispatchService;
    private TcpListener? _listener;
    private const int Port = 9999;

    public JudgeBridgeBackgroundService(
        ILogger<JudgeBridgeBackgroundService> logger ,
        JudgeConnectionRegistry registry ,
        JudgeDispatchService dispatchService)
    {
        _logger = logger;
        _registry = registry;
        _dispatchService = dispatchService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any , Port);
        _listener.Start();

        _logger.LogInformation("Judge bridge TCP listener started on port {Port}" , Port);

        while ( !stoppingToken.IsCancellationRequested )
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(stoppingToken);
            }
            catch ( OperationCanceledException )
            {
                break;
            }

            _ = Task.Run(() => HandleClientAsync(client , stoppingToken) , stoppingToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client , CancellationToken cancellationToken)
    {
        string? judgeId = null;

        try
        {
            _logger.LogInformation("Incoming judge TCP connection from {RemoteEndPoint}" , client.Client.RemoteEndPoint);

            using var stream = client.GetStream();

            var packet = await BridgePacketCodec.ReadPacketAsync(stream , cancellationToken);
            if ( packet is null )
            {
                _logger.LogWarning("Handshake packet is null.");
                client.Close();
                return;
            }

            var packetName = GetString(packet , "name");
            judgeId = GetString(packet , "id");
            var key = GetString(packet , "key");

            _logger.LogInformation("Received first packet: {PacketName} from judge {JudgeId}" , packetName , judgeId);

            if ( !string.Equals(packetName , "handshake" , StringComparison.Ordinal) )
            {
                _logger.LogWarning("First packet is not handshake.");
                client.Close();
                return;
            }

            if ( judgeId != "judge1" || key != "secretkey" )
            {
                _logger.LogWarning("Judge auth failed for {JudgeId}" , judgeId);
                await BridgePacketCodec.WritePacketAsync(stream , new
                {
                    name = "handshake-failed"
                } , cancellationToken);
                client.Close();
                return;
            }

            _registry.Register(judgeId! , client);

            await BridgePacketCodec.WritePacketAsync(stream , new
            {
                name = "handshake-success"
            } , cancellationToken);

            _logger.LogInformation("Judge {JudgeId} authenticated successfully." , judgeId);

            while ( !cancellationToken.IsCancellationRequested && client.Connected )
            {
                var nextPacket = await BridgePacketCodec.ReadPacketAsync(stream , cancellationToken);
                if ( nextPacket is null )
                {
                    _logger.LogInformation("Judge {JudgeId} disconnected." , judgeId);
                    break;
                }

                _dispatchService.HandlePacket(judgeId! , nextPacket);
            }
        }
        catch ( OperationCanceledException )
        {
        }
        catch ( Exception ex )
        {
            _logger.LogError(ex , "Judge connection crashed.");
        }
        finally
        {
            if ( !string.IsNullOrWhiteSpace(judgeId) )
                _registry.Remove(judgeId);

            try { client.Close(); } catch { }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try { _listener?.Stop(); } catch { }
        return base.StopAsync(cancellationToken);
    }

    private static string? GetString(Dictionary<string , JsonElement> packet , string key)
    {
        if ( !packet.TryGetValue(key , out var value) )
            return null;

        if ( value.ValueKind == JsonValueKind.String )
            return value.GetString();

        return value.ToString();
    }
}