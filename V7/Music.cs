//using NAudio.Wave;
//using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Audio;
using OpenTK.Audio.OpenAL;
using NVorbis.Ogg;

using System.Reflection;
using OpenTK;

namespace V7
{
    public class Music : IDisposable
    {
        //public bool IsNull { get; private set; } = false;
        //AccurateVorbisWaveReader vr;
        //WaveOut wo;
        //VorbisLoop vl;
        //VolumeSampleProvider vp;
        //public string Name;
        //public int Volume = 100;
        //private float vol = 100;
        //private float previousVolume = -1;
        //private float fadeSpeed;
        //public bool isFaded => (vol == 0) || (vol == Volume);
        //public int LoopStart { get => vl.LoopStart; set => vl.LoopStart = value; }
        //public bool EnableLooping { get => vl.EnableLooping; set => vl.EnableLooping = value; }
        //public bool IsPlaying { get; private set; }
        //public bool Loaded { get; private set; }
        //public Music(string path)
        //{
        //    Loaded = false;
        //    string fName = path.Split('/').Last();
        //    fName = fName.Substring(0, fName.Length - 4);
        //    Name = fName;
        //    if (!path.EndsWith(".ogg"))
        //        throw new InvalidOperationException("Music can only be .ogg files!");
        //    vr = new AccurateVorbisWaveReader(path);
        //    wo = new WaveOut();
        //    vl = new VorbisLoop(vr);
        //    vp = new VolumeSampleProvider(vl);
        //    string[] s = vr.Comments.Where((st) => st.StartsWith("LOOP")).ToArray();
        //    for (int i = 0; i < s.Length; i++)
        //    {
        //        int.TryParse(s[i].Split('=').Last(), out int v);
        //        if (s[i].StartsWith("LOOPSTART"))
        //        {
        //            vl.LoopStart = v;
        //            vl.EnableLooping = true;
        //        }
        //    }
        //}

        //public void Initialize()
        //{
        //    if (Loaded) return;
        //    wo.Init(vp);
        //    Loaded = true;
        //}

        //public void Dispose()
        //{
        //    vr?.Dispose();
        //    wo?.Dispose();
        //    vl?.Dispose();
        //    IsNull = true;
        //}

        //private Music() { }
        //public static Music Empty
        //{
        //    get
        //    {
        //        Music ret = new Music();
        //        ret.IsNull = true;
        //        ret.Name = "Silence";
        //        return ret;
        //    }
        //}

        //public void FadeOut(float speed = 1)
        //{
        //    fadeSpeed = -speed;
        //}
        //public void FadeIn(float speed = 1)
        //{
        //    fadeSpeed = speed;
        //    if (!IsPlaying && !IsNull) Resume();
        //}

        //public void Process()
        //{
        //    if (!Loaded) return;
        //    if (fadeSpeed < 0)
        //    {
        //        if (IsNull)
        //        {
        //            vol = 0;
        //            fadeSpeed = 0;
        //        }
        //        if (vol > 0)
        //        {
        //            vol += fadeSpeed * (Volume / 100f);
        //            if (vol <= 0)
        //            {
        //                vol = 0;
        //                if (!IsNull)
        //                    Pause();
        //                fadeSpeed = 0;
        //            }
        //        }
        //    }
        //    else if (fadeSpeed > 0)
        //    {
        //        if (vol < Volume)
        //        {
        //            vol += fadeSpeed * (Volume / 100f);
        //            if (vol >= Volume)
        //            {
        //                vol = Volume;
        //                fadeSpeed = 0;
        //            }
        //        }
        //    }
        //    if (vol != previousVolume)
        //    {
        //        if (!IsNull)
        //            vp.Volume = (vol / 100);
        //        previousVolume = vol;
        //    }
        //}

        //public void Play()
        //{
        //    IsPlaying = true;
        //    if (!Loaded) return;
        //    if (!IsNull)
        //    vl.Position = 0;
        //    vol = Volume;
        //    fadeSpeed = 0;
        //    if (!IsNull)
        //        wo.Play();
        //}

        //public void Stop()
        //{
        //    IsPlaying = false;
        //    if (!Loaded) return;
        //    if (!IsNull)
        //        wo.Stop();
        //}

        //public void Resume()
        //{
        //    IsPlaying = true;
        //    if (!Loaded) return;
        //    if (!IsNull)
        //        wo.Play();
        //}

        //public void Pause()
        //{
        //    IsPlaying = false;
        //    if (!Loaded) return;
        //    if (!IsNull)
        //        wo.Stop();
        //}

