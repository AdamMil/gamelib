using System;
using System.IO;
using System.Collections;
using GameLib.IO;
using GameLib.Interop.SndFile;
using GameLib.Interop.SDL;
using GameLib.Interop.GLMixer;

namespace GameLib.Audio
{
// TODO: check return values for calls into GLMixer

[Flags]
public enum SampleFormat : ushort
{ Eight=GLMixer.Format.Eight, Sixteen=GLMixer.Format.Sixteen, BitsPart=GLMixer.Format.BitsPart,
  Signed=GLMixer.Format.Signed, BigEndian=GLMixer.Format.BigEndian,

  U8=Eight, U16=Sixteen, S8=Eight|Signed, S16=Sixteen|Signed,
  U8BE=U8|BigEndian, U16BE=U16|BigEndian, S8BE=S8|BigEndian, S16BE=S16|BigEndian,
  U8Sys=GLMixer.Format.U8Sys, U16Sys=GLMixer.Format.U16Sys, S8Sys=GLMixer.Format.S8Sys, S16Sys=GLMixer.Format.S16Sys,
  
  Default=S16Sys, MixerFormat=GLMixer.Format.MixerFormat
}

public enum Speakers : byte { Mono=1, Stereo=2 }
public enum AudioStatus { Stopped, Playing, Paused }
public enum Fade { None, In, Out }
public enum PlayPolicy { Fail, Oldest, Priority, OldestPriority }
public enum MixPolicy  { DontDivide, Divide }

public struct AudioFormat
{ public AudioFormat(uint frequency, SampleFormat format, byte channels)
  { Frequency=frequency; Format=format; Channels=channels;
  }
  public byte SampleSize  { get { return (byte)((int)(Format&SampleFormat.BitsPart)>>3); } }
  public byte FrameSize   { get { return (byte)(SampleSize*Channels); } }
  public uint ByteRate    { get { return (uint)(FrameSize*Frequency); } }

  public uint         Frequency;
  public SampleFormat Format;
  public byte         Channels;
}

public unsafe delegate void MixFilter(int* buffer, int frames, AudioFormat format);
public delegate void ChannelFinishedHandler(Channel channel);

#region AudioSource
public abstract class AudioSource
{ public abstract bool CanRewind { get; }
  public abstract bool CanSeek   { get; }
  public abstract int  Position  { get; set; }
  public AudioFormat Format { get { return format; } }
  public int Length  { get { return length; } }
  public int Priority { get { return priority; } set { priority=value; } }
  
  public int Volume
  { get { return volume; }
    set { Mixer.CheckVolume(value); volume=value; }
  }
  public float PlaybackRate { get { return rate; } set { Mixer.CheckRate(rate); lock(this) rate=value; } }

  public abstract void Rewind();

  public int Play() { return Play(0, Mixer.Infinite, Mixer.FreeChannel); }
  public int Play(int loops) { return Play(loops, Mixer.Infinite, Mixer.FreeChannel); }
  public int Play(int loops, int timeoutMs) { return Play(loops, timeoutMs, Mixer.FreeChannel); }
  public int Play(int loops, int timeoutMs, int channel)
  { if(channel<0) return Mixer.StartPlaying(channel, this, loops, Fade.None, 0, timeoutMs);
    else
      lock(Mixer.Channels[channel])
      { Mixer.Channels[channel].StartPlaying(this, loops, Fade.None, 0, timeoutMs);
        return channel;
      }
  }

  public int FadeIn(uint fadeMs) { return FadeIn(fadeMs, 0, Mixer.FreeChannel, Mixer.Infinite); }
  public int FadeIn(uint fadeMs, int loops) { return FadeIn(fadeMs, loops, Mixer.FreeChannel, Mixer.Infinite); }
  public int FadeIn(uint fadeMs, int loops, int timeoutMs) { return FadeIn(fadeMs, loops, timeoutMs, Mixer.FreeChannel); }
  public int FadeIn(uint fadeMs, int loops, int timeoutMs, int channel)
  { if(channel<0) return Mixer.StartPlaying(channel, this, loops, Fade.In, fadeMs, timeoutMs);
    else
      lock(Mixer.Channels[channel])
      { Mixer.Channels[channel].StartPlaying(this, loops, Fade.In, fadeMs, timeoutMs);
        return channel;
      }
  }

  public abstract byte[] ReadAll();
  public abstract int ReadBytes(byte[] buf, int length);
  public unsafe int ReadFrames(int* buffer, int frames) { return ReadFrames(buffer, frames, -1); }
  public abstract unsafe int ReadFrames(int* buffer, int frames, int volume);
  
  protected AudioFormat format;
  protected float rate=1f;
  protected int   volume=Mixer.MaxVolume, length=-1, priority;
  internal  int   playing;
}

public abstract class StreamSource : AudioSource
{ public override bool CanRewind { get { return stream.CanSeek; } }
  public override bool CanSeek   { get { return stream.CanSeek; } }
  public override int  Position
  { get { return curPos; }
    set { if(value!=curPos) lock(this) curPos=(int)(stream.Position=value+startPos); }
  }

