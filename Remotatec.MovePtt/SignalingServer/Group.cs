using System.Collections.Concurrent;
using SIPSorcery.Net;

namespace SignalingServer
{
    public class Group
    {
        public string Id { get; set; }
        public ConcurrentDictionary<string, RTCPeerConnection> UserConnections;
        public int ConnectionsCount => UserConnections.Count;

        public Group(string id)
        {
            Id = id;
            UserConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        }

        public void AddUserConnection(string userId, RTCPeerConnection peerConnection)
        {
            UserConnections.TryAdd(userId, peerConnection);
        }

        public void RemoveUserConnection(string userId)
        {
            UserConnections.TryRemove(userId, out _);
        }

        public void SendAudioRtpRaw(string userIdSender, RTPPacket packet)
        {
            foreach (var userConnection in UserConnections)
            {
                if (userConnection.Key != userIdSender)
                {
                    userConnection.Value.SendRtpRaw(
                        SDPMediaTypesEnum.audio,
                        packet.Payload,
                        packet.Header.Timestamp,
                        packet.Header.MarkerBit,
                        packet.Header.PayloadType
                    );
                }
            }
        }
    }
}