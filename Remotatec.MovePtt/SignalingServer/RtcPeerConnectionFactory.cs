using System.Collections.Concurrent;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

namespace SignalingServer
{
    public static class RtcPeerConnectionFactory
    {
        private static ConcurrentDictionary<string, RTCPeerConnection> _peers = new();

        public static Task<RTCPeerConnection> CreatePeerConnectionAsync()
        {
            var peerConnection = CreateRtcPeerConnection();
            _peers.TryAdd(peerConnection.SessionID, peerConnection);

            AddMediaToPeerConnection(peerConnection);

            ConfigurePeerConnectionEventHandlers(peerConnection);

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

        private static void ConfigurePeerConnectionEventHandlers(RTCPeerConnection peerConnection)
        {
            peerConnection.onconnectionstatechange += async state => await OnConnectionStateChanged(state, peerConnection);

            // Diagnostics.
            peerConnection.OnReceiveReport += (re, media, rr) => Console.WriteLine($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
            peerConnection.OnSendReport += (media, sr) => Console.WriteLine($"RTCP Send for {media}\n{sr.GetDebugSummary()}");
            peerConnection.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => Console.WriteLine($"STUN {msg.Header.MessageType} received from {ep}.");
            peerConnection.oniceconnectionstatechange += (state) => Console.WriteLine($"ICE connection state change to {state}.");

            peerConnection.OnRtpPacketReceived += (rep, media, rtpPkt) => PeerConnection_OnRtpPacketReceived(rep, media, rtpPkt, peerConnection);
        }

        private static void PeerConnection_OnRtpPacketReceived(System.Net.IPEndPoint rep, SDPMediaTypesEnum mediaType, RTPPacket rtpPkt, RTCPeerConnection peerConnection)
        {
            Console.WriteLine("RtpPackatReceived...");

            foreach (var peer in _peers)
            {
                if (peer.Key != peerConnection.SessionID)
                {
                    peer.Value.SendRtpRaw(mediaType, rtpPkt.Payload, rtpPkt.Header.Timestamp, rtpPkt.Header.MarkerBit, rtpPkt.Header.PayloadType);
                }
            }
        }

        private static async Task OnConnectionStateChanged(RTCPeerConnectionState state, RTCPeerConnection peerConnection)
        {
            Console.WriteLine($"Peer connection state change to {state}.");

            if (state == RTCPeerConnectionState.connected)
            {
                return;
            }

            if (state == RTCPeerConnectionState.closed)
            {
                return;
            }

            if (state == RTCPeerConnectionState.failed)
            {
                peerConnection.Close("ice disconnection");
            }
        }
    }
}