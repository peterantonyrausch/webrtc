using System.Collections.Concurrent;
using SIPSorcery.Net;

namespace SignalingServer
{
    public class User
    {
        public string Id { get; set; }
        public ConcurrentDictionary<string, RTCPeerConnection> GroupConnections;
        public int ConnectionsCount => GroupConnections.Count;

        public User(string id)
        {
            Id = id;
            GroupConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        }

        public void AddGroupConnection(string groupId, RTCPeerConnection peerConnection)
        {
            GroupConnections.TryAdd(groupId, peerConnection);
        }

        public void RemoveGroupConnection(string groupId)
        {
            GroupConnections.TryRemove(groupId, out _);
        }
    }
}