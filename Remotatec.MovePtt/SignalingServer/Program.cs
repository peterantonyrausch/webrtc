using System.Net;
using System.Text.Json;
using SignalingServer;
using SignalingServer.Dtos;
using SIPSorcery.Net;
using WebSocketSharp.Server;

const int WEBSOCKET_PORT = 8081;

// Start web socket.
Console.WriteLine("Starting web socket server...");

var webSocketServer = new WebSocketServer(IPAddress.Any, WEBSOCKET_PORT);

webSocketServer.AddWebSocketService<SdpExchangeWebSocketPeer>("/", (sdpExchanger) =>
{
    sdpExchanger.OnMessageReceived += WebSocketMessageReceived;
});

void WebSocketMessageReceived(SdpExchangeWebSocketPeer socket, WebSocketMessage message)
{
    try
    {
        if (message?.Action == "SDP")
        {
            var payload = JsonUtils.Deserialize<SdpInput>(message.Payload);

            var user = Database.GetUser(payload.UserId);

            if (user.GroupConnections.TryGetValue(payload.GroupId, out var peerConnection))
            {
                if (RTCSessionDescriptionInit.TryParse(payload.SdpInfo, out var remoteDescription))
                {
                    peerConnection?.setRemoteDescription(remoteDescription);
                }
            }
        }

        if (message?.Action == "ICE_CANDIDATE")
        {
            var payload = JsonUtils.Deserialize<IceCandidateInput>(message.Payload);

            var user = Database.GetUser(payload.UserId);

            if (user.GroupConnections.TryGetValue(payload.GroupId, out var peerConnection))
            {
                if (RTCIceCandidateInit.TryParse(payload.IceCandidateInfo, out var iceCandidate))
                {
                    peerConnection?.addIceCandidate(new RTCIceCandidateInit { candidate = iceCandidate.candidate });
                }
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"PAUNAKOMBI: {e}");
    }
}

webSocketServer.Start();

Console.WriteLine($"Waiting for web socket connections on {webSocketServer.Address}:{webSocketServer.Port}...");

Console.ReadLine();
