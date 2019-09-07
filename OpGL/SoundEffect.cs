using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace OpGL
{
    public class SoundEffect
    {
        Stream source;
        WaveStream reader;
        WaveOut wav;
        public string Name;

        public SoundEffect(string path)
        {
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
            wav.Init(reader);
        }

        public void Play()
        {
            reader.Seek(0, SeekOrigin.Begin);
            wav.Play();
        }

        public void Dispose()
        {
            reader.Dispose();
            source.Dispose();
            wav.Dispose();
        }
    }
}
