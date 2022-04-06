using SignalingServer.Dtos;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace SignalingServer
{
    public class SdpExchangeWebSocketPeer : WebSocketBehavior
    {
        public string UserId { get; set; }

        public event Action<SdpExchangeWebSocketPeer, WebSocketMessage> OnMessageReceived;

        protected override void OnMessage(MessageEventArgs e)
        {
            var message = JsonUtils.Deserialize<WebSocketMessage>(e.Data);

            if (message?.Action == "LOGIN")
            {
                var payload = JsonUtils.Deserialize<LoginInput>(message.Payload);
                UserId = payload.UserId;

                return;
            }

            if (message?.Action == "CONNECT_TO_GROUP")
            {
                var payload = JsonUtils.Deserialize<ConnectToGroupInput>(message.Payload);

                var group = Database.GetGroup(payload.GroupId);
                var user = Database.GetUser(payload.UserId);

                var peerConnectionInGroup = RtcPeerConnectionFactory.CreatePeerConnectionAsync(group, user).GetAwaiter().GetResult();
                var sdpOffer = peerConnectionInGroup.createOffer(null);
                peerConnectionInGroup.setLocalDescription(sdpOffer).GetAwaiter().GetResult();

                var offerMessage = JsonUtils.Serialize(
                    new WebSocketMessage
                    {
                        Action = "SDP",
                        Payload = JsonUtils.Serialize(new
                        {
                            GroupId = message.Payload,
                            Offer = sdpOffer.toJSON()
                        })
                    }
                );

                Context.WebSocket.Send(offerMessage);

                return;
            }

            if (message is not null)
            {
                OnMessageReceived(this, message);
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            var user = Database.GetUser(UserId);

            foreach (var peerConnection in user.GroupConnections.Values)
            {
                peerConnection.Close("websocket closed");
                peerConnection.Dispose();
            }

            user.GroupConnections.Clear();
        }
    }
}