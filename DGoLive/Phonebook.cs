using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.ComponentModel;

namespace DGoLive
{
    [Serializable]
    class Phonebook : List<Remote>
    {
        public void WriteXML(string FileName)
        {
            using (Stream stream = File.Open(FileName, FileMode.Create))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                bformatter.Serialize(stream, this);
            }
        }

        public void ReadXML(string FileName)
        {
            using (Stream stream = File.Open(FileName, FileMode.OpenOrCreate))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                this.Clear();
                if (stream.Length > 0)
                    this.AddRange((Phonebook)bformatter.Deserialize(stream));
            }
        }
    }
    [Serializable]
    class Remote
    {
        [Category("General")]
        public string Name { get; set; }
        [Category("IP Endpoint")]
        public string IPAddress { get; set; }
        [Category("IP Endpoint")]
        public int Port { get; set; }
        [Category("Decoder Settings")]
        public CodecType CodecType { get; set; }
        public IPEndPoint GetIPEndPoint() 
        { 
            return new IPEndPoint(System.Net.IPAddress.Parse(IPAddress), Port);
        }
    }

    [Serializable]
    class Settings
    {
        public Phonebook Remotes { get; } = new Phonebook();
        public string PhonebookFilename { get; set; } = "phonebook.dat";
    }
}
