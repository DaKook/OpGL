﻿using System;
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
        //Stream source;
        //WaveFileReader reader;
        //DirectSoundOut wav;
        public string Name;
        public bool Loaded { get; private set; }

        SFML.Audio.Sound snd;

        public SoundEffect(string path)
        {
            SFML.Audio.SoundBuffer sound = new SFML.Audio.SoundBuffer(path);
            snd = new SFML.Audio.Sound(sound);
            Loaded = true;
            //Loaded = false;
            string fName = path.Split('/').Last();
            fName = fName.Substring(0, fName.Length - 4);
            Name = fName;
            //source = File.Open(path, FileMode.Open);
            //if (path.EndsWith(".wav"))
            //{
            //    reader = new WaveFileReader(source);
            //}
            //else
            //    throw new InvalidOperationException("Sound effects can only be .wav files!");
        }

        public void Initialize()
        {
            //if (Loaded) return;
            //wav = new DirectSoundOut();
            //WaveChannel32 wc = new WaveChannel32(reader);
            //wav.Init(wc);
            //Loaded = true;
        }

        public void ReInit()
        {
            //if (!Loaded) return;
            //wav.Dispose();
            //wav = new DirectSoundOut();
            //wav.Init(reader.ToSampleProvider());
        }

        public void Play()
        {
            //if (!Loaded) return;
            //if (wav.PlaybackState == PlaybackState.Playing)
            //{
            //    wav.PlaybackStopped += Replay;
            //    wav.Stop();
            //}
            //else
            //{
            //    reader.Position = 0;
            //    //wav.PlaybackStopped += (sender, e) => { throw e.Exception; };
            //    wav.Play();
            //}
            snd.Play();
        }

        //private void Replay(object sender, EventArgs e)
        //{
        //    if (!Loaded) return;
        //    wav.PlaybackStopped -= Replay;
        //    Play();
        //}

        public void Dispose()
        {
            snd.Dispose();
        }
    }
}
