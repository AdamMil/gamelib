/*
This is an example application for the GameLib multimedia/gaming library.
http://www.adammil.net/
Copyright (C) 2004 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Drawing;
using GameLib.Events;
using GameLib.Video;

namespace BasicVideo
{

class App
{ static void Main()
  { 
    #if DEBUG
    string dataPath = "../../../"; // set to something correct
    #else
    string dataPath = "data/"; // set to something correct
    #endif
    
    Video.Initialize();
    Video.SetMode(640, 480, 32); // set 640x480x32bpp windowed mode

    Video.DisplaySurface.Fill(new Rectangle(0, 0, Video.Width/2, Video.Height/2), Color.White);

    Surface smiley = new Surface(dataPath+"smiley.png"); // load a smiley face image
    // display it centered (uses alpha blending)
    smiley.Blit(Video.DisplaySurface, (Video.Width-smiley.Width)/2, (Video.Height-smiley.Height)/2);
    smiley.UsingAlpha = false; // turn off alpha blending for this image
    smiley.Blit(Video.DisplaySurface, (Video.Width-smiley.Width)/2, 0); // blit it centered horizontally only
    
    Surface temp = new Surface(100, 100, 24); // 100x100x24bpp surface without alpha blending
    temp.SetColorKey(Color.Magenta); // set its transparent color to be magenta
    temp.Fill(Color.Magenta); // and fill it with magenta (transparent)
    Primitives.Box(temp, temp.Bounds, Color.Green); // draw a box surrounding it
    Primitives.Circle(temp, temp.Width/2, temp.Height/2, 40, Color.Red); // with a red circle
    // now blit the temporary onto the display buffer
    temp.Blit(Video.DisplaySurface, 40, (Video.Height-temp.Height)/2);

    Events.Initialize();
    while(true)
    { Event e = Events.NextEvent();
      if(e is KeyboardEvent)
      { KeyboardEvent ke = (KeyboardEvent)e;
        if(ke.Down && ke.Key==GameLib.Input.Key.Escape) break; // quit on escape
      }
      else if(e is QuitEvent) break; // or when the system tells us to
      else if(e is RepaintEvent) Video.Flip(); // repaint the screen when necessary
    }
    
    Video.Deinitialize();
  }
}

} // namespace BasicVideo