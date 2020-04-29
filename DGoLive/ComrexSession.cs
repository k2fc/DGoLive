using System.Collections.Generic;
using SIPSorcery.Net;

namespace DGoLive
{
    class ComrexSession : RTPSession
    {
        private const int RTP_MAX_PAYLOAD = 1400;
        internal MediaStreamTrack AudioLocalTrack { get; private set; }
        private int sent;
        public ComrexSession() : base(false, false, false)
        {
            List<SDPMediaFormat> capabilities = new List<SDPMediaFormat>();
            capabilities.Add(new SDPMediaFormat(21));
            AudioLocalTrack = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, capabilities);
            addTrack(AudioLocalTrack);
            sent = 0;
            
        }

        public new void SendAudioFrame(uint duration, int payloadTypeID, byte[] buffer)
        {
            if (sent == 1)
            {
                base.SendAudioFrameWithMarker(duration, payloadTypeID, buffer);
            }
            else
            {
                base.SendAudioFrame(duration, payloadTypeID, buffer);
            }
            sent++;
        }

        public void SendGoodbyeFrame()
        {
            SendAudioFrame(960, 0, new byte[0]);
            
        }
        
    }
}
