// 
//  Connector.cs
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
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace scfscli
{
	public class Connector
	{
		public int RemotePort = 6034;
		public string RemoteIP = "127.0.0.1";
		public TcpClient Client;
		
		public Connector ()
		{
			
		}
		
		public bool Connect()
		{
			try
			{
				Client = new TcpClient();
				Client.Connect(RemoteIP, RemotePort);
			}
			catch(Exception ex)
			{
				Console.WriteLine("Connection to {0}:{1} failed.", RemoteIP, RemotePort);
				Console.WriteLine(ex.Message);
				return false;
			}
			
			return true;
		}
	}
}

