using System;
using GameLib.Interop.GLUtility;

namespace GameLib
{

public class Timing
{ private Timing() { }
  static Timing() // FIXME: Utility never deinitialized
  { Utility.Check(Utility.Init());
    timerFreq = Utility.GetTimerFrequency();
  }

  // tick timer (since GameLib was initialized)
  public static long Frequency { get { return timerFreq; } }
  public static long Counter { get { return Utility.GetTimerCounter(); } }

  // real-time timers (since GameLib was initialized)
  public static uint   Msecs   { get { return Utility.GetMilliseconds(); } }
  public static double Seconds { get { return Utility.GetSeconds(); } }

  static long timerFreq;
}

}