        //public void Silence()
        //{
        //    vol = 0;
        //    if (!IsNull)
        //        vp.Volume = (vol / 100);
        //}

        //public void UnSilence()
        //{
        //    vol = Volume;
        //    if (!IsNull)
        //        vp.Volume = (vol / 100);
        //}
        public string Name;
        public bool IsNull;
        public bool IsFaded => fadeSpeed == 0;
        public float Volume = 80;
        float vol = 100;
        float fadeSpeed = 0;
        public NVorbis.VorbisReader Reader;
        public bool IsPlaying;
        public float CurrentVolume { get; private set; }
        public int LoopStart;
        public bool IsOriginal = true;

        public static void Initialize() => MusicBase.Initialize();

        static class MusicBase
        {
            public static bool Initialized = false;
            public static Music CurrentSong;
            static int sourceId;
            static bool kill;

            static NVorbis.VorbisReader reader;
            public static void Initialize()
            {
                //IntPtr device = Alc.OpenDevice(null);
                //ContextHandle context = Alc.CreateContext(device, new int[] { });
                //Alc.MakeContextCurrent(context);
                Initialized = true;
                sourceId = AL.GenSource();
                System.Threading.Thread thread = new System.Threading.Thread(() =>
                {
                    System.Diagnostics.Stopwatch stp = new System.Diagnostics.Stopwatch();
                    stp.Start();
                    int[] buffers = new int[2];
                    int currentBuffer = 1;
                    while (!kill)
                    {
                        if (CurrentSong.IsPlaying && CurrentSong.Reader is object)
                        {
                            reader = CurrentSong.Reader;
                            stp.Restart();
                            Play();
                            AL.GetSource(sourceId, ALGetSourcei.BuffersQueued, out int q);
                            AL.GetSource(sourceId, ALGetSourcei.BuffersProcessed, out int c);
                            q -= c;
                            CheckError();
                            while (q < 2)
                            {
                                currentBuffer = 1 - currentBuffer;
                                if (c > 0)
                                {
                                    //AL.BufferData(buffers[currentBuffer], ALFormat.Mono16, new byte[] { }, 0, 44100);
                                    AL.SourceUnqueueBuffer(sourceId);
                                    CheckError();
                                    AL.DeleteBuffer(buffers[currentBuffer]);
                                }
                                CheckError();
                                buffers[currentBuffer] = AL.GenBuffer();
                                float[] fBuffer = new float[4410];
                                int samples = reader.ReadSamples(fBuffer, 0, fBuffer.Length);
                                while (samples < fBuffer.Length)
                                {
                                    reader.SamplePosition = CurrentSong.LoopStart;
                                    int newSamples = reader.ReadSamples(fBuffer, samples, fBuffer.Length - samples);
                                    samples += newSamples;
                                    reader.SamplePosition += newSamples / reader.Channels;
                                }
                                for (int i = 0; i < fBuffer.Length; i++)
                                {
                                    fBuffer[i] *= (CurrentSong.CurrentVolume / 100);
                                }
                                if (!CurrentSong.IsPlaying)
                                    break;
                                AL.BufferData(buffers[currentBuffer], ALFormat.StereoFloat32Ext, fBuffer, reader.SampleRate);
                                CheckError();
                                AL.SourceQueueBuffer(sourceId, buffers[currentBuffer]);
                                CheckError();
                                q++;
                            }
                            while (stp.ElapsedTicks < System.Diagnostics.Stopwatch.Frequency / 500)
                            {
                                if (stp.ElapsedTicks < System.Diagnostics.Stopwatch.Frequency / 1000)
                                    System.Threading.Thread.Sleep(1);
                            }
                        }
                        else
                        {
                            AL.SourceUnqueueBuffer(sourceId);
                            System.Threading.Thread.Sleep(1);
                        }
                    }
                    ALC.CloseDevice(ALC.GetContextsDevice(ALC.GetCurrentContext()));
                });
                thread.Start();
            }

            static void CheckError()
            {
                ALError err = AL.GetError();
#if DEBUG
                //if (err != ALError.NoError)
                //    throw new Exception("AL Error: " + err.ToString());
#endif
            }

            public static void Stop()
            {
                kill = true;
            }

            public static void Pause()
            {
                AL.SourcePause(sourceId);
                AL.Source(sourceId, ALSourcei.SampleOffset, 4409);
                CheckError();
            }

            public static void Play()
            {
                if (AL.GetSourceState(sourceId) != ALSourceState.Playing)
                    AL.SourcePlay(sourceId);
            }
        }

        public static void Close()
        {
            MusicBase.Stop();
        }

