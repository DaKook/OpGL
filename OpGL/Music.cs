using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class Music : IDisposable
    {
        public bool IsNull { get; private set; } = false;
        AccurateVorbisWaveReader vr;
        WaveOut wo;
        VorbisLoop vl;
        VolumeSampleProvider vp;
        public string Name;
        public int Volume = 100;
        private float vol = 100;
        private float previousVolume = -1;
        private float fadeSpeed;
        public bool isFaded => (vol == 0) || (vol == Volume);
        public int LoopStart { get => vl.LoopStart; set => vl.LoopStart = value; }
        public bool EnableLooping { get => vl.EnableLooping; set => vl.EnableLooping = value; }
        public bool IsPlaying { get; private set; }
        public bool Loaded { get; private set; }
        public Music(string path)
        {
            Loaded = false;
            string fName = path.Split('/').Last();
            fName = fName.Substring(0, fName.Length - 4);
            Name = fName;
            if (!path.EndsWith(".ogg"))
                throw new InvalidOperationException("Music can only be .ogg files!");
            vr = new AccurateVorbisWaveReader(path);
            wo = new WaveOut();
            vl = new VorbisLoop(vr);
            vp = new VolumeSampleProvider(vl);
            string[] s = vr.Comments.Where((st) => st.StartsWith("LOOP")).ToArray();
            for (int i = 0; i < s.Length; i++)
            {
                int.TryParse(s[i].Split('=').Last(), out int v);
                if (s[i].StartsWith("LOOPSTART"))
                {
                    vl.LoopStart = v;
                    vl.EnableLooping = true;
                }
            }
        }

        public void Initialize()
        {
            if (Loaded) return;
            wo.Init(vp);
            Loaded = true;
        }

        public void Dispose()
        {
            vr?.Dispose();
            wo?.Dispose();
            vl?.Dispose();
            IsNull = true;
        }

        private Music() { }
        public static Music Empty
        {
            get
            {
                Music ret = new Music();
                ret.IsNull = true;
                ret.Name = "Silence";
                return ret;
            }
        }

        public void FadeOut(float speed = 1)
        {
            fadeSpeed = -speed;
        }
        public void FadeIn(float speed = 1)
        {
            fadeSpeed = speed;
            if (!IsPlaying && !IsNull) Resume();
        }

        public void Process()
        {
            if (!Loaded) return;
            if (fadeSpeed < 0)
            {
                if (IsNull)
                {
                    vol = 0;
                    fadeSpeed = 0;
                }
                if (vol > 0)
                {
                    vol += fadeSpeed * (Volume / 100f);
                    if (vol <= 0)
                    {
                        vol = 0;
                        if (!IsNull)
                            Pause();
                        fadeSpeed = 0;
                    }
                }
            }
            else if (fadeSpeed > 0)
            {
                if (vol < Volume)
                {
                    vol += fadeSpeed * (Volume / 100f);
                    if (vol >= Volume)
                    {
                        vol = Volume;
                        fadeSpeed = 0;
                    }
                }
            }
            if (vol != previousVolume)
            {
                if (!IsNull)
                    vp.Volume = (vol / 100);
                previousVolume = vol;
            }
        }

        public void Play()
        {
            IsPlaying = true;
            if (!Loaded) return;
            if (!IsNull)
            vl.Position = 0;
            vol = Volume;
            fadeSpeed = 0;
            if (!IsNull)
                wo.Play();
        }

        public void Stop()
        {
            IsPlaying = false;
            if (!Loaded) return;
            if (!IsNull)
                wo.Stop();
        }

        public void Resume()
        {
            IsPlaying = true;
            if (!Loaded) return;
            if (!IsNull)
                wo.Play();
        }

        public void Pause()
        {
            IsPlaying = false;
            if (!Loaded) return;
            if (!IsNull)
                wo.Stop();
        }

        public void Silence()
        {
            vol = 0;
            if (!IsNull)
                vp.Volume = (vol / 100);
        }

        public void UnSilence()
        {
            vol = Volume;
            if (!IsNull)
                vp.Volume = (vol / 100);
        }
    }

    class VorbisLoop : WaveStream, ISampleProvider
    {
        AccurateVorbisWaveReader source;
        public int LoopStart;
        public bool EnableLooping;

        public VorbisLoop(AccurateVorbisWaveReader source)
        {
            this.source = source;
        }

        public override WaveFormat WaveFormat => source.WaveFormat;

        public override long Length => source.Length;

        public override long Position { get => source.Position; set => source.Position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int read = count - totalBytesRead;
                int bytesRead = source.Read(buffer, offset + totalBytesRead, read);
                if (bytesRead == 0)
                {
                    if (source.Position == 0 || !EnableLooping)
                    {
                        // something wrong with the source stream
                        break;
                    }
                    // loop
                    source.Position = LoopStart * 8;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int totalSamplesRead = 0;

            while (totalSamplesRead < count)
            {
                int read = count - totalSamplesRead;
                int bytesRead = source.Read(buffer, offset + totalSamplesRead, read);
                if (bytesRead == 0)
                {
                    if (source.Position == 0 || !EnableLooping)
                    {
                        // something wrong with the source stream
                        break;
                    }
                    // loop
                    source.Position = LoopStart * 8;
                }
                totalSamplesRead += bytesRead;
            }
            return totalSamplesRead;
        }
    }

    public class AccurateVorbisWaveReader : WaveStream, IDisposable, ISampleProvider, IWaveProvider
    {
        NVorbis.VorbisReader _reader;
        NAudio.Wave.WaveFormat _waveFormat;

        public AccurateVorbisWaveReader(string fileName)
        {
            _reader = new NVorbis.VorbisReader(fileName);

            _waveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(_reader.SampleRate, _reader.Channels);
        }

        public AccurateVorbisWaveReader(System.IO.Stream sourceStream)
        {
            _reader = new NVorbis.VorbisReader(sourceStream, false);

            _waveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(_reader.SampleRate, _reader.Channels);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }

            base.Dispose(disposing);
        }

        public override NAudio.Wave.WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }

        public override long Length
        {
            get { return _reader.TotalSamples * _waveFormat.BlockAlign; }
        }

        public override long Position
        {
            get
            {
                return _reader.DecodedPosition * _waveFormat.BlockAlign;
            }
            set
            {
                if (value < 0 || value > Length) throw new ArgumentOutOfRangeException("value");

                _reader.DecodedPosition = value / _waveFormat.BlockAlign;
            }
        }

        // This buffer can be static because it can only be used by 1 instance per thread
        [ThreadStatic]
        static float[] _conversionBuffer = null;

        public override int Read(byte[] buffer, int offset, int count)
        {
            // adjust count so it is in floats instead of bytes
            count /= sizeof(float);

            // make sure we don't have an odd count
            count -= count % _reader.Channels;

            // get the buffer, creating a new one if none exists or the existing one is too small
            var cb = _conversionBuffer ?? (_conversionBuffer = new float[count]);
            if (cb.Length < count)
            {
                cb = (_conversionBuffer = new float[count]);
            }

            // let Read(float[], int, int) do the actual reading; adjust count back to bytes
            int cnt = Read(cb, 0, count) * sizeof(float);

            // move the data back to the request buffer
            Buffer.BlockCopy(cb, 0, buffer, offset, cnt);

            // done!
            return cnt;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _reader.ReadSamples(buffer, offset, count);
        }

        public bool IsParameterChange { get { return _reader.IsParameterChange; } }

        public void ClearParameterChange()
        {
            _reader.ClearParameterChange();
        }

        public int StreamCount
        {
            get { return _reader.StreamCount; }
        }

        public int? NextStreamIndex { get; set; }

        public bool GetNextStreamIndex()
        {
            if (!NextStreamIndex.HasValue)
            {
                var idx = _reader.StreamCount;
                if (_reader.FindNextStream())
                {
                    NextStreamIndex = idx;
                    return true;
                }
            }
            return false;
        }

        public int CurrentStream
        {
            get { return _reader.StreamIndex; }
            set
            {
                if (!_reader.SwitchStreams(value))
                {
                    throw new System.IO.InvalidDataException("The selected stream is not a valid Vorbis stream!");
                }

                if (NextStreamIndex.HasValue && value == NextStreamIndex.Value)
                {
                    NextStreamIndex = null;
                }
            }
        }

        /// <summary>
        /// Gets the encoder's upper bitrate of the current selected Vorbis stream
        /// </summary>
        public int UpperBitrate { get { return _reader.UpperBitrate; } }

        /// <summary>
        /// Gets the encoder's nominal bitrate of the current selected Vorbis stream
        /// </summary>
        public int NominalBitrate { get { return _reader.NominalBitrate; } }

        /// <summary>
        /// Gets the encoder's lower bitrate of the current selected Vorbis stream
        /// </summary>
        public int LowerBitrate { get { return _reader.LowerBitrate; } }

        /// <summary>
        /// Gets the encoder's vendor string for the current selected Vorbis stream
        /// </summary>
        public string Vendor { get { return _reader.Vendor; } }

        /// <summary>
        /// Gets the comments in the current selected Vorbis stream
        /// </summary>
        public string[] Comments { get { return _reader.Comments; } }

        /// <summary>
        /// Gets the number of bits read that are related to framing and transport alone
        /// </summary>
        public long ContainerOverheadBits { get { return _reader.ContainerOverheadBits; } }

        /// <summary>
        /// Gets stats from each decoder stream available
        /// </summary>
        public NVorbis.IVorbisStreamStatus[] Stats { get { return _reader.Stats; } }
    }
}
