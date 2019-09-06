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
        WaveFileReader reader;
        WaveOut wav;
        public string Name;

        public SoundEffect(string path)
        {
            string fName = path.Split('/').Last();
            fName = fName.Substring(0, fName.Length - 4);
            Name = fName;
            source = File.Open(path, FileMode.Open);
            wav = new WaveOut();
            reader = new WaveFileReader(source);
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
