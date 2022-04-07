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