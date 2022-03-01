using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using NAudio.Wave;
//using NAudio.Wave.SampleProviders;
//using CSCore.Codecs.WAV;
//using CSCore.SoundOut;

namespace V7
{

    public class SoundEffect : IDisposable
    {
        public string Name;
        public bool Loaded { get; private set; }

        SFML.Audio.Sound snd;

        public SoundEffect(string path)
        {
            SFML.Audio.SoundBuffer sound = new SFML.Audio.SoundBuffer(path);
            snd = new SFML.Audio.Sound(sound);
            Loaded = true;
            string fName = path.Split('/').Last();
            fName = fName.Substring(0, fName.Length - 4);
            Name = fName;
        }

        public void Play()
        {
            snd.Play();
        }

        public void Dispose()
        {
            snd.Dispose();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
