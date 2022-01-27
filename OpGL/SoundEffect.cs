using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
//using CSCore.Codecs.WAV;
//using CSCore.SoundOut;

namespace V7
{
    public class SoundEffect : IDisposable
    {
        Stream source;
        WaveFileReader reader;
        WaveOut wav;
        public string Name;
        public bool Loaded { get; private set; }

        public SoundEffect(string path)
        {
            Loaded = false;
            string fName = path.Split('/').Last();
            fName = fName.Substring(0, fName.Length - 4);
            Name = fName;
            source = File.Open(path, FileMode.Open);
            wav = new WaveOut();
            if (path.EndsWith(".wav"))
            {
                reader = new WaveFileReader(source);
            }
            else
                throw new InvalidOperationException("Sound effects can only be .wav files!");
        }

        public void Initialize()
        {
            if (Loaded) return;
            wav.Init(reader);
            Loaded = true;
        }

        public void Play()
        {
            if (!Loaded) return;
            if (wav.PlaybackState == PlaybackState.Playing)
            {
                wav.PlaybackStopped += Replay;
                wav.Stop();
            }
            else
            {
                reader.Position = 0;
                wav.Play();
            }
        }

        private void Replay(object sender, EventArgs e)
        {
            if (!Loaded) return;
            wav.PlaybackStopped -= Replay;
            Play();
        }

        public void Dispose()
        {
            reader.Dispose();
            source.Dispose();
            wav.Dispose();
        }
    }
}