  public override void Rewind() { Position=0; }

  public override byte[] ReadAll()
  { lock(this)
    { if(Position>0) Rewind();
      if(Length>=0)
      { byte[] buf = new byte[Length];
        if(ReadBytes(buf, Length)!=Length) throw new EndOfStreamException();
        return buf;
      }
      else throw new NotImplementedException("no implementation for ReadAll() on streams with an unknown length"); // TODO: implement me (and others)
    }
  }

  public override int ReadBytes(byte[] buf, int length)
  { lock(this)
    { int read = stream.Read(buf, 0, length);
      if(read>=0) curPos += read;
      return read;
    }
  }

  public override unsafe int ReadFrames(int *dest, int frames, int volume)
  { lock(this)
    { int shift=format.FrameSize>>1, toRead=Math.Min(length-curPos, frames<<shift), read, readSamps;
      if(buffer==null || toRead>buffer.Length) buffer = new byte[toRead];
      read = stream.Read(buffer, 0, toRead); // FIXME: assumes stream is reliable
      readSamps = (read>>shift)*format.Channels;
      fixed(byte* src = buffer)
        if(format.Format==SampleFormat.MixerFormat)
          if(volume>=0) GLMixer.Mix(dest, (int*)src, (uint)readSamps, (ushort)volume);
          else GLMixer.Copy(dest, (int*)src, (uint)readSamps);
        else
          GLMixer.ConvertMix(dest, src, (uint)readSamps, (ushort)format.Format,
                             (ushort)(volume<0 ? Mixer.MaxVolume : volume));
      curPos += read;
      return read>>shift;
    }
  }
  
  protected void Init(Stream stream, AudioFormat format, int startPos, int length)
  { this.stream=stream;
    this.format=format;
    this.startPos=startPos;
    this.length=length;
    curPos = stream.CanSeek ? (int)stream.Position-startPos : 0;
  }

  protected byte[] buffer;
  protected Stream stream;
  protected int    curPos, startPos;
}

public class RawSource : StreamSource
{ public RawSource(string filename, AudioFormat format) : this(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read), format) { }
  public RawSource(string filename, AudioFormat format, int start, int length) : this(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read), format, start, length) { }
  public RawSource(Stream stream, AudioFormat format) : this(stream, format, 0, stream.CanSeek ? (int)stream.Length : -1) { }
  public RawSource(Stream stream, AudioFormat format, int start, int length)
  { Init(stream, format, start, length);
  }
}

public class SoundFileSource : AudioSource
{ public SoundFileSource(string filename)
  { SF.Info info = new SF.Info();
    info.format = SF.Format.CPUEndian;
    sndfile = SF.Open(filename, SF.OpenMode.Read, ref info);
    Init(ref info);
  }
  public SoundFileSource(string filename, AudioFormat format)
  { SF.Info info = new SF.Info();
    InitInfo(ref info, format);
    sndfile = SF.Open(filename, SF.OpenMode.Read, ref info);
    Init(ref info);
  }
  public SoundFileSource(Stream stream) : this(stream, true) { }
  public SoundFileSource(Stream stream, bool autoClose)
  { SF.Info info = new SF.Info();
    calls = new StreamIOCalls(stream, autoClose);
    unsafe
    { fixed(SF.IOCalls* io = &calls.calls)
        sndfile = SF.OpenCalls(io, new IntPtr(null), SF.OpenMode.Read, ref info, autoClose ? 1 : 0);
    }
    Init(ref info);
  }
  public SoundFileSource(Stream stream, AudioFormat format) : this(stream, format, true) { }
  public SoundFileSource(Stream stream, AudioFormat format, bool autoClose)
  { SF.Info info = new SF.Info();
    InitInfo(ref info, format);
    calls = new StreamIOCalls(stream, autoClose);
    unsafe
    { fixed(SF.IOCalls* io = &calls.calls)
        sndfile = SF.OpenCalls(io, new IntPtr(null), SF.OpenMode.Read, ref info, autoClose ? 1 : 0);
    }
    Init(ref info);
  }

  public override bool CanRewind { get { return true; } }
  public override bool CanSeek   { get { return true; } }
  public override int  Position
  { get { return curPos; }
    set
    { if(value==curPos) return;
      lock(this) curPos=(int)SF.Seek(sndfile, value, SF.SeekType.Absolute);
    }
  }

  public override void Rewind() { Position=0; }

  public override byte[] ReadAll()
  { lock(this)
    { if(Position>0) Rewind();
      if(Length>=0)
      { byte[] buf = new byte[Length*format.FrameSize];
        if(ReadBytes(buf, buf.Length)!=buf.Length) throw new EndOfStreamException();
        return buf;
      }
      else throw new NotImplementedException("no implementation for ReadAll() on streams with an unknown length"); // TODO: implement me (and others)
    }
  }

