using System.Net;
using System.Text.Json;
using SignalingServer;
using SIPSorcery.Net;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

const int WEBSOCKET_PORT = 8081;

// Start web socket.
Console.WriteLine("Starting web socket server...");

var webSocketServer = new WebSocketServer(IPAddress.Any, WEBSOCKET_PORT);

webSocketServer.AddWebSocketService<SdpExchangeWebSocketPeer>("/", (sdpExchanger) =>
{
    sdpExchanger.WebSocketOpened += SendSdpOffer;
    sdpExchanger.OnMessageReceived += WebSocketMessageReceived;
});

void WebSocketMessageReceived(RTCPeerConnection peerConnection, string message)
{
    try
    {
        if (peerConnection.remoteDescription is null)
        {
            Console.WriteLine($"Answer SDP: {message}");

            if (RTCSessionDescriptionInit.TryParse(message, out var remoteDescription))
            {
                peerConnection.setRemoteDescription(remoteDescription);
            }
        }
        else
        {
            Console.WriteLine($"ICE Candidate: {message}");

            if (RTCIceCandidateInit.TryParse(message, out var iceCandidate))
            {
                peerConnection.addIceCandidate(new RTCIceCandidateInit { candidate = iceCandidate.candidate});
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"PAUNAKOMBI: {e}");
    }
}

async Task<RTCPeerConnection> SendSdpOffer(WebSocketContext webSocketContext)
{
    var peerConnection = await RtcPeerConnectionFactory.CreatePeerConnectionAsync();
    var sdpOffer = peerConnection.createOffer(null);
    await peerConnection.setLocalDescription(sdpOffer);

    webSocketContext.WebSocket.Send(sdpOffer.toJSON());

    return peerConnection;
}

webSocketServer.Start();

Console.WriteLine($"Waiting for web socket connections on {webSocketServer.Address}:{webSocketServer.Port}...");

Console.ReadLine();