using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Music
    {
        AccurateVorbisWaveReader vr;
        WaveOut wo;
        VorbisLoop vl;
        public string Name;
        public int LoopStart { get => vl.LoopStart; set => vl.LoopStart = value; }
        public bool EnableLooping { get => vl.EnableLooping; set => vl.EnableLooping = value; }
        public Music(string path)
        {
            string fName = path.Split('/').Last();
            fName = fName.Substring(0, fName.Length - 4);
            Name = fName;
            if (!path.EndsWith(".ogg"))
                throw new InvalidOperationException("Music can only be .ogg files!");
            vr = new AccurateVorbisWaveReader(path);
            wo = new WaveOut();
            vl = new VorbisLoop(vr);
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
            wo.Init(vl);
        }

        public void Play()
        {
            vl.Position = 0;
            wo.Play();
        }

        public void Stop()
        {
            wo.Stop();
        }
    }

    class VorbisLoop : WaveStream
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
    }

    public class AccurateVorbisWaveReader : WaveStream, IDisposable, ISampleProvider, IWaveProvider
    {
        NVorbis.VorbisReader _reader;
        WaveFormat _waveFormat;

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