  public override int ReadBytes(byte[] buf, int length)
  { int frames = length/format.FrameSize;
    if(frames*format.FrameSize != length) throw new ArgumentException("length must be multiple of framesize");
    lock(this)
    { int read;
      unsafe { fixed(byte* pbuf = buf) read=(int)SF.ReadShorts(sndfile, (short*)pbuf, frames); }
      if(read>=0) curPos += read;
      return read*format.FrameSize;
    }
  }

  public override unsafe int ReadFrames(int *dest, int frames, int volume)
  { lock(this)
    { if(volume<0) return (int)SF.ReadInts(sndfile, dest, frames);
      else
      { int samples = frames*format.Channels;
        short* buf = stackalloc short[samples];
        int read = (int)SF.ReadShorts(sndfile, buf, frames);
        GLMixer.Check(GLMixer.ConvertMix(dest, buf, (uint)samples, (ushort)format.Format, (ushort)volume));
        curPos += read;
        return read;
      }
    }
  }

  void InitInfo(ref SF.Info info, AudioFormat format)
  { info.channels   = format.Channels;
    info.samplerate = (int)format.Frequency;
    info.format     = SF.Format.RAW;
    if((format.Format&SampleFormat.S16)==SampleFormat.S16) info.format |= SF.Format.PCM_S16;
    else if((format.Format&SampleFormat.U8)==SampleFormat.U8) info.format |= SF.Format.PCM_U8;
    else if((format.Format&SampleFormat.S8)==SampleFormat.S8) info.format |= SF.Format.PCM_S8;
    if((format.Format&SampleFormat.BigEndian)!=0) info.format |= SF.Format.BigEndian;
    else info.format |= SF.Format.LittleEndian;
  }

  void Init(ref SF.Info info)
  { unsafe { if(sndfile.ToPointer()==null) throw new FileNotFoundException(); }
    format.Channels  = (byte)info.channels;
    format.Frequency = (uint)info.samplerate;
    format.Format    = SampleFormat.S16;
    #if BIGENDIAN
    if(info.Has(SF.Format.CPUEndian) || info.Has(SF.Format.BigEndian))
    #else
    if(!info.Has(SF.Format.CPUEndian) && info.Has(SF.Format.BigEndian))
    #endif
    { format.Format |= SampleFormat.BigEndian;
    }
    
    length = (int)info.frames;
  }
  
  IntPtr sndfile;
  StreamIOCalls calls;
  protected int curPos;
}

public class SampleSource : AudioSource
{ public override bool CanRewind { get { return true; } }
  public override bool CanSeek   { get { return true; } }
  public override int  Position
  { get { return curPos; }
    set
    { if(value!=curPos)
      { if(value<0 || value>length) throw new ArgumentOutOfRangeException("Position");
        lock(this) curPos = value;
      }
    }
  }

  public override void Rewind() { Position=0; }

  public SampleSource(AudioSource stream) : this(stream, true) { }
  public SampleSource(AudioSource stream, bool mixerFormat)
  { if(stream.Length>=0)
    { data   = stream.ReadAll();
      format = stream.Format;

      if(mixerFormat)
      { data = Mixer.Convert(data, format, Mixer.Format).Shrunk;
        format = Mixer.Format;
      }
      length = data.Length;
    }
    else throw new NotImplementedException("SampleSource can't handle streams of unknown length");
  }

  public SampleSource(AudioSource stream, AudioFormat convertTo)
  { data = Mixer.Convert(stream.ReadAll(), stream.Format, format=convertTo).Shrunk;
    length = data.Length;
  }

  public override byte[] ReadAll() { return (byte[])data.Clone(); }

  public override int ReadBytes(byte[] buf, int length)
  { lock(this)
    { int toRead = Math.Min(length, this.length-curPos);
      Array.Copy(data, curPos, buf, 0, toRead);
      curPos += toRead;
      return toRead;
    }
  }

  public override unsafe int ReadFrames(int *dest, int frames, int volume)
  { lock(this)
    { int shift=format.FrameSize>>1, toRead=Math.Min(length-curPos, frames<<shift),
          readFrames=toRead>>shift, samples=readFrames*format.Channels;
      fixed(byte* src = data)
        if(format.Format==SampleFormat.MixerFormat)
          if(volume>=0) GLMixer.Mix(dest, (int*)(src+curPos), (uint)samples, (ushort)volume);
          else GLMixer.Copy(dest, (int*)(src+curPos), (uint)samples);
        else
          GLMixer.ConvertMix(dest, src+curPos, (uint)samples, (ushort)format.Format,
                             (ushort)(volume<0 ? Mixer.MaxVolume : volume));
      curPos += toRead;
      return readFrames;
    }
  }

  protected byte[] data;
  protected int curPos;
}
#endregion

#region Channel
public sealed class Channel
{ internal Channel(int channel) { number=channel; volume=Mixer.MaxVolume; }
  
  public event MixFilter Filters;
  public event ChannelFinishedHandler Finished;