        public Music(string fileName)
        {
            string fName = fileName.Split('/', '\\').Last();
            fName = fName.Substring(0, fName.Length - 4);
            Name = fName;
            Reader = new NVorbis.VorbisReader(fileName);
            string loop = Reader.Tags.GetTagSingle("LOOPSTART");
            if (loop is object)
            {
                int.TryParse(loop, out LoopStart);
            }
        }

        public void Update(string fileName)
        {
            bool play;
            if (play = IsPlaying)
                Pause();
            Reader.Dispose();
            Reader = new NVorbis.VorbisReader(fileName);
            if (play)
                Play();
        }

        private Music()
        {
            IsNull = true;
        }

        public void Dispose()
        {
            Reader.Dispose();
        }

        public void Rewind()
        {
            if (Reader is object && Reader.SamplePosition != 0)
                Reader.SamplePosition = 0;
            fadeSpeed = 0;
        }

        public void Play()
        {
            if (!MusicBase.Initialized) MusicBase.Initialize();
            if (MusicBase.CurrentSong is object)
                MusicBase.CurrentSong.IsPlaying = false;
            if (fadeSpeed == 0)
            {
                vol = 100;
                CurrentVolume = vol * Volume / 100;
            }
            MusicBase.CurrentSong = this;
            MusicBase.Play();
            IsPlaying = true;

        }

        public void Pause()
        {
            //music.Pause();
            IsPlaying = false;
            MusicBase.Pause();
        }

        public void Stop()
        {
            //music.Stop();
            IsPlaying = false;
            MusicBase.Pause();
            if (Reader is object)
                Reader.SamplePosition = 0;
        }

        public void Process()
        {
            CurrentVolume = vol * (Volume / 100);
            if (fadeSpeed != 0)
            {
                vol += fadeSpeed;
                if (vol >= 100)
                {
                    vol = 100;
                    fadeSpeed = 0;
                }
                else if (vol <= 0)
                {
                    vol = 0;
                    fadeSpeed = 0;
                    Pause();
                }
            }
        }

        public void FadeIn(float speed = 1)
        {
            if (IsNull) return;
            fadeSpeed = speed;
            if (!IsPlaying)
            {
                vol = 0;
                Play();
            }
        }

        public void FadeOut(float speed = 1)
        {
            if (IsNull) return;
            fadeSpeed = -1;
        }

        public void Silence()
        {
            vol = 0;
        }

        public static Music Empty => new Music();
    }

    //class VorbisLoop : WaveStream, ISampleProvider
    //{
    //    AccurateVorbisWaveReader source;
    //    public int LoopStart;
    //    public bool EnableLooping;

    //    public VorbisLoop(AccurateVorbisWaveReader source)
    //    {
    //        this.source = source;
    //    }

    //    public override WaveFormat WaveFormat => source.WaveFormat;

    //    public override long Length => source.Length;

    //    public override long Position { get => source.Position; set => source.Position = value; }

    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        int totalBytesRead = 0;

    //        while (totalBytesRead < count)
    //        {
    //            int read = count - totalBytesRead;
    //            int bytesRead = source.Read(buffer, offset + totalBytesRead, read);
    //            if (bytesRead == 0)
    //            {
    //                if (source.Position == 0 || !EnableLooping)
    //                {
    //                    // something wrong with the source stream
    //                    break;
    //                }
    //                // loop
    //                source.Position = LoopStart * 8;
    //            }
    //            totalBytesRead += bytesRead;
    //        }
    //        return totalBytesRead;
    //    }

    //    public int Read(float[] buffer, int offset, int count)
    //    {
    //        int totalSamplesRead = 0;

    //        while (totalSamplesRead < count)
    //        {
    //            int read = count - totalSamplesRead;
    //            int bytesRead = source.Read(buffer, offset + totalSamplesRead, read);
    //            if (bytesRead == 0)
    //            {
    //                if (source.Position == 0 || !EnableLooping)
    //                {
    //                    // something wrong with the source stream
    //                    break;
    //                }
    //                // loop
    //                source.Position = LoopStart * 8;
    //            }
    //            totalSamplesRead += bytesRead;
    //        }
    //        return totalSamplesRead;
    //    }
    //}

    //public class AccurateVorbisWaveReader : WaveStream, IDisposable, ISampleProvider, IWaveProvider
    //{
    //    NVorbis.VorbisReader _reader;
    //    NAudio.Wave.WaveFormat _waveFormat;

    //    public AccurateVorbisWaveReader(string fileName)
    //    {
    //        _reader = new NVorbis.VorbisReader(fileName);

    //        _waveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(_reader.SampleRate, _reader.Channels);
    //    }

