using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using softsyst.qirx.audiodecoder;
using softsyst.Generic;
using NAudio.Wave;

namespace DGoLive
{
    class Decode
    {
        aacDecoder AACDecoder;
        IntPtr hDecoder;

        ConstSizeBuffer audioOutBuf = new ConstSizeBuffer(10000);
        int sample_rate = 48000;
        byte channels = 1;
        bool _initialized = false;
        public Decode()
        {
            AACDecoder = new aacDecoder();
            hDecoder = AACDecoder.open();
        }

        private bool initialize (byte[] rxBuf)
        {
            int result = AACDecoder.init(hDecoder, rxBuf, ref sample_rate, ref channels);
            if (result != 0)
                return false;
            return true;
            
        }

        public bool processBuffer(byte[] rxBuf, out byte[] pcm16)
        {
            pcm16 = null;
            if (!_initialized)
            {
                if (!initialize(rxBuf))
                    return false;
            }

            //next does not include the header (initialized with asc), works with all bitrates
            int bytesConsumed;
            int decoderObjectType = 0;
            int decoderChannels = 0;
            int decoderSamplingRate = 0;
            int startIx = 0;

            int error = AACDecoder.decode(hDecoder, rxBuf, startIx, out pcm16, out bytesConsumed,
                out decoderSamplingRate, out decoderChannels, out decoderObjectType);
            if (error != 0)
            {
                string s = AACDecoder.getErrorMessage((byte)error);
                //logger.Error(s);
                return false;
            }
            
            return true;
        }
    }
}
