using System;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using FragLabs.Audio.Codecs;
using System.Net;
using SIPSorcery.Net;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;

namespace DGoLive
{
    public partial class Form1 : Form
    {
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                inputList.Items.Add(WaveIn.GetCapabilities(i).ProductName);
            }
            if (inputList.Items.Count > 0)
                inputList.SelectedIndex = 0;
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                outputList.Items.Add(WaveOut.GetCapabilities(i).ProductName);
            }
            if (outputList.Items.Count > 0)
                outputList.SelectedIndex = 0;

            settings.Remotes.ReadXML(settings.PhonebookFilename);
            foreach (Remote remote in settings.Remotes)
            {
                comboBox1.Items.Add(remote.Name);
            }
            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
            playersForm = new PlayersForm(new WaveFormat(48000,16,1));
        }

        OpusEncoder encoder;
        
        WaveIn waveIn;
        WaveOut waveOut;
        BufferedWaveProvider liveInput;
        MixingSampleProvider MixingSampleProvider;

        int bytesPerSegment;
        BufferedWaveProvider playBuffer;
        Timer timer = null;
        ComrexSession session;

        IDGDecoder Decoder;

        int rxpacketcount;
        int lostpacketcount;
        ushort lastseq;

        int segmentFrames;
        IPEndPoint farEnd;

        double sendAudiolevel = 0;
        double rxAudioLevel = 0;
        double sendGain = 0;
        double rxGain = 0;
        bool killsession = false;

        

        Settings settings = new Settings();

        PlayersForm playersForm;

        private void button1_Click(object sender, EventArgs e)
        {
            playersForm.StopAll();
            Remote remote = settings.Remotes[comboBox1.SelectedIndex];
            try
            {
                farEnd = remote.GetIPEndPoint();
            }
            catch
            {
                MessageBox.Show("This is not a valid IP Address");
                return;
            }
            button2.Enabled = true;
            button1.Enabled = false;
            label5.Visible = true;
            segmentFrames = 960;
            encoder = OpusEncoder.Create(48000, 1, FragLabs.Audio.Codecs.Opus.Application.Audio);
            Decoder = new DGDecoder(remote.CodecType, 48000, 1);
            encoder.Bitrate = 64000;
            bytesPerSegment = encoder.FrameByteCount(segmentFrames);

            waveIn = new WaveIn(WaveCallbackInfo.FunctionCallback());
            
            waveIn.DeviceNumber = inputList.SelectedIndex;
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveIn.WaveFormat = new WaveFormat(48000, 16, 1);

            liveInput = new BufferedWaveProvider(waveIn.WaveFormat);
            List<ISampleProvider> sampleProviders = new List<ISampleProvider>();
            sampleProviders.Add(liveInput.ToSampleProvider());
            sampleProviders.Add(playersForm.Mixer);
            MixingSampleProvider = new MixingSampleProvider(sampleProviders);
            

            playBuffer = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            playBuffer.DiscardOnBufferOverflow = true;
            waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.DeviceNumber = outputList.SelectedIndex;
            playersForm.PlaybackDeviceNum = outputList.SelectedIndex;
            

            
            session = new ComrexSession();
            session.SetDestination(SDPMediaTypesEnum.audio, farEnd, farEnd);
            session.OnRtpPacketReceived += Session_OnRtpPacketReceived;
            session.Start();
            killsession = false;

            waveOut.Init(playBuffer);
            waveIn.StartRecording();


            if (timer == null)
            {
                timer = new Timer();
                timer.Interval = 100;
                timer.Tick += timer_Tick;
            }
            timer.Start();

            comboBox1.Enabled = false;
            outputList.Enabled = false;
            inputList.Enabled = false;

            
        }


        

        
        private void Session_OnRtpPacketReceived(SDPMediaTypesEnum arg1, RTPPacket arg2)
        {
            RTPPacket packet = arg2;
            if (packet.Header.PayloadType == 0 || killsession)
            {
                killsession = true;
                return;
            }
            rxpacketcount++;
            if (packet.Header.SequenceNumber != lastseq + 1 && lastseq != 0)
            {
                lostpacketcount++;
            }
                
            lastseq = packet.Header.SequenceNumber;
            
            int len = 0;
            try
            {
                if (playBuffer.BufferedDuration.TotalMilliseconds < 250)
                {
                    byte[] decoded = new byte[] { 0 };
                    Decoder.processBuffer(packet.Payload, out decoded);
                    len = decoded.Length;
                    //decoded = decoder.Decode(encoded, encoded.Length, out len);
                    rxAudioLevel = AudioLevelDB(decoded);
                    decoded = AdjustAudioLevelDB(decoded, rxGain);
                    
                    playBuffer.AddSamples(decoded, 0, len);
                }
            }
            catch (Exception ex)
            {
               
            }
            if ((int)playBuffer.BufferedDuration.TotalMilliseconds > 20)
            {
                waveOut.Play();
            }
            else
            {
                //waveOut.Pause();
            }
        }

        

        private void timer_Tick(object sender, EventArgs e)
        {
            updateAudioMeters();
            if (killsession)
                button2_Click(null, null);
        }
        
        private void updateAudioMeters()
        {
            if (rxAudioLevel > -24)
                progressBar2.Value = (int)(rxAudioLevel + 24);
            else
                progressBar2.Value = 0;

            if (sendAudiolevel > -24)
                progressBar1.Value = (int)(sendAudiolevel + 24);
            else
                progressBar1.Value = 0;

            label5.Text = "RX Buffer: " + playBuffer.BufferedDuration.TotalMilliseconds.ToString() + "mS";
        }

        private double AudioLevelDB(byte[] buffer)
        {
            //meter
            float max = 0;
            // interpret as 16 bit audio
            for (int index = 0; index < buffer.Length; index += 2)
            {
                short sample = (short)((buffer[index + 1] << 8) |
                                        buffer[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                // is this the max value?
                if (sample32 > max) max = sample32;
            }

            return 10 * Math.Log10((double)max);
        }

        private byte[] AdjustAudioLevelDB (byte[] input, double gain)
        {
            for (int i = 0; i < input.Length; i+=2)
            {
                short sample = (short)((input[i + 1] << 8) | input[i]);
                var sample32 = sample / 32768f;
                sample32 *= (float)Math.Pow(10, (gain / 10));
                sample = (short)(sample32 * 32768);
                input[i + 1] = (byte)(sample >> 8);
                input[i] = (byte)(sample);
            }
            return input;
        }

        byte[] notEncodedBuffer = new byte[0];
        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] liveBytes = AdjustAudioLevelDB(e.Buffer, sendGain);
            liveInput.AddSamples(liveBytes, 0, e.BytesRecorded);
            byte[] mixed = new byte[e.BytesRecorded];
            MixingSampleProvider.ToWaveProvider16().Read(mixed, 0, e.BytesRecorded);
            byte[] soundBuffer = new byte[e.BytesRecorded + notEncodedBuffer.Length];
            for (int i = 0; i < notEncodedBuffer.Length; i++)
                soundBuffer[i] = notEncodedBuffer[i];
            for (int i = 0; i < e.BytesRecorded; i++)
                soundBuffer[i + notEncodedBuffer.Length] = mixed[i];
            
            sendAudiolevel = AudioLevelDB(soundBuffer);
            int byteCap = bytesPerSegment;
            int segmentCount = (int)Math.Floor((decimal)soundBuffer.Length / byteCap);
            int segmentsEnd = segmentCount * byteCap;
            int notEncodedCount = soundBuffer.Length - segmentsEnd;
            notEncodedBuffer = new byte[notEncodedCount];

            for (int i = 0; i < notEncodedCount; i++)
            {
                notEncodedBuffer[i] = soundBuffer[segmentsEnd + i];
            }
            for (int i = 0; i < segmentCount; i++)
            {
                byte[] segment = new byte[byteCap];
                for (int j = 0; j < segment.Length; j++)
                    segment[j] = soundBuffer[(i * byteCap) + j];
                int len;
                byte[] buff = encoder.Encode(segment, segment.Length, out len);
                byte[] newbuff = new byte[++len];
                newbuff[0] = 16;
                for (int j = 1; j < newbuff.Length; j++)
                    newbuff[j] = buff[j-1];
                session.SendAudioFrame((uint)segmentFrames, 21, newbuff);
                playersForm.IsEncoding = true;
            }
            if (playBuffer.BufferedDuration.TotalMilliseconds > 10)
            {
                button1.ForeColor = Color.Black;
                button1.BackColor = Color.Green;
            }
            else
            {
                button1.ForeColor = Color.White;
                button1.BackColor = Color.Red;
                
            }
               

        }
        private void button2_Click(object sender, EventArgs e)
        {
            playersForm.StopAll();
            if (button2.Enabled)
            {
                button2.Enabled = false;
                waveIn.StopRecording();
                while (notEncodedBuffer.Length > bytesPerSegment) { }
                System.Threading.Thread.Sleep(2*waveIn.BufferMilliseconds);
                session.SendGoodbyeFrame();
                session.OnRtpPacketReceived -= Session_OnRtpPacketReceived;
                session = null;
                button1.Enabled = true;
                comboBox1.Enabled = true;
                outputList.Enabled = true;
                inputList.Enabled = true;
                label5.Visible = false;
                button1.ForeColor = Color.Black;
                button1.BackColor = SystemColors.Control;
                playersForm.IsEncoding = false;
                Decoder.Dispose();
            }

        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            sendGain = trackBar1.Value /10;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            rxGain = trackBar2.Value / 10;
        }

        private void Form1_Closing(object sender, EventArgs e)
        {
            button2_Click(sender, e);
            playersForm.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //listBox1 is the object containing the collection.  Remember, if the collection
            //belongs to the class you're editing, you can use this
            //Items is the name of the property that is the collection you wish to edit.
            PropertyDescriptor pd = TypeDescriptor.GetProperties(settings)["Remotes"];
            UITypeEditor editor = (UITypeEditor)pd.GetEditor(typeof(UITypeEditor));
            RuntimeServiceProvider serviceProvider = new RuntimeServiceProvider();
            editor.EditValue(serviceProvider, serviceProvider, settings.Remotes);
            settings.Remotes.WriteXML(settings.PhonebookFilename);
            comboBox1.Items.Clear();
            foreach (Remote remote in settings.Remotes)
            {
                comboBox1.Items.Add(remote.Name);
            }
            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
        }

        private void outputList_SelectedIndex_Changed (object sender, EventArgs e)
        {
            if (waveOut != null)
                waveOut.DeviceNumber = outputList.SelectedIndex;
            if (playersForm != null)
                playersForm.PlaybackDeviceNum = outputList.SelectedIndex;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            playersForm.Show();
        }
    }
    
}
