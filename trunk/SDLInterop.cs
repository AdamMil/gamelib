using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GameLib.Interop.SDL
{

[System.Security.SuppressUnmanagedCodeSecurity()]
internal class SDL
{

  [CallConvCdecl] public unsafe delegate int SeekHandler(RWOps* ops, int offset, SeekType type);
  [CallConvCdecl] public unsafe delegate int ReadHandler(RWOps* ops, byte* data, int size, int maxnum);
  [CallConvCdecl] public unsafe delegate int WriteHandler(RWOps* ops, byte* data, int size, int num);
  [CallConvCdecl] public unsafe delegate int CloseHandler(RWOps* ops);

  #region Constants
  public const uint CDFramesPerSecond=75;
  #endregion
  
  #region Enums
  #region Key enum
  public enum Key : int
  { Unknown=0, Backspace=8, Tab=9, Clear=12, Return=13, Pause=19, Escape=27, Space=32, Exclaim=33, Quotedbl=34,
    Hash=35, Dollar=36, Ampersand=38, Quote=39, LeftParen=40, RightParen=41, Asterisk=42, Plus=43, Comma=44,
    Minus=45, Period=46, Slash=47, Num0=48, Num1=49, Num2=50, Num3=51, Num4=52, Num5=53, Num6=54, Num7=55,
    Num8=56, Num9=57, Colon=58, Semicolon=59, Less=60, Equals=61, Greater=62, Question=63, At=64,
    LeftBracket=91, Backslash=92, RightBracket=93, Caret=94, Underscore=95, Backquote=96,
    A=97, B=98, C=99, D=100, E=101, F=102, G=103, H=104, I=105, J=106, K=107, L=108, M=109, N=110,
    O=111, P=112, Q=113, R=114, S=115, T=116, U=117, V=118, W=119, X=120, Y=121, Z=122, Delete=127,

    World_0=160,  World_1=161,  World_2=162,  World_3=163,  World_4=164,  World_5=165,  World_6=166,  World_7=167,
    World_8=168,  World_9=169,  World_10=170, World_11=171, World_12=172, World_13=173, World_14=174, World_15=175,
    World_16=176, World_17=177, World_18=178, World_19=179, World_20=180, World_21=181, World_22=182, World_23=183,
    World_24=184, World_25=185, World_26=186, World_27=187, World_28=188, World_29=189, World_30=190, World_31=191,
    World_32=192, World_33=193, World_34=194, World_35=195, World_36=196, World_37=197, World_38=198, World_39=199,
    World_40=200, World_41=201, World_42=202, World_43=203, World_44=204, World_45=205, World_46=206, World_47=207,
    World_48=208, World_49=209, World_50=210, World_51=211, World_52=212, World_53=213, World_54=214, World_55=215,
    World_56=216, World_57=217, World_58=218, World_59=219, World_60=220, World_61=221, World_62=222, World_63=223,
    World_64=224, World_65=225, World_66=226, World_67=227, World_68=228, World_69=229, World_70=230, World_71=231,
    World_72=232, World_73=233, World_74=234, World_75=235, World_76=236, World_77=237, World_78=238, World_79=239,
    World_80=240, World_81=241, World_82=242, World_83=243, World_84=244, World_85=245, World_86=246, World_87=247,
    World_88=248, World_89=249, World_90=250, World_91=251, World_92=252, World_93=253, World_94=254, World_95=255,

    KP0=256, KP1=257, KP2=258, KP3=259, KP4=260, KP5=261, KP6=262, KP7=263, KP8=264, KP9=265, KP_Period=266,
    KP_Divide=267, KP_Multiply= 268, KP_Minus=269, KP_Plus=270, KP_Enter=271, KP_Equals=272,

    Up=273, Down=274, Right=275, Left=276, Insert=277, Home=278, End=279, PageUp=280, PageDown=281,

    F1=282, F2=283, F3=284, F4=285, F5=286, F6=287, F7=288, F8=289, F9=290, F10=291, F11=292, F12=293, F13=294,
    F14=295, F15=296,

    NumLock=300, CapsLock=301, ScrollLock=302, RShift=303, LShift=304, RCtrl=305, LCtrl=306, RAlt=307, LAlt=308,
    RMeta=309, LMeta=310, LSuper=311, RSuper=312, Mode=313, Compose=314,

    Help=315, Print=316, Sysreq=317, Break=318, Menu=319, Power=320, Euro=321, Undo=322,
    
    NumKeys=323
  }
  #endregion

  [Flags]
  public enum KeyMod : uint
  { None=0, LShift=0x1, RShift=0x2, LCtrl=0x40, RCtrl=0x80, LAlt=0x100, RAlt=0x200, LMeta=0x400, RMeta=0x800,
    NumLock=0x1000, CapsLock=0x2000, Mode=0x4000,
    Shift=LShift|RShift, Ctrl=LCtrl|RCtrl, Alt=LAlt|RAlt, Meta=LMeta|RMeta
  }

  [Flags]
  public enum InitFlag : uint
  { Nothing=0, Timer=0x0001, Audio=0x0010, Video=0x0020, CDRom=0x0100, Joystick=0x0200, Everything=0xFFFF,
    NoParachute=0x100000, EventThread=0x1000000
  }
  [Flags]
  public enum VideoFlag : uint
  { None = 0,
    SWSurface   = 0x00000000, HWSurface = 0x00000001, AsyncBlit    = 0x00000004, 
    AnyFormat   = 0x10000000, HWPalette = 0x20000000, DoubleBuffer = 0x40000000,
    FullScreen  = 0x80000000, OpenGL    = 0x00000002, OpenGLBlit   = 0x0000000A,
    Resizable   = 0x00000010, NoFrame   = 0x00000020, RLEAccel     = 0x00004000,
    SrcColorKey = 0x00001000, SrcAlpha  = 0x00010000,
  }
  public enum EventType : byte
  { None, Active, KeyDown, KeyUp, MouseMove, MouseDown, MouseUp, JoyAxis, JoyBall, JoyHat, JoyDown, JoyUp,
    Quit, SysWMEvent, VideoResize=16, VideoExposed, UserEvent0=24, NumEvents=32
  }
  [Flags]
  public enum FocusType : byte
  { MouseFocus=1, InputFocus=2, AppActive=4
  }
  [Flags]
  public enum HatPos : byte
  { Centered=0, Up=1, Right=2, Down=4, Left=8,
    UpRight=Up|Right, UpLeft=Up|Left, DownRight=Down|Right, DownLeft=Down|Left
  }
  public enum TrackType : byte
  { Audio=0, Data=4
  }
  public enum CDStatus : int
  { TrayEmpty, Stopped, Playing, Paused, Error=-1
  }
  public enum SeekType : int
  { Absolute, Relative, FromEnd
  }

  [Flags]
  public enum InfoFlag : uint
  { Hardware=0x0001, WindowManager=0x0002, HH=0x0200, HHKeyed=0x0400, HHAlpha=0x0800,
    SH=0x1000, SHKeyed=0x2000, SHAlpha=0x4000, Fills=0x8000
  }
  
  public enum AudioStatus : int
  { Stopped, Playing, Paused
  }
  #endregion

  #region Structs
  [StructLayout(LayoutKind.Sequential, Pack=4)]
  public struct Rect
  { public Rect(System.Drawing.Rectangle rect)
    { X = (short)rect.X; Y = (short)rect.Y; Width = (ushort)rect.Width; Height = (ushort)rect.Height;
    }
    public Rect(int x, int y) { X=(short)x; Y=(short)y; Width=Height=1; }
    public System.Drawing.Rectangle ToRectangle() { return new System.Drawing.Rectangle(X, Y, Width, Height); }
    public short  X, Y;
    public ushort Width, Height;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct Color
  { public Color(System.Drawing.Color c) { Value=0; Red=c.R; Green=c.G; Blue=c.B; Alpha=c.A; }
    [FieldOffset(0)] public byte Red;
    [FieldOffset(1)] public byte Green;
    [FieldOffset(2)] public byte Blue;
    [FieldOffset(3)] public byte Alpha; // TODO: make bigendian friendly? (is it necessary? check SDL docs)

    [FieldOffset(0)] public uint Value;
  }
  [StructLayout(LayoutKind.Sequential, Pack=4)]
  public unsafe struct Palette
  { public int Entries;
    public Color* Colors;
  }
  [StructLayout(LayoutKind.Sequential, Pack=4)]
  public unsafe struct PixelFormat
  { public Palette* Palette;
    public byte BitsPerPixel, BytesPerPixel;
    public byte Rloss, Gloss, Bloss, Aloss;
    public byte Rshift, Gshift, Bshift, Ashift;
    public uint Rmask, Gmask, Bmask, Amask;
    public uint Key;
    public byte Alpha;
  }
  [StructLayout(LayoutKind.Sequential, Pack=4)]
  public unsafe struct Surface
  { public  uint Flags;
    public  PixelFormat* Format;
    public  int    Width, Height;
    public  ushort Pitch;
    public  void*  Pixels;
    private int    Offset;
    private IntPtr HWData;
    public  Rect   ClipRect;
    private uint   Unused;
    public  uint   Locked;
    private IntPtr Map;
    private uint   FormatVersion;
    private int    RefCount;
  }
  [StructLayout(LayoutKind.Explicit)] // TODO: do something to make these more compatible with different systems
  public struct KeySym
  { [FieldOffset(0)]  public byte Scan;
    [FieldOffset(4)]  public int  Sym;
    [FieldOffset(8)]  public uint Mod;
    [FieldOffset(12)] public char Unicode;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct Event
  { [FieldOffset(0)] public EventType        Type;
    [FieldOffset(0)] public ActiveEvent      Active;
    [FieldOffset(0)] public KeyboardEvent    Keyboard;
    [FieldOffset(0)] public MouseMoveEvent   MouseMove;
    [FieldOffset(0)] public MouseButtonEvent MouseButton;
    [FieldOffset(0)] public JoyAxisEvent     JoyAxis;
    [FieldOffset(0)] public JoyBallEvent     JoyBall;
    [FieldOffset(0)] public JoyHatEvent      JoyHat;
    [FieldOffset(0)] public JoyButtonEvent   JoyButton;
    [FieldOffset(0)] public ResizeEvent      Resize;
    [FieldOffset(0)] public ExposedEvent     Exposed;
    [FieldOffset(0)] public QuitEvent        Quit;
    [FieldOffset(0)] public UserEvent        User;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct ActiveEvent
  { [FieldOffset(0)] public EventType Type;
    [FieldOffset(1)] public byte      Focused;
    [FieldOffset(2)] public FocusType State;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct KeyboardEvent
  { [FieldOffset(0)] public EventType Type;
    [FieldOffset(1)] public byte   Device;
    [FieldOffset(2)] public byte   Down;
    [FieldOffset(4)] public KeySym Key;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct MouseMoveEvent
  { [FieldOffset(0)]  public EventType Type;
    [FieldOffset(1)]  public byte   Device;
    [FieldOffset(2)]  public byte   State;
    [FieldOffset(4)]  public ushort X;
    [FieldOffset(6)]  public ushort Y;
    [FieldOffset(8)]  public short  Xrel;
    [FieldOffset(10)] public short  Yrel;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct MouseButtonEvent
  { [FieldOffset(0)] public EventType Type;
    [FieldOffset(1)] public byte   Device;
    [FieldOffset(2)] public byte   Button;
    [FieldOffset(3)] public byte   Down;
    [FieldOffset(4)] public ushort X;
    [FieldOffset(6)] public ushort Y;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct JoyAxisEvent
  { [FieldOffset(0)] public EventType Type;
    [FieldOffset(1)] public byte  Device;
    [FieldOffset(2)] public byte  Axis;
    [FieldOffset(4)] public short Value;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct JoyBallEvent
  { [FieldOffset(0)] public EventType Type;
    [FieldOffset(1)] public byte  Device;
    [FieldOffset(2)] public byte  Ball;
    [FieldOffset(4)] public short Xrel;
    [FieldOffset(6)] public short Yrel;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct JoyHatEvent
  { [FieldOffset(0)] public EventType Type;
    [FieldOffset(1)] public byte   Device;
    [FieldOffset(2)] public byte   Hat;
    [FieldOffset(3)] public HatPos Position;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct JoyButtonEvent
  { [FieldOffset(0)] public EventType Type;
    [FieldOffset(1)] public byte Device;
    [FieldOffset(2)] public byte Button;
    [FieldOffset(3)] public byte Down;
  }
  [StructLayout(LayoutKind.Explicit)]
  public struct ResizeEvent
  { [FieldOffset(0)] public EventType Type;
    [FieldOffset(4)] public int Width;
    [FieldOffset(8)] public int Height;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct ExposedEvent
  { public EventType Type;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct QuitEvent
  { public EventType Type;
  }
  [StructLayout(LayoutKind.Sequential, Pack=4)]
  public struct UserEvent
  { public EventType Type;
    public int    Code;
    public IntPtr Data1;
    public IntPtr Data2;
  }
  [StructLayout(LayoutKind.Sequential, Pack=4, Size=12)]
  public struct CDTrack
  { public byte      Number;
    public TrackType Type;
    public int       Length, Offset; // in frames
  }
  [StructLayout(LayoutKind.Sequential, Pack=4, Size=1220)]
  public struct CD
  { public unsafe CDTrack* GetTrack(int track)
    { if(track<0 || track>=NumTracks) throw new ArgumentOutOfRangeException("track");
      fixed(CDTrack* tracks=&Tracks) return tracks+track;
    }
    public int      ID;
    public CDStatus Status;
    public int NumTracks;
    public int CurTrack;
    public int CurFrame;
    public CDTrack Tracks;
  };
  [StructLayout(LayoutKind.Sequential, Pack=4)]
  public unsafe struct RWOps
  { public void* Seek, Read, Write, Close;
    uint type;
    void* p1, p2, p3;
  }
  [StructLayout(LayoutKind.Sequential, Pack=4)]
  public unsafe struct VideoInfo
  { public InfoFlag flags;
    public uint videoMem;
    void* format;
  }
  [StructLayout(LayoutKind.Sequential, Pack=4)]
  public unsafe struct AudioSpec
  { public int   Freq;
    public short Format;
    public byte  Channels;
    public byte  Silence;
    public short Samples;
    public uint  Size;
    public void* Callback, UserData;
  }
  [StructLayout(LayoutKind.Sequential, Pack=4)]
  public unsafe struct AudioCVT
  { int    needed;
    short  src_format, dst_format;
    double rate_incr;
    public byte*  buf;
    public int    len, len_cvt, len_mult;
    public double len_ratio;
    void*  f0, f1, f2, f3, f4, f5, f6, f7, f8, f9;
    int    filter_index;
  }
  #endregion
  
  #region General
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_Init", CallingConvention=CallingConvention.Cdecl)]
  private static extern int Init(InitFlag flags);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_InitSubSystem", CallingConvention=CallingConvention.Cdecl)]
  private static extern int InitSubSystem(InitFlag systems);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_QuitSubSystem", CallingConvention=CallingConvention.Cdecl)]
  private static extern void QuitSubSystem(InitFlag systems);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_Quit", CallingConvention=CallingConvention.Cdecl)]
  private static extern void Quit();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_WasInit", CallingConvention=CallingConvention.Cdecl)]
  public static extern uint WasInit(InitFlag systems);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetError", CallingConvention=CallingConvention.Cdecl)]
  public static extern string GetError();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_ClearError", CallingConvention=CallingConvention.Cdecl)]
  public static extern void ClearError();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetTicks", CallingConvention=CallingConvention.Cdecl)]
  public static extern uint GetTicks();

  [DllImport(Config.SDLImportPath, EntryPoint="SDL_RWFromFile", CallingConvention=CallingConvention.Cdecl)]
  public static extern RWOps* RWFromFile(string file, string mode);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_RWFromMem", CallingConvention=CallingConvention.Cdecl)]
  public static extern RWOps* RWFromMem(byte[] mem, int size);
  #endregion

  #region Video
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_VideoModeOK", CallingConvention=CallingConvention.Cdecl)]
  public static extern int VideoModeOK(int width, int height, uint depth, uint flags);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetVideoInfo", CallingConvention=CallingConvention.Cdecl)]
  public static extern VideoInfo* GetVideoInfo();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_SetVideoMode", CallingConvention=CallingConvention.Cdecl)]
  public static extern Surface* SetVideoMode(int width, int height, uint depth, uint flags);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_FreeSurface", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern void FreeSurface(Surface* surface);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_Flip", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int Flip(Surface* screen);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_UpdateRect", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int UpdateRect(Surface* screen, int x, int y, int width, int height);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_UpdateRects", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int UpdateRects(Surface* screen, uint numRects, Rect* rects);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_FillRect", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int FillRect(Surface* surface, ref Rect rect, uint color);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_MapRGBA", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern uint MapRGBA(PixelFormat* format, byte r, byte g, byte b, byte a);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetRGBA", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern void GetRGBA(uint pixel, PixelFormat* format, out byte r, out byte g, out byte b, out byte a);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_ShowCursor", CallingConvention=CallingConvention.Cdecl)]
  public static extern int ShowCursor(int toggle);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_WarpMouse", CallingConvention=CallingConvention.Cdecl)]
  public static extern void WarpMouse(ushort x, ushort y);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CreateRGBSurface", CallingConvention=CallingConvention.Cdecl)]
  public static extern Surface* CreateRGBSurface(uint flags, int width, int height, uint depth, uint Rmask, uint Gmask, uint Bmask, uint Amask);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_UpperBlit", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int BlitSurface(Surface* src, Rect* srcrect, Surface* dest, Rect* destrect);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetVideoSurface", CallingConvention=CallingConvention.Cdecl)]
  public static extern Surface* GetVideoSurface();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_SetPalette", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int SetPalette(Surface* surface, uint flags, Color* colors, uint firstColor, uint numColors);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_SetGamma", CallingConvention=CallingConvention.Cdecl)]
  public static extern int SetGamma(float red, float green, float blue);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetGammaRamp", CallingConvention=CallingConvention.Cdecl)]
  public static extern int GetGammaRamp(ushort[] red, ushort[] green, ushort[] blue);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_SetGammaRamp", CallingConvention=CallingConvention.Cdecl)]
  public static extern int SetGammaRamp(ushort[] red, ushort[] green, ushort[] blue);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_LockSurface", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int LockSurface(Surface* surface);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_UnlockSurface", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern void UnlockSurface(Surface* surface);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_SetColorKey", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int SetColorKey(Surface* surface, uint flag, uint key);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_SetClipRect", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern void SetClipRect(Surface* surface, ref Rect rect);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetClipRect", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern void GetClipRect(Surface* surface, ref Rect rect);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_DisplayFormat", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern Surface* DisplayFormat(Surface* surface);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_DisplayFormatAlpha", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern Surface* DisplayFormatAlpha(Surface* surface);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_ConvertSurface", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern Surface* ConvertSurface(Surface* src, PixelFormat* format, uint flags);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_SetAlpha", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int SetAlpha(Surface* src, uint flag, byte alpha);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_ListModes", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern Rect** ListModes(PixelFormat* format, uint flags);
  #endregion

  #region Audio
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_OpenAudio", CallingConvention=CallingConvention.Cdecl)]
  public static extern int OpenAudio(ref AudioSpec desired, out AudioSpec obtained);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_PauseAudio", CallingConvention=CallingConvention.Cdecl)]
  public static extern void PauseAudio(int pause);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetAudioStatus", CallingConvention=CallingConvention.Cdecl)]
  public static extern AudioStatus GetAudioStatus();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_BuildAudioCVT", CallingConvention=CallingConvention.Cdecl)]
  public static extern int BuildAudioCVT(out AudioCVT cvt, short src_format, byte src_channels, uint src_rate, short dst_format, byte dst_channels, uint dst_rate);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_ConvertAudio", CallingConvention=CallingConvention.Cdecl)]
  public static extern int ConvertAudio(ref AudioCVT cvt);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_MixAudio", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern void MixAudio(byte* dst, byte* src, uint len, int volume);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_LockAudio", CallingConvention=CallingConvention.Cdecl)]
  public static extern void LockAudio();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_UnlockAudio", CallingConvention=CallingConvention.Cdecl)]
  public static extern void UnlockAudio();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CloseAudio", CallingConvention=CallingConvention.Cdecl)]
  public static extern void CloseAudio();
  #endregion
  
  #region Events
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_PollEvent", CallingConvention=CallingConvention.Cdecl)]
  public static extern int PollEvent(ref Event evt);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_WaitEvent", CallingConvention=CallingConvention.Cdecl)]
  public static extern int WaitEvent(ref Event evt);
  #endregion
  
  #region Input
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_EnableUNICODE", CallingConvention=CallingConvention.Cdecl)]
  public static extern int EnableUNICODE(int enable);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_EnableKeyRepeat", CallingConvention=CallingConvention.Cdecl)]
  public static extern int EnableKeyRepeat(int delay, int interval);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetKeyState", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern byte* GetKeyState(int* numkeys);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetModState", CallingConvention=CallingConvention.Cdecl)]
  public static extern KeyMod GetModState();

  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GetMouseState", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern byte GetMouseState(int* x, int* y);
  #endregion

  #region Window manager
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_WM_SetCaption", CallingConvention=CallingConvention.Cdecl)]
  public static extern void WM_SetCaption(string title, string icon);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_WM_GetCaption", CallingConvention=CallingConvention.Cdecl)]
  public static extern void WM_GetCaption(out string title, out string icon);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_WM_SetIcon", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern void WM_SetIcon(Surface* icon, ref byte mask);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_WM_IconifyWindow", CallingConvention=CallingConvention.Cdecl)]
  public static extern int WM_IconifyWindow();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_WM_GrabInput", CallingConvention=CallingConvention.Cdecl)]
  public static extern int WM_GrabInput(int grab);
  #endregion
  
  #region Joysticks
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_NumJoysticks", CallingConvention=CallingConvention.Cdecl)]
  public static extern int NumJoysticks();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_JoystickName", CallingConvention=CallingConvention.Cdecl)]
  public static extern string JoystickName(int index);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_JoystickOpen", CallingConvention=CallingConvention.Cdecl)]
  public static extern IntPtr JoystickOpen(int index);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_JoystickIndex", CallingConvention=CallingConvention.Cdecl)]
  public static extern int JoystickIndex(IntPtr joystick);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_JoystickNumAxes", CallingConvention=CallingConvention.Cdecl)]
  public static extern int JoystickNumAxes(IntPtr joystick);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_JoystickNumBalls", CallingConvention=CallingConvention.Cdecl)]
  public static extern int JoystickNumBalls(IntPtr joystick);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_JoystickNumHats", CallingConvention=CallingConvention.Cdecl)]
  public static extern int JoystickNumHats(IntPtr joystick);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_JoystickNumButtons", CallingConvention=CallingConvention.Cdecl)]
  public static extern int JoystickNumButtons(IntPtr joystick);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_JoystickClose", CallingConvention=CallingConvention.Cdecl)]
  public static extern void JoystickClose(IntPtr joystick);
  #endregion

  #region OpenGL
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GL_SwapBuffers", CallingConvention=CallingConvention.Cdecl)]
  public static extern void SwapBuffers();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GL_SetAttribute", CallingConvention=CallingConvention.Cdecl)]
  public static extern int SetAttribute(int attribute, int value);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_GL_GetAttribute", CallingConvention=CallingConvention.Cdecl)]
  public static extern int GetAttribute(int attribute, out int value);
  #endregion

  #region CDROM
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDNumDrives", CallingConvention=CallingConvention.Cdecl)]
  public static extern int CDNumDrives();
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDName", CallingConvention=CallingConvention.Cdecl)]
  public static extern string CDName(int drive);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDOpen", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern CD* CDOpen(int drive);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDStatus", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern CDStatus GetCDStatus(CD* cdrom);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDPlay", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int CDPlay(CD* cdrom, int start, int length);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDPlayTracks", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int CDPlayTracks(CD* cdrom, int startTrack, int startFrame, int numTracks, int numFrames);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDPause", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int CDPause(CD* cdrom);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDResume", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int CDResume(CD* cdrom);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDStop", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int CDStop(CD* cdrom);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDEject", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern int CDEject(CD* cdrom);
  [DllImport(Config.SDLImportPath, EntryPoint="SDL_CDClose", CallingConvention=CallingConvention.Cdecl)]
  public unsafe static extern void CDClose(CD* cdrom);
  #endregion
  
  #region Non-SDL helper functions
  public static void Check(int result) { if(result<0) SDL.RaiseError(); }

  public static void RaiseError() // TODO: add parameter specifying type of exception to throw (instead of GameLibException)
  { string error = GetError();
    if(error==null) throw new GameLibException("GameLib sez: Something bad happened, but SDL disagrees");
    ClearError();
    if(error.IndexOf("Surface was lost")!=-1) throw new SurfaceLostException(error);
    else throw new GameLibException(error);
  }
  
  public static void Initialize(InitFlag sys)
  { if(sys==InitFlag.Nothing) return;
  
    int  i;
    bool done=false;

    for(i=0; i<counts.Length; i++) if(counts[i]!=0) break;
    if(i==counts.Length) { Check(Init(sys)); done=true; }

    for(i=0; i<counts.Length; i++)
      if((sys&inits[i])!=0)
      { if(counts[i]++==0 && !done) InitSubSystem(inits[i]); // TODO: check for errors
        sys &= ~inits[i];
      }
    if(sys!=InitFlag.Nothing && !done)
      throw new ArgumentException("Unknown flag, or flag that must be passed on the first call.", "sys");
  }

  public static void Deinitialize(InitFlag sys)
  { if(sys==InitFlag.Nothing) return;

    uint count=0;
    for(int i=0; i<counts.Length; i++) count += counts[i];
    if(count==0) throw new InvalidOperationException("Deinitialize called too many times!");

    for(int i=0; i<counts.Length; i++)
      if((sys&inits[i])!=0)
      { if(--counts[i]==0) QuitSubSystem(inits[i]);
        sys &= ~inits[i];
        count--;
      }
    if(sys!=InitFlag.Nothing) throw new ArgumentException("Invalid flag(s)", "sys");
    if(count==0) Quit();
  }
  #endregion
  
  static readonly InitFlag[] inits = new InitFlag[6]
  { InitFlag.Timer, InitFlag.Audio, InitFlag.Video, InitFlag.CDRom, InitFlag.Joystick, InitFlag.EventThread
  };
  static uint[] counts = new uint[6];
}

#region StreamSource class
internal class StreamSource
{ public unsafe StreamSource(Stream stream)
  { if(stream==null) throw new ArgumentNullException("stream");
    else if(!stream.CanSeek || !stream.CanRead)
      throw new ArgumentException("Stream must be seekable and readable", "stream");
    this.stream = stream;
    seek  = new SDL.SeekHandler(OnSeek);
    read  = new SDL.ReadHandler(OnRead);
    write = new SDL.WriteHandler(OnWrite);
    close = new SDL.CloseHandler(OnClose);
    ops.Seek  = new DelegateMarshaller(seek).ToPointer();
    ops.Read  = new DelegateMarshaller(read).ToPointer();
    ops.Write = new DelegateMarshaller(write).ToPointer();
    ops.Close = new DelegateMarshaller(close).ToPointer();
  }

  unsafe int OnSeek(SDL.RWOps* ops, int offset, SDL.SeekType type)
  { long pos=-1;
    switch(type)
    { case SDL.SeekType.Absolute: pos = stream.Seek(offset, SeekOrigin.Begin); break;
      case SDL.SeekType.Relative: pos = stream.Seek(offset, SeekOrigin.Current); break;
      case SDL.SeekType.FromEnd:  pos = stream.Seek(offset, SeekOrigin.End); break;
    }
    return (int)pos;
  }
  
  unsafe int OnRead(SDL.RWOps* ops, byte* data, int size, int maxnum)
  { if(size<=0 || maxnum<=0) return 0;

    byte[] buf = new byte[size];
    int i=0, read;
    try
    { for(; i<maxnum; i++)
      { read = stream.Read(buf, 0, size);
        if(read!=size) { return i==0 ? -1 : i; }
        for(int j=0; j<size; j++) *data++=buf[j];
      }
      return i;
    }
    catch { return i==0 ? -1 : i; }
  }

  unsafe int OnWrite(SDL.RWOps* ops, byte* data, int size, int num)
  { if(!stream.CanWrite) return -1;
    if(size<=0 || num<=0) return 0;
    int total=size*num, len = Math.Min(total, 1024);
    byte[] buf = new byte[len];
    try
    { do
      { if(total<len) len=total;
        for(int i=0; i<len; i++) buf[i]=*data++;
        stream.Write(buf, 0, len);
        total -= len;
      } while(total>0);
      return size;
    }
    catch { return -1; }
  }
  
  unsafe int OnClose(SDL.RWOps* ops) { stream=null; return 0; }
  
  internal SDL.RWOps ops;
  Stream stream;
  SDL.SeekHandler  seek;
  SDL.ReadHandler  read;
  SDL.WriteHandler write;
  SDL.CloseHandler close;
}
#endregion

} // namespace GameLib.InterOp.SDL