  public int Number { get { return number; } }
  public int Volume
  { get { return volume; }
    set { Mixer.CheckVolume(value); volume = value; }
  }
  public AudioSource Source { get { return source; } }
  public int  Priority { get { return priority; } }
  public uint Age { get { return source==null ? 0 : Timing.Ticks-startTime; } }
  public AudioStatus Status
  { get { return source==null ? AudioStatus.Stopped : paused ? AudioStatus.Paused : AudioStatus.Playing; }
  }
  public Fade Fading { get { return fade; } }
  public int  Position { get { return position; } set { lock(this) position=value; } }
  public float PlaybackRate { get { return rate; } set { Mixer.CheckRate(rate); lock(this) rate=value; } }

  public void Pause()  { paused=true; }
  public void Resume() { paused=false; }
  public void Stop()   { lock(this) StopPlaying(); }

  public void FadeOut(uint fadeMs)
  { lock(this)
    { if(source==null) return;
      fade        = Fade.Out;
      fadeTime    = fadeMs;
      fadeStart   = Timing.Ticks;
      fadeVolume  = EffectiveVolume;
    }
  }

  internal void StartPlaying(AudioSource source, int loops, Fade fade, uint fadeMs, int timeoutMs)
  { StopPlaying();
    lock(source)
    { if(!source.CanRewind && loops!=0) throw new ArgumentException("Can't play loop sources that can't be rewound");
      if(!source.CanSeek && source.playing>0)
        throw new ArgumentException("Can't play unseekable streams more than once at a time");
      this.source   = source;
      this.loops    = loops;
      this.fade     = fade;
      this.timeout  = timeoutMs;
      priority  = source.Priority;
      position  = 0;
      paused    = false;
      startTime = Timing.Ticks;
      source.playing++;
      convert = !source.Format.Equals(Mixer.Format);
      if(convert)
      { GLMixer.AudioCVT cvt = Mixer.SetupCVT(source.Format, Mixer.Format);
        sdMul=cvt.lenMul; sdDiv=cvt.lenDiv;
      }
      else convBuf=null;
      if(fade!=Fade.None)
      { fadeTime   = fadeMs;
        fadeVolume = fade==Fade.In ? 0 : EffectiveVolume;
        fadeStart  = Timing.Ticks;
      }
    }
  }

  internal void StopPlaying()
  { if(source==null) return;
    lock(source)
    { if(Finished!=null) Finished(this);
      Mixer.OnChannelFinished(this);
      source = null;
    }
  }

  internal unsafe void Mix(int* stream, int frames, MixFilter filters)
  { if(source==null || paused) return;
    lock(source)
    { AudioFormat format = source.Format;
      float rate = EffectiveRate;
      int volume = EffectiveVolume, read, toRead;
      bool convert = this.convert;

      if(timeout!=Mixer.Infinite && Age>timeout)
      { StopPlaying();
        return;
      }
      if(fade!=Fade.None)
      { uint fadeSoFar = Timing.Ticks-fadeStart;
        int  target = fade==Fade.In ? volume : 0;
        if(fadeSoFar>fadeTime)
        { volume = target;
          if(fade==Fade.Out) { StopPlaying(); return; }
          fade = Fade.None;
        }
        else volume = fadeVolume + (target-fadeVolume)*(int)fadeSoFar/(int)fadeTime;
      }

      if(source.CanSeek) source.Position = position;
      if(convert || rate!=1f)
      { int len = frames*Mixer.Format.FrameSize;
        if(rate==1f) toRead = len*sdDiv/sdMul;
        else
        { int shift = format.FrameSize;
          THIS IS A HACK, FIX ME
          format.Frequency = (uint)(format.Frequency*rate)/50*50;
          if(format.Frequency==0) return;
          convert=true;
          GLMixer.AudioCVT cvt = Mixer.SetupCVT(format, Mixer.Format);
          toRead = (len*cvt.lenDiv/cvt.lenMul+shift)>>shift<<shift;
        }
        len = Math.Max(toRead, len);
        if(convBuf==null || convBuf.Length<len) convBuf = new byte[len];
      }
      else toRead = frames;

      while(true)
      { if(convert)
        { read = source.ReadBytes(convBuf, toRead);
          Mixer.SizedArray sa = Mixer.Convert(convBuf, format, Mixer.Format, read);
          if(sa.Array!=convBuf) convBuf = sa.Array;
          int framesRead = sa.Length/Mixer.Format.FrameSize, samples = framesRead*Mixer.Format.Channels;
          if(Filters==null && filters==null)
          { fixed(byte* src = convBuf)
                  GLMixer.Check(GLMixer.ConvertMix(stream, src, (uint)samples,
                                                  (ushort)Mixer.Format.Format, (ushort)volume));
          }
          else
          { int* buffer = stackalloc int[samples];
            fixed(byte* src = convBuf)
              GLMixer.Check(GLMixer.ConvertMix(buffer, src, (uint)samples,
                                              (ushort)Mixer.Format.Format, (ushort)Mixer.MaxVolume));
            if(Filters!=null) Filters(buffer, framesRead, Mixer.Format);
            if(filters!=null) filters(buffer, framesRead, Mixer.Format);
            GLMixer.Check(GLMixer.Mix(stream, buffer, (uint)samples, (ushort)volume));
          }
        }
        else
        { if(Filters==null && filters==null) read=source.ReadFrames(stream, toRead, volume);
          else
          { int* buffer = stackalloc int[toRead];
            read = source.ReadFrames(buffer, toRead, -1);
            if(read>0)
            { if(Filters!=null) Filters(buffer, read, format);
              if(filters!=null) filters(buffer, read, format);
              GLMixer.Check(GLMixer.Mix(stream, buffer, (uint)(read*Mixer.Format.Channels), (ushort)volume));
            }
          }
        }
        toRead -= read;
        if(toRead<=0) break;
        if(read==0)
        { if(loops==0) { StopPlaying(); return; }
          else
          { source.Rewind();
            position = 0;
            if(loops!=Mixer.Infinite) loops--;
          }
        }
      }
      position = source.Position;
    }
  }
  
