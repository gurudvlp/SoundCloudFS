// 
//  Interface.cs
//  
//  Author:
//       Brian Murphy <gurudvlp@gmail.com>
// 
//  Copyright (c) 2012 Brian Murphy
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// 


using System;

namespace btEngine
{


	public abstract class Interface
	{
		public string IncomingBuffer = "";
		public string OutgoingBuffer = "";
		public bool TerminateAfterSend = false;
		public bool UseAsciiOutput = true;
		public byte[] OutgoingByteBuffer;
		public string RemoteIP = "";
		
		public Interface ()
		{
		}
		
		public void ReceivedData(string datain)
		{
			if(datain == null) { return; }
			IncomingBuffer = IncomingBuffer + datain;
		}
		
		public abstract bool TakeTurn();
		
		public abstract bool Run(string commandstring);
	}
}
