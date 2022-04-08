namespace SignalingServer
{
    public class WebSocketMessage
    {
        public string Action { get; set; }
        public string Payload { get; set; }

        public override string ToString()
        {
            return $"ACTION={Action}{Environment.NewLine}PAYLOAD={Payload}";
        }
    }
}