  int   EffectiveVolume { get { int v=source.Volume; return v==Mixer.MaxVolume ? volume : (volume*v)>>8; } }
  float EffectiveRate   { get { return source.PlaybackRate*rate; } }

  AudioSource source;
  byte[] convBuf;
  float rate=1f;
  uint startTime, fadeStart, fadeTime;
  int  volume, fadeVolume, timeout, number, position, loops, priority, sdMul, sdDiv;
  Fade fade;
  bool paused, convert;
}
#endregion

#region Mixer class
public class Mixer
{ private Mixer() { }

  public const int Infinite=-1, FreeChannel=-1, MaxVolume=256;

  public static event MixFilter Filters
  { add    { lock(callback) eFilters += value; }
    remove { lock(callback) eFilters -= value; }
  }
  public static event MixFilter PostFilters
  { add    { lock(callback) ePostFilters += value; }
    remove { lock(callback) ePostFilters -= value; }
  }
  public static event ChannelFinishedHandler ChannelFinished;

  public static bool Initialized { get { return init; } }
  public static AudioFormat Format { get { CheckInit(); return format; } }
  public static object SyncRoot { get { CheckInit(); return callback; } }

  public static int MasterVolume
  { get { CheckInit(); return (int)GLMixer.GetMixVolume(); }
    set { CheckInit(); CheckVolume(value); GLMixer.SetMixVolume((ushort)value); }
  }
  public static int ReservedChannels
  { get { CheckInit(); return reserved; }
    set
    { CheckInit();
      if(reserved<0 || reserved>chans.Length) throw new ArgumentOutOfRangeException("value");
      reserved=value;
    }
  }
  public static Channel[] Channels { get { return chans; } }
  public static PlayPolicy PlayPolicy { get { return playPolicy; } set { playPolicy=value; } }
  public static MixPolicy  MixPolicy  { get { return mixPolicy; } set { mixPolicy=value; } }

  public static bool Initialize() { return Initialize(22050, SampleFormat.Default, Speakers.Stereo, 100); }
  public static bool Initialize(uint frequency) { return Initialize(frequency, SampleFormat.Default, Speakers.Stereo, 100); }
  public static bool Initialize(uint frequency, SampleFormat format) { return Initialize(frequency, format, Speakers.Stereo, 100); }
  public static bool Initialize(uint frequency, SampleFormat format, Speakers chans) { return Initialize(frequency, format, chans, 100); }
  public unsafe static bool Initialize(uint frequency, SampleFormat format, Speakers chans, uint bufferMs)
  { if(init) throw new InvalidOperationException("Already initialized. Deinitialize first to change format");
    SDL.Initialize(SDL.InitFlag.Audio);
    init        = true;
    callback    = new GLMixer.MixCallback(FillBuffer);
    Mixer.chans = new Channel[0];
    groups      = new ArrayList();

    try { GLMixer.Check(GLMixer.Init(frequency, (ushort)format, (byte)chans, bufferMs, callback, new IntPtr(null))); }
    catch(Exception e) { Deinitialize(); throw e; }
    
    uint freq, bytes;
    ushort form;
    byte   chan;
    SDL.Check(GLMixer.GetFormat(out freq, out form, out chan, out bytes));
    Mixer.format = new AudioFormat(freq, (SampleFormat)form, chan);
    
    SDL.PauseAudio(0);
    return freq==frequency && form==(short)format && chan==(byte)chans;
  }
  
  public static void Deinitialize()
  { if(init)
    { GLMixer.Quit();
      SDL.Deinitialize(SDL.InitFlag.Audio);
      chans  = null;
      groups = null;
      init   = false;
    }
  }
  
  public static void AllocateChannels(int numChannels)
  { CheckInit();
    if(numChannels<0) throw new ArgumentOutOfRangeException("numChannels");
    lock(callback)
    { for(int i=numChannels; i<chans.Length; i++) chans[i].Stop();
      Channel[] narr = new Channel[numChannels];
      Array.Copy(narr, chans, chans.Length);
      for(int i=chans.Length; i<numChannels; i++) narr[i] = new Channel(i);
      chans = narr;
      if(numChannels<reserved) reserved=numChannels;
    }
  }

