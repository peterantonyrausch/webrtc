using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

namespace SignalingServer
{
    public static class RtcPeerConnectionFactory
    {
        public static Task<RTCPeerConnection> CreatePeerConnectionAsync(Group group, User user)
        {
            var peerConnection = CreateRtcPeerConnection();

            user.AddGroupConnection(group.Id, peerConnection);
            group.AddUserConnection(user.Id, peerConnection);

            AddMediaToPeerConnection(peerConnection);

            ConfigurePeerConnectionEventHandlers(group, user, peerConnection);

            return Task.FromResult(peerConnection);
        }

        private static RTCPeerConnection CreateRtcPeerConnection()
        {
            var rtcConfiguration = CreateRtcConfiguration();
            var peerConnection = new RTCPeerConnection(rtcConfiguration);

            return peerConnection;
        }

        private static RTCConfiguration CreateRtcConfiguration()
        {
            return new RTCConfiguration
            {
                iceServers = new List<RTCIceServer>
                {
                    new RTCIceServer
                    {
                        urls = "stun:stun.sipsorcery.com"
                    }
                }
            };
        }

        private static AudioExtrasSource SetupAudioEndpoint()
        {
            return new AudioExtrasSource(new AudioEncoder(), new AudioSourceOptions { AudioSource = AudioSourcesEnum.Music });
        }

        private static void AddMediaToPeerConnection(RTCPeerConnection peerConnection)
        {
            var audioTrack = new MediaStreamTrack(
                SDPMediaTypesEnum.audio,
                false,
                new List<SDPAudioVideoMediaFormat>
                {
                    new SDPAudioVideoMediaFormat(SDPWellKnownMediaFormatsEnum.PCMU)
                }
            );
            peerConnection.addTrack(audioTrack);
        }

        private static void ConfigurePeerConnectionEventHandlers(Group group, User user, RTCPeerConnection peerConnection)
        {
            peerConnection.onconnectionstatechange += async state => await OnConnectionStateChanged(state, group, user);

            // Diagnostics.
            peerConnection.OnReceiveReport += (re, media, rr) => Console.WriteLine($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
            peerConnection.OnSendReport += (media, sr) => Console.WriteLine($"RTCP Send for {media}\n{sr.GetDebugSummary()}");
            peerConnection.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => Console.WriteLine($"STUN {msg.Header.MessageType} received from {ep}.");
            peerConnection.oniceconnectionstatechange += (state) => Console.WriteLine($"ICE connection state change to {state}.");

            peerConnection.OnRtpPacketReceived += (rep, media, rtpPkt) => PeerConnection_OnRtpPacketReceived(rep, media, rtpPkt, group, user);
        }

        private static void PeerConnection_OnRtpPacketReceived(System.Net.IPEndPoint rep, SDPMediaTypesEnum mediaType, RTPPacket rtpPkt, Group group, User user)
        {
            Console.WriteLine($"User '{user.Id}' send packet to {group.ConnectionsCount} user(s) to group '{group.Id}'");

            group.SendAudioRtpRaw(user.Id, rtpPkt);
        }

        private static async Task OnConnectionStateChanged(RTCPeerConnectionState state, Group group, User user)
        {
            Console.WriteLine($"Peer connection state change to {state}.");

            if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed || state == RTCPeerConnectionState.disconnected)
            {
                group.RemoveUserConnection(user.Id);
                user.RemoveGroupConnection(group.Id);
                return;
            }
        }
    }
}