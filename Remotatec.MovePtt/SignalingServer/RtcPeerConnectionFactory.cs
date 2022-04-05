using SIPSorcery.Media;
using SIPSorcery.Net;

namespace SignalingServer
{
    public static class RtcPeerConnectionFactory
    {
        public static Task<RTCPeerConnection> CreatePeerConnectionAsync()
        {
            var peerConnection = CreateRtcPeerConnection();

            var audioSource = SetupAudioEndpoint();

            AddMediaToPeerConnection(audioSource, peerConnection);

            ConfigureAudioEventHandlers(audioSource, peerConnection);

            ConfigurePeerConnectionEventHandlers(peerConnection, audioSource);

            peerConnection.OnAudioFormatsNegotiated += audioFormats =>
            {
                audioSource.SetAudioSourceFormat(audioFormats.First());
            };

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

        private static void AddMediaToPeerConnection(AudioExtrasSource audioSource, RTCPeerConnection peerConnection)
        {
            var audioTrack = new MediaStreamTrack(audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendRecv);
            peerConnection.addTrack(audioTrack);
        }

        private static void ConfigureAudioEventHandlers(AudioExtrasSource audioSource, RTCPeerConnection peerConnection)
        {
            audioSource.OnAudioSourceEncodedSample += peerConnection.SendAudio;
        }

        private static void ConfigurePeerConnectionEventHandlers(RTCPeerConnection peerConnection, AudioExtrasSource audioSource)
        {
            peerConnection.onconnectionstatechange += async state => await OnConnectionStateChanged(state, audioSource, peerConnection);

            // Diagnostics.
            peerConnection.OnReceiveReport += (re, media, rr) => Console.WriteLine($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
            peerConnection.OnSendReport += (media, sr) => Console.WriteLine($"RTCP Send for {media}\n{sr.GetDebugSummary()}");
            peerConnection.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => Console.WriteLine($"STUN {msg.Header.MessageType} received from {ep}.");
            peerConnection.oniceconnectionstatechange += (state) => Console.WriteLine($"ICE connection state change to {state}.");

            peerConnection.OnRtpPacketReceived += PeerConnection_OnRtpPacketReceived;
        }

        private static void PeerConnection_OnRtpPacketReceived(System.Net.IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt)
        {
            Console.WriteLine("RtpPackatReceived...");
        }

        private static async Task OnConnectionStateChanged(RTCPeerConnectionState state, AudioExtrasSource audioSource, RTCPeerConnection peerConnection)
        {
            Console.WriteLine($"Peer connection state change to {state}.");

            if (state == RTCPeerConnectionState.connected)
            {
                await audioSource.StartAudio();
                return;
            }

            if (state == RTCPeerConnectionState.closed)
            {
                await audioSource.CloseAudio();
                return;
            }

            if (state == RTCPeerConnectionState.failed)
            {
                peerConnection.Close("ice disconnection");
            }
        }
    }
}