  public int AddGroup()
  { CheckInit();
    lock(groups)
    { for(int i=0; i<groups.Count; i++)
        if(groups[i]==null)
        { groups[i] = new ArrayList();
          return i;
        }
      return groups.Add(new ArrayList());
    }
  }
  
  public void RemoveGroup(int group) { lock(groups) { GetGroup(group); groups[ToGroup(group)]=null; } }

  public void GroupChannel(int channel, int group)
  { CheckChannel(channel);
    lock(groups)
    { ArrayList list = GetGroup(group);
      if(!list.Contains(channel)) list.Add(channel);
    }
  }

  public void GroupRange(int start, int end, int group)
  { CheckChannel(start); CheckChannel(end);
    if(start>end) throw new ArgumentException("start should be <= end");
    lock(groups)
    { ArrayList list = GetGroup(group);
      for(; start<=end; start++) if(!list.Contains(start)) list.Add(start);
    }
  }
  
  public void UngroupChannel(int channel, int group) { lock(groups) GetGroup(group).Remove(channel); }
  
  public int GroupSize(int group) { lock(groups) return GetGroup(group).Count; }

  public int[] GetGroupChannels(int group) { lock(groups) return (int[])GetGroup(group).ToArray(typeof(int)); }

  public int OldestChannel(bool unreserved) { return OldestChannel(-1, unreserved); }
  public int OldestChannel(int group, bool unreserved)
  { int  i,oi;
    uint age=0;
    if(group==-1)
    { CheckInit();
      lock(callback)
        for(oi=unreserved ? 0 : reserved,i=oi; i<chans.Length; i++) if(chans[i].Age>age) { age=chans[i].Age; oi=i; }
    }
    else
      lock(groups)
      { ArrayList list = GetGroup(group);
        for(oi=i=0; i<list.Count; i++)
        { int chan = (int)list[i];
          if(!unreserved && chan<reserved) continue;
          if(chans[chan].Age>age) { age=chans[chan].Age; oi=chan; }
        }
      }
    return oi;
  }

  public void FadeOut(uint fadeMs) { FadeOut(-1, fadeMs); }
  public void FadeOut(int group, uint fadeMs)
  { if(group==-1) for(int i=0; i<chans.Length; i++) chans[i].FadeOut(fadeMs);
    else
      lock(groups)
      { ArrayList list = GetGroup(group);
        for(int i=0; i<list.Count; i++) chans[(int)list[i]].FadeOut(fadeMs);
      }
  }

  public void Pause() { Pause(-1); }
  public void Pause(int group)
  { if(group==-1) for(int i=0; i<chans.Length; i++) chans[i].Pause();
    else
      lock(groups)
      { ArrayList list = GetGroup(group);
        for(int i=0; i<list.Count; i++) chans[(int)list[i]].Pause();
      }
  }

  public void Resume() { Resume(-1); }
  public void Resume(int group)
  { if(group==-1) for(int i=0; i<chans.Length; i++) chans[i].Resume();
    else
      lock(groups)
      { ArrayList list = GetGroup(group);
        for(int i=0; i<list.Count; i++) chans[(int)list[i]].Resume();
      }
  }

  public void Stop() { Stop(-1); }
  public void Stop(int group)
  { if(group==-1) for(int i=0; i<chans.Length; i++) chans[i].Stop();
    else
      lock(groups)
      { ArrayList list = GetGroup(group);
        for(int i=0; i<list.Count; i++) chans[(int)list[i]].Stop();
      }
  }
  
  internal static void OnChannelFinished(Channel chan)
  { if(ChannelFinished!=null) ChannelFinished(chan);
  }
  
