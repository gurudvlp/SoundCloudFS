// 
//  Listeners.cs
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
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Data;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace btEngine
{
	
	[XmlRoot("TcpListener")]
	public class Listeners
	{
		//	This class provides the ability to listen for incoming connections.
		[XmlElement("Port")] public int Port = -1;
		[XmlElement("Title")] public string Title = "[Untitled Listener]";
		[XmlElement("Protocol")] public string Protocol = "[N/A]";
		
		
		[XmlIgnore()] private TcpListener tcpListener;
    	[XmlIgnore()] private Thread listenThread;
		
		public Listeners()
		{
			//
			//	This object listens for incoming TCP Connections
			//
		}
		
		public Listeners(int port, string title, string protocol)
		{
			this.Port = port;
			this.Title = title;
			this.Protocol = protocol;
		}
		
		public void Listen()
		{
			if(this.Port == -1)
			{
				return;
			}
			
			if(this.Port < 0 || this.Port > 65535)
			{
				Console.WriteLine("Invalid port ({0}), choose 1 - 65535", this.Port);
				return;
			}
			
			this.tcpListener = new TcpListener(IPAddress.Any, this.Port);
      		this.listenThread = new Thread(new ThreadStart(ListenForClients));
      		
			this.listenThread.Start();
		}
		
		private void ListenForClients()
		{
			//
			//	This function runs as a seperate thread, and simply listens for
			//	incoming connections.
			//	The objects for new connections are passed to the Connections
			//	static method, which will assign it to a connection id.
			//
			
  			this.tcpListener.Start();
			
			//Console.WriteLine("TcpListener Activated: {0}::{1} : {2}", this.Protocol, this.Port, this.Title);
			Logging.Write("btEngine: Listener: " + this.Protocol + " " + this.Port.ToString() + " " + this.Title + " activated.");
			
  			while (true)
  			{
    			//blocks until a client has connected to the server
    			TcpClient client = this.tcpListener.AcceptTcpClient();
				
				//	a connection has been made!!  woot
				//Console.WriteLine("{0} :: Connection Established", this.Protocol);
				
    			//	Originally this spawned a new thread for each
				//	connected client.  This causes a lot of crashes though
				//	because of how data can easily be missing exactly when it
				//	is needed.   So, rather than having seperate threads for
				//	each client, there will be one 'clients' thread, which
				//	handles each connection in a loop.
				
				IncomingConnections.AddIncomingConnection(client, this.Protocol);
				
  			}
		}
		
		public bool Save(string conffilename)
		{
			//
			//	Save this listener.
			//
			
			try
			{
				XmlSerializer s = new XmlSerializer( this.GetType() );
				TextWriter w = new StreamWriter(Engine.ConfigPath + "Listeners/" + conffilename + ".xml" );
				s.Serialize( w, this );
				w.Close();
			}
			catch(Exception ex)
			{
				Console.WriteLine("btEngine: TcpListener: Save failed.");
				Console.WriteLine(ex.Message);
								
				Console.WriteLine("Inner Exception: {0}", (ex.InnerException).Message);
				Console.WriteLine("Base Exception: {0}", (ex.GetBaseException()).Message);
				return false;
			}
			
			return true;
		}
	}
}
