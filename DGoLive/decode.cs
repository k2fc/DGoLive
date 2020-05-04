using System;
using System.Linq;
using softsyst.qirx.audiodecoder;
using softsyst.Generic;
using NAudio.Wave;
using FragLabs.Audio.Codecs;

namespace DGoLive
{
    enum CodecType
    {
        AAC,
        Opus,
        G722,
        G711
    }
    class DGDecoder : IDGDecoder
    {
        IDGDecoder decoder;
        CodecType CodecType;
        public DGDecoder (CodecType Type, int SampleRate, int Channels)
        {
            CodecType = Type;
            switch (CodecType)
            {
                case CodecType.AAC:
                    decoder = new AACDecode();
                    break;
                case CodecType.Opus:
                    decoder = new OpusDecoder(SampleRate, Channels);
                    break;
            }
        }

        public bool processBuffer(byte[] rxBuf, out byte[] pcm16)
        {
            return decoder.processBuffer(rxBuf, out pcm16);
        }

        public void Dispose()
        {
            decoder.Dispose();
        }
    }

    interface IDGDecoder : IDisposable
    {
        bool processBuffer(byte[] rxBuf, out byte[] pcm16);
    }
    class AACDecode : IDGDecoder
    {
        aacDecoder AACDecoder;
        IntPtr hDecoder;

        ConstSizeBuffer audioOutBuf = new ConstSizeBuffer(10000);
        int sample_rate = 0;
        byte channels = 0;
        bool _initialized = false;
        public AACDecode()
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

        public void Dispose()
        {
            AACDecoder.Close(hDecoder);
            AACDecoder.Dispose();
        }

        public bool processBuffer(byte[] rxBuf, out byte[] pcm16)
        {
            pcm16 = null;
            rxBuf = rxBuf.Skip(5).ToArray();
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
    class OpusDecoder :IDGDecoder
    {
        FragLabs.Audio.Codecs.OpusDecoder decoder;
        public OpusDecoder(int SampleRate, int Channels)
        {
            decoder = FragLabs.Audio.Codecs.OpusDecoder.Create(SampleRate, Channels);
        }

        public void Dispose()
        {
            decoder.Dispose();
        }

        public bool processBuffer(byte[] rxBuf, out byte[] pcm16)
        {
            int len;
            pcm16 = null;
            try
            {
                pcm16 = decoder.Decode(rxBuf.Skip(1).ToArray(), rxBuf.Length - 1, out len);
                pcm16 = pcm16.Take(len).ToArray();
                return true;
            }
            catch { }
            return false;
        }
    }
}