  internal static int StartPlaying(int channel, AudioSource source, int loops, Fade fade, uint fadeMs, int timeoutMs)
  { CheckInit();
    if(reserved==chans.Length) return -1;

    IList group=null;
    bool  tried=false;
    do
    { if(channel==FreeChannel)
      { for(int i=reserved; i<chans.Length; i++)
          if(chans[i].Status==AudioStatus.Stopped) // try to lock as little as possible
          { tried=true;
            lock(chans[i])
              if(chans[i].Status==AudioStatus.Stopped)
              { chans[i].StartPlaying(source, loops, fade, fadeMs, timeoutMs);
                return i;
              }
          }
      }
      else
        lock(groups)
        { group = GetGroup(channel);
          for(int i=0; i<group.Count; i++)
          { int chan = (int)group[i];
            if(chan<reserved) continue;
            if(chans[chan].Status==AudioStatus.Stopped) // try to lock as little as possible
            { tried=true;
              lock(chans[chan])
                if(chans[chan].Status==AudioStatus.Stopped)
                { chans[chan].StartPlaying(source, loops, fade, fadeMs, timeoutMs);
                  return chan;
                }
            }
          }
        }
    } while(!tried);

    switch(playPolicy)
    { case PlayPolicy.Oldest:
        lock(callback)
        { int  oi=reserved;
          uint age=0;
          if(channel==FreeChannel)
            for(int i=reserved; i<chans.Length; i++) { if(chans[i].Age>age) { age=chans[i].Age; oi=i; } }
          else
            lock(groups)
              for(int i=0; i<group.Count; i++)
              { int chan = (int)group[i];
                if(chan<reserved) continue;
                if(chans[chan].Age>age) { age=chans[chan].Age; oi=chan; }
              }
          lock(chans[oi]) chans[oi].StartPlaying(source, loops, fade, fadeMs, timeoutMs);
          return oi;
        }
      case PlayPolicy.Priority:
        lock(callback)
        { int pi=reserved, prio=int.MaxValue;
          if(channel==FreeChannel)
            for(int i=reserved; i<chans.Length; i++) { if(chans[i].Priority<prio) { prio=chans[i].Priority; pi=i; } }
          else
            lock(groups)
              for(int i=0; i<group.Count; i++)
              { int chan = (int)group[i];
                if(chan<reserved) continue;
                if(chans[chan].Priority<prio) { prio=chans[chan].Priority; pi=chan; }
              }
          lock(chans[pi]) chans[pi].StartPlaying(source, loops, fade, fadeMs, timeoutMs);
          return pi;
        }
      case PlayPolicy.OldestPriority:
        lock(callback)
        { int  pi=reserved, oi, prio=int.MaxValue;
          uint age=0;
          if(channel==FreeChannel)
          { for(int i=reserved; i<chans.Length; i++) if(chans[i].Priority<prio) { prio=chans[i].Priority; pi=i; }
            oi=pi;
            for(int i=reserved; i<chans.Length; i++)
              if(chans[i].Priority==prio && chans[i].Age>age) { oi=i; age=chans[i].Age; }
          }
          else
            lock(groups)
            { for(int i=0; i<group.Count; i++)
              { int chan = (int)group[i];
                if(chan<reserved) continue;
                if(chans[chan].Priority<prio) { prio=chans[chan].Priority; pi=chan; }
              }
              oi=pi;
              for(int i=0; i<group.Count; i++)
              { int chan = (int)group[i];
                if(chan<reserved) continue;
                if(chans[chan].Priority==prio && chans[chan].Age>age) { oi=chan; age=chans[chan].Age; }
              }
            }
          lock(chans[oi]) chans[oi].StartPlaying(source, loops, fade, fadeMs, timeoutMs);
          return oi;
        }
      default: return -1;
    }
  }

  internal class SizedArray
  { public SizedArray(byte[] array) { Array=array; Length=array.Length; }
    public SizedArray(byte[] array, int length)
    { Array=array; Length=length;
    }
    public byte[] Shrunk
    { get
      { if(Array.Length>Length)
        { byte[] ret = new byte[Length];
          System.Array.Copy(Array, ret, Length);
          Array=ret;
        }
        return Array;
      }
    }
    public byte[] Array;
    public int    Length;
  }
  
  internal static void CheckRate(float rate)
  { if(rate<0f) throw new ArgumentOutOfRangeException("PlaybackRate");
  }
  internal static void CheckChannel(int channel)
  { CheckInit();
    if(channel!=FreeChannel && channel<0) throw new ArgumentOutOfRangeException("channel");
  }
  internal static void CheckVolume(int volume)
  { if(volume<0 || volume>Mixer.MaxVolume) throw new ArgumentOutOfRangeException("value");
  }

  internal static GLMixer.AudioCVT SetupCVT(AudioFormat srcFormat, AudioFormat destFormat)
  { GLMixer.AudioCVT cvt = new GLMixer.AudioCVT();
    if(srcFormat.Equals(destFormat)) { cvt.lenMul=cvt.lenDiv=1; }
    if(srcFormat.Format==SampleFormat.MixerFormat && destFormat.Format==SampleFormat.MixerFormat)
      throw new NotImplementedException("Can't convert between two different mixer formats"); // FIXME: lift this restriction
    if(srcFormat.Format==SampleFormat.MixerFormat) { cvt.lenMul=destFormat.SampleSize; cvt.lenDiv=4; }
    else if(destFormat.Format==SampleFormat.MixerFormat) { cvt.lenMul=4; cvt.lenDiv=destFormat.SampleSize; }
    else
    { cvt.srcRate    = (int)srcFormat.Frequency;
      cvt.srcFormat  = (ushort)srcFormat.Format;
      cvt.srcChans   = srcFormat.Channels;
      cvt.destRate   = (int)destFormat.Frequency;
      cvt.destFormat = (ushort)destFormat.Format;
      cvt.destChans  = destFormat.Channels;
      GLMixer.Check(GLMixer.SetupCVT(ref cvt));
    }
    return cvt;
  }