    //    public AccurateVorbisWaveReader(System.IO.Stream sourceStream)
    //    {
    //        _reader = new NVorbis.VorbisReader(sourceStream, false);

    //        _waveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(_reader.SampleRate, _reader.Channels);
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        if (disposing && _reader != null)
    //        {
    //            _reader.Dispose();
    //            _reader = null;
    //        }

    //        base.Dispose(disposing);
    //    }

    //    public override NAudio.Wave.WaveFormat WaveFormat
    //    {
    //        get { return _waveFormat; }
    //    }

    //    public override long Length
    //    {
    //        get { return _reader.TotalSamples * _waveFormat.BlockAlign; }
    //    }

    //    public override long Position
    //    {
    //        get
    //        {
    //            return _reader.DecodedPosition * _waveFormat.BlockAlign;
    //        }
    //        set
    //        {
    //            if (value < 0 || value > Length) throw new ArgumentOutOfRangeException("value");

    //            _reader.DecodedPosition = value / _waveFormat.BlockAlign;
    //        }
    //    }

    //    // This buffer can be static because it can only be used by 1 instance per thread
    //    [ThreadStatic]
    //    static float[] _conversionBuffer = null;

    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        // adjust count so it is in floats instead of bytes
    //        count /= sizeof(float);

    //        // make sure we don't have an odd count
    //        count -= count % _reader.Channels;

    //        // get the buffer, creating a new one if none exists or the existing one is too small
    //        var cb = _conversionBuffer ?? (_conversionBuffer = new float[count]);
    //        if (cb.Length < count)
    //        {
    //            cb = (_conversionBuffer = new float[count]);
    //        }

    //        // let Read(float[], int, int) do the actual reading; adjust count back to bytes
    //        int cnt = Read(cb, 0, count) * sizeof(float);

    //        // move the data back to the request buffer
    //        Buffer.BlockCopy(cb, 0, buffer, offset, cnt);

    //        // done!
    //        return cnt;
    //    }

    //    public int Read(float[] buffer, int offset, int count)
    //    {
    //        return _reader.ReadSamples(buffer, offset, count);
    //    }

    //    public bool IsParameterChange { get { return _reader.IsParameterChange; } }

    //    public void ClearParameterChange()
    //    {
    //        _reader.ClearParameterChange();
    //    }

    //    public int StreamCount
    //    {
    //        get { return _reader.StreamCount; }
    //    }

    //    public int? NextStreamIndex { get; set; }

    //    public bool GetNextStreamIndex()
    //    {
    //        if (!NextStreamIndex.HasValue)
    //        {
    //            var idx = _reader.StreamCount;
    //            if (_reader.FindNextStream())
    //            {
    //                NextStreamIndex = idx;
    //                return true;
    //            }
    //        }
    //        return false;
    //    }

    //    public int CurrentStream
    //    {
    //        get { return _reader.StreamIndex; }
    //        set
    //        {
    //            if (!_reader.SwitchStreams(value))
    //            {
    //                throw new System.IO.InvalidDataException("The selected stream is not a valid Vorbis stream!");
    //            }

    //            if (NextStreamIndex.HasValue && value == NextStreamIndex.Value)
    //            {
    //                NextStreamIndex = null;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Gets the encoder's upper bitrate of the current selected Vorbis stream
    //    /// </summary>
    //    public int UpperBitrate { get { return _reader.UpperBitrate; } }

    //    /// <summary>
    //    /// Gets the encoder's nominal bitrate of the current selected Vorbis stream
    //    /// </summary>
    //    public int NominalBitrate { get { return _reader.NominalBitrate; } }

    //    /// <summary>
    //    /// Gets the encoder's lower bitrate of the current selected Vorbis stream
    //    /// </summary>
    //    public int LowerBitrate { get { return _reader.LowerBitrate; } }

    //    /// <summary>
    //    /// Gets the encoder's vendor string for the current selected Vorbis stream
    //    /// </summary>
    //    public string Vendor { get { return _reader.Vendor; } }

    //    /// <summary>
    //    /// Gets the comments in the current selected Vorbis stream
    //    /// </summary>
    //    public string[] Comments { get { return _reader.Comments; } }

    //    /// <summary>
    //    /// Gets the number of bits read that are related to framing and transport alone
    //    /// </summary>
    //    public long ContainerOverheadBits { get { return _reader.ContainerOverheadBits; } }

    //    /// <summary>
    //    /// Gets stats from each decoder stream available
    //    /// </summary>
    //    public NVorbis.IVorbisStreamStatus[] Stats { get { return _reader.Stats; } }
    //}
}
