/*
GameLib is a library for developing games and other multimedia applications.
http://www.adammil.net/
Copyright (C) 2002-2004 Adam Milazzo

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

#include "VorbisWrapper.h"
#include <stdlib.h>

size_t rwRead(void *ptr, size_t size, size_t nmemb, void *datasource)
{ return ((VW_Callbacks*)datasource)->Read(ptr, (Sint32)size, (Sint32)nmemb);
}

int rwSeek(void *datasource, ogg_int64_t offset, int whence)
{ return ((VW_Callbacks*)datasource)->Seek((Sint32)offset, whence);
}

int rwClose(void *datasource)
{ ((VW_Callbacks*)datasource)->Close();
  return 0;
}

long rwTell(void *datasource) { return ((VW_Callbacks*)datasource)->Tell(); }

ov_callbacks Callbacks = { rwRead, rwSeek, rwClose, rwTell };

int VW_Open(OggVorbis_File **vf, VW_Callbacks calls)
{ int ret;
  VW_Callbacks *pcalls;
  *vf = (OggVorbis_File*)malloc(sizeof(OggVorbis_File)+sizeof(VW_Callbacks));
  pcalls = (VW_Callbacks*)(*vf+1);
  *pcalls = calls;
  ret = ov_open_callbacks(pcalls, *vf, NULL, 0, Callbacks);
  if(ret<=-128) { free(vf); *vf=0; }
  return ret;
}
void VW_Close(OggVorbis_File *vf) { ov_clear(vf); free(vf); }

Sint32 VW_PcmLength(OggVorbis_File *vf, int section) { return (Sint32)ov_pcm_total(vf, section); }
Sint32 VW_PcmTell(OggVorbis_File *vf) { return (Sint32)ov_pcm_tell(vf); }
Sint32 VW_PcmSeek(OggVorbis_File *vf, Sint32 frames) { return (Sint32)ov_pcm_seek(vf, frames); }

double VW_TimeLength(OggVorbis_File *vf, int section) { return ov_time_total(vf, section); }
double VW_TimeTell(OggVorbis_File *vf);
Sint32 VW_TimeSeek(OggVorbis_File *vf, double seconds) { return ov_time_seek(vf, seconds); }

Sint32 VW_Read(OggVorbis_File *vf, Uint8 *buf, Sint32 length, int bigEndian, int word, int sgned, int *section)
{ return ov_read(vf, (char*)buf, length, bigEndian, word, sgned, section);
}

vorbis_info * VW_Info(OggVorbis_File *vf, int section) { return ov_info(vf, section); }