  internal static SizedArray Convert(byte[] srcData, AudioFormat srcFormat, SampleFormat destFormat)
  { return Convert(srcData, srcFormat, destFormat, -1);
  }
  internal static SizedArray Convert(byte[] srcData, AudioFormat srcFormat, SampleFormat destFormat, int length)
  { AudioFormat df = srcFormat;
    df.Format = destFormat;
    return Convert(srcData, srcFormat, df, length);
  }
  internal static SizedArray Convert(byte[] srcData, AudioFormat srcFormat, AudioFormat destFormat)
  { return Convert(srcData, srcFormat, destFormat, -1);
  }
  internal static SizedArray Convert(byte[] srcData, AudioFormat srcFormat, AudioFormat destFormat, int length)
  { if(srcFormat.Equals(destFormat)) return new SizedArray(srcData);
    if(length>=0)
    { if(srcFormat.Format==SampleFormat.MixerFormat || destFormat.Format==SampleFormat.MixerFormat)
        throw new NotSupportedException("Can't convert to or from a mixer format inline");
      // FIXME: remove this restriction --^
    }
    else
    { if(srcFormat.Format==SampleFormat.MixerFormat && destFormat.Format==SampleFormat.MixerFormat)
        throw new NotSupportedException("Can't convert between two different mixer formats");
      // FIXME: remove this retriction --^
      if(srcFormat.Format==SampleFormat.MixerFormat)
      { int samples = srcData.Length/4;
        byte[] ret  = new byte[samples*destFormat.SampleSize];
        unsafe
        { fixed(byte* src = srcData)
            fixed(byte* dest = ret)
              GLMixer.Check(GLMixer.ConvertAcc(dest, (int*)src, (uint)samples, (ushort)destFormat.Format));
        }
        if(srcFormat.Channels!=destFormat.Channels || srcFormat.Frequency!=destFormat.Frequency)
        { AudioFormat format = srcFormat;
          format.Format = destFormat.Format;
          return Convert(ret, format, destFormat);
        }
        return new SizedArray(ret);
      }
      else if(destFormat.Format==SampleFormat.MixerFormat)
      { if(srcFormat.Channels!=Mixer.Format.Channels || srcFormat.Frequency!=Mixer.Format.Frequency)
        { SizedArray sa = Convert(srcData, srcFormat, Mixer.Format);
          srcData = sa.Array;
          length  = sa.Length;
          srcFormat = Mixer.Format;
        }
        else length = srcData.Length;
        int samples = length/srcFormat.SampleSize;
        byte[] ret = new byte[samples*4];
        unsafe
        { fixed(byte* src = srcData)
            fixed(byte* dest = ret)
              GLMixer.Check(GLMixer.ConvertMix((int*)dest, src, (uint)samples, (ushort)srcFormat.Format, MaxVolume));
        }
        return new SizedArray(ret);
      }
    }
    unsafe
    { GLMixer.AudioCVT cvt = new GLMixer.AudioCVT();
      if(length<0) length = srcData.Length;
      cvt.srcRate    = (int)srcFormat.Frequency;
      cvt.srcFormat  = (ushort)srcFormat.Format;
      cvt.srcChans   = srcFormat.Channels;
      cvt.destRate   = (int)destFormat.Frequency;
      cvt.destFormat = (ushort)destFormat.Format;
      cvt.destChans  = destFormat.Channels;
      cvt.len        = length;

      GLMixer.Check(GLMixer.SetupCVT(ref cvt));

      if(srcData.Length>=cvt.lenCvt)
      { fixed(byte* buf = srcData)
        { cvt.buf = buf;
          GLMixer.Check(GLMixer.Convert(ref cvt));
        }
        return new SizedArray(srcData, cvt.lenCvt);
      }
      else
      { byte[] ret = new byte[cvt.lenCvt];
        Array.Copy(srcData, ret, length);
        fixed(byte* buf = ret)
        { cvt.buf = buf;
          GLMixer.Check(GLMixer.Convert(ref cvt));
        }
        return new SizedArray(ret, cvt.lenCvt);
      }
    }
  }

  static int ToGroup(int group) { return -group-2; }
  static ArrayList GetGroup(int group)
  { CheckInit();
    int index = ToGroup(group);
    if(index<0 || index>=groups.Count) throw new ArgumentException("Invalid group ID");
    ArrayList list = (ArrayList)groups[ToGroup(group)];
    if(list==null) throw new ArgumentException("Invalid group ID");
    return list;
  }

  static void CheckInit()
  { if(!init) throw new InvalidOperationException("Mixer has not been initialized.");
  }
  static unsafe void FillBuffer(int* stream, uint frames, IntPtr context)
  { lock(callback)
    { for(int i=0; i<chans.Length; i++) lock(chans[i]) chans[i].Mix(stream, (int)frames, eFilters);
      if(ePostFilters!=null) ePostFilters(stream, (int)frames, format);
      if(MixPolicy==MixPolicy.Divide) GLMixer.DivideAccumulator(chans.Length);
    }
  }

  static AudioFormat format;
  static GLMixer.MixCallback callback;
  static Channel[] chans;
  static ArrayList groups;
  static MixFilter eFilters, ePostFilters;
  static int reserved;
  static PlayPolicy playPolicy = PlayPolicy.Fail;
  static MixPolicy  mixPolicy  = MixPolicy.DontDivide;
  static bool init;
}
#endregion

} // namespace GameLib.Audio