using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DGoLive
{
    public partial class PlayersForm : Form
    {
        OpenFileDialog dialog = new OpenFileDialog();
        public List<Player> Players { get; } = new List<Player>();
        public int PlaybackDeviceNum { get; set; } = 0;
        WaveFormat WaveFormat;
        public MixingSampleProvider Mixer { get; private set; }
        public bool IsEncoding { get; set; }
        public PlayersForm(WaveFormat format)
        {
            InitializeComponent();
            //dialog. = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //dialog.RestoreDirectory = true;
            dialog.Title = "Choose an audio clip to load";
            dialog.Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*";
            this.WaveFormat = format;
            var dummySampleProvider = new SilenceProvider(format).ToSampleProvider();
            var listofonesampleprovider = new List<ISampleProvider>();
            listofonesampleprovider.Add(dummySampleProvider);
            Mixer = new MixingSampleProvider(listofonesampleprovider);
        }

        public void StopAll()
        {
            foreach(Player player in Players)
            {
                player.Stop();
            }
        }

        private void btnAddClip_Click(object sender, EventArgs e)
        {
           
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                string filename = dialog.FileName;
                Player newplayer = new Player(filename, WaveFormat, this);
                Players.Add(newplayer);
                ReloadPlayers();
            }
                
        }

        public void ReloadPlayers()
        {
            Mixer.RemoveAllMixerInputs();
            this.Controls.Clear();
            int bottom = 0;
            foreach (Player player in Players)
            {
                if (!player.GroupBox.IsDisposed)
                {
                    player.GroupBox.Top = bottom + 6;
                    this.Controls.Add(player.GroupBox);
                    bottom = player.GroupBox.Bottom;
                    Mixer.AddMixerInput(player.Buffer.ToSampleProvider());
                }
            }
            btnAddClip.Top = bottom + 6;
            this.Height = btnAddClip.Bottom + 50;
            this.Controls.Add(btnAddClip);
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }

    public class Player : IDisposable
    {
        private AudioFileReader reader;
        private IWaveProvider converter;
        private WaveOutEvent audioPlayer;
        private PlayersForm CallingForm;
        private int clipBytes = 0;
        public BufferedWaveProvider Buffer { get; private set; }
        private Timer timer = new Timer()
        {
            Interval = 50,
            Enabled = false
        };
        private Button PlayButton = new Button()
        {
            Size = new Size(81, 81),
            Text = "Play",
            Left = 88,
            Top = 19
        };

        private Button UpButton = new Button()
        {
            Left = 6,
            Top = 19,
            Size = new Size(76, 23),
            Text = "Move Up"
        };

        private Button DeleteButton = new Button()
        {
            Left = 6,
            Top = 48,
            Size = new Size(76, 23),
            Text = "Delete"
        };

        private Button DownButton = new Button()
        {
            Left = 6,
            Top = 77,
            Size = new Size(76, 23),
            Text = "Move Down"
        };

        public GroupBox GroupBox { get; } = new GroupBox()
        {
            Size = new Size(184, 109),
            Left = 12
        };

        public Player(string filename, WaveFormat format, PlayersForm form)
        {
            CallingForm = form;
            GroupBox.Text = Path.GetFileName(filename);

            try
            {
                reader = new AudioFileReader(filename);
                converter = new WdlResamplingSampleProvider(reader, format.SampleRate).ToMono().ToWaveProvider16();
                clipBytes = (int)(((double)converter.WaveFormat.AverageBytesPerSecond / (double)reader.WaveFormat.AverageBytesPerSecond) * reader.Length);
                if (converter.WaveFormat.AverageBytesPerSecond != format.AverageBytesPerSecond)
                {
                    MessageBox.Show("Audio file is wrong format.  Should be " + format.ToString());
                    this.Dispose();
                    return;
                }
                audioPlayer = new WaveOutEvent();
                audioPlayer.Init(converter);
                Buffer = new BufferedWaveProvider(format);
                Buffer.BufferDuration = reader.TotalTime;
                Buffer.DiscardOnBufferOverflow = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"Error loading audio file",MessageBoxButtons.OK,MessageBoxIcon.Error);
                this.Dispose();
            }
            GroupBox.Controls.Add(PlayButton);
            GroupBox.Controls.Add(DeleteButton);
            GroupBox.Controls.Add(UpButton);
            GroupBox.Controls.Add(DownButton);

            PlayButton.Click += PlayButton_Click;
            DeleteButton.Click += DeleteButton_Click;
            UpButton.Click += UpButton_Click;
            DownButton.Click += DownButton_Click;
            PlayButton.BackColor = Color.Green;
        }

        private void AudioPlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            Buffer.ClearBuffer();
            timer.Stop();
            PlayButton.Text = "Play";
            PlayButton.BackColor = Color.Green;
            reader.CurrentTime = TimeSpan.Zero;
            DeleteButton.Enabled = true;
            
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            this.Dispose();
            CallingForm.ReloadPlayers();
        }

        private void DownButton_Click(object sender, EventArgs e)
        {
            if (this != CallingForm.Players.Last())
            {
                int oldIndex = CallingForm.Players.IndexOf(this);
                CallingForm.Players.Remove(this);
                CallingForm.Players.Insert(oldIndex+=1, this);
            }
            CallingForm.ReloadPlayers();
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            if (this != CallingForm.Players.First())
            {
                int oldIndex = CallingForm.Players.IndexOf(this);
                CallingForm.Players.Remove(this);
                CallingForm.Players.Insert(oldIndex -= 1, this);
            }
            CallingForm.ReloadPlayers();
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (audioPlayer.PlaybackState == PlaybackState.Stopped)
            {
                if (CallingForm.IsEncoding)
                {
                    reader.Position = 0;
                    Buffer.ClearBuffer();
                    byte[] clipBuffer = new byte[clipBytes];
                    converter.Read(clipBuffer, 0, clipBytes);
                    Buffer.AddSamples(clipBuffer, 0, clipBytes);
                    PlayButton.BackColor = Color.Lime;
                }
                else
                {
                    PlayButton.BackColor = Color.Yellow;
                }
                reader.Position = 0;
                audioPlayer.DeviceNumber = CallingForm.PlaybackDeviceNum;
                audioPlayer.Init(reader);
                audioPlayer.PlaybackStopped += AudioPlayer_PlaybackStopped;
                audioPlayer.Play();
                
                
                DeleteButton.Enabled = false;
                timer.Start();
                timer.Tick += Timer_Tick;
            }
            else
            {
                Stop();
            }
        }

        public void Stop()
        {
            audioPlayer.Stop();
            Buffer.ClearBuffer();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (audioPlayer.PlaybackState == PlaybackState.Playing && Buffer.BufferedBytes > 0)
            {
                PlayButton.Text = Buffer.BufferedDuration.TotalSeconds.ToString("N1");
            }
            else if (audioPlayer.PlaybackState == PlaybackState.Playing)
            {
                PlayButton.Text = (reader.TotalTime - reader.CurrentTime).TotalSeconds.ToString("N1");
            }
        }

        public void Dispose()
        {
            PlayButton.Click -= PlayButton_Click;
            DeleteButton.Click -= DeleteButton_Click;
            UpButton.Click -= UpButton_Click;
            DownButton.Click -= DownButton_Click;
            GroupBox.Dispose();
            PlayButton.Dispose();
            DeleteButton.Dispose();
            UpButton.Dispose();
            DownButton.Dispose();
            if (reader != null)
                reader.Dispose();
            if (Buffer != null)
                Buffer.BufferLength = 0;
        }
    }
}
