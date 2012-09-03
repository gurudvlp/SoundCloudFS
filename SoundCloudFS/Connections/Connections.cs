// 
//  Connections.cs
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
	public class IncomingConnections
	{
		//	This class keeps track of the info for each incoming connection.
		//	Depending on some details I haven't thought about yet, this
		//	will need to bind an interface controller to itself so that it
		//	knows how to handle the connection properly.
		
		
		public string Protocol = "";
		public Interface TheInterface;
		
		//int connectiontype = -1;
		bool isactive = false;
		
	  	byte[] message = new byte[4096];
	  	int bytesRead;
		int clientloggedin = 0;
		string currentmsgin;
		//char[] delimiterChars = {' '};
		//char[] linedelimiters = {'\n'};
		//string inputbuffer = "";
		bool clientstillconnected = true;
				
		bool alreadypinged = false;
		long pingtimeforwebsite = 0;		
	
		int threadloops = 0;
		long lastmessagetime = Engine.TimeStamp();
		
		int turnssincemessage = 0;
		
		int connectionid = -1;
		
		TcpClient tcpClient;
		NetworkStream clientStream;
		
		public string RemoteIP
		{
			get
			{
				if(tcpClient == null) { return ""; }
				return tcpClient.Client.RemoteEndPoint.ToString();
			}
		}
		
		static ASCIIEncoding encoder = new ASCIIEncoding();
		
		public IncomingConnections(int tconnectionid)
		{
			connectionid = tconnectionid;
		}
		
		public static void AddIncomingConnection(object client, string protocol)
		{
			//
			//	This method will assign an incoming connection to an open
			//	connection object.  It will also tell that object what
			//	protocol is being represented so that it knows what interface
			//	to assign
			//
			
			for(int econnection = 0; econnection < Engine.Config.MaxDaemonConnections; econnection++)
			{
				try
				{
					if(Engine.Connections[econnection].IsActive() == false)
					{
						//	This connection is open, so we should sign it up for the new
						//	connection being made to the btEngine
						//Logging.Write("btEngine: Connections: Claiming a new connection (" + econnection.ToString() + ")");
						
						if(Engine.Connections == null) { Logging.Write("btEngine: Connections: Engine connection list is null."); }
						if(Engine.Connections != null && Engine.Connections[econnection] == null) { Logging.Write("btEngine: Connections: Connection[" + econnection.ToString() + "] is null."); }
						Engine.Connections[econnection].Claim(client, protocol);
						return;
					}
				}
				catch(Exception ex)
				{
					//	This connection isn't currently active/loaded
					Logging.Write("btEngine: Connections: Exception");
					Logging.Write(ex.Message);
					Logging.Write(ex.StackTrace);
					//Logging.Write(ex.GetBaseException().Message);
					//Logging.Write(ex.InnerException.Message);
					Logging.Write("----");
					//Logging.Write(Environment.StackTrace);
				}
			}
			
			//	No open connections were found.
			
			Logging.Write("No open connection spots available");
			
		}
		
		public void Claim(object client, string protocol)
		{
			//	This method is called when a client attempts to log in,
			//	and the IncomingConnection thing verified there was a
			//	slot left.
			
			//Logging.Write("btEngine: Connections: Claiming a connection.");
			tcpClient = new TcpClient();
			tcpClient = (TcpClient)client;
	 		clientStream = tcpClient.GetStream();
			//encoder = new ASCIIEncoding();
			
			
			isactive = true;
			clientloggedin = 0;
			clientstillconnected = true;
			lastmessagetime = Engine.TimeStamp();
			
			//	The protocol specifies what interface we need to attach to this
			//	connection object.  The interface takes care of the actual data
			//	parsing and manipulation
			
			this.Protocol = protocol.ToUpper();
			//	bind an interface heyah
			
			//Logging.Write("btEngine: Connections: Assinging interface for incoming (" + this.RemoteIP + ").");
			TheInterface = new scfsd();
			TheInterface.RemoteIP = this.RemoteIP;
		}
		
		public bool IsActive()
		{
			return isactive;
		}
		
		public bool Close()
		{
			
			tcpClient.Close();
			tcpClient = null;
			clientStream = null;
			isactive = false;
			turnssincemessage = 0;
			
			clientstillconnected = false;

			
			return true;
		}
		
		public void TakeTurn()
		{
			//	This void performs this connections turn of events.
			//	Originally this would have
			
			//Logging.Write("Taking turn..");
			
			if(IsActive() == false) { return; } // This connection isn't active
			if(TheInterface == null) {
				return;
			} //	??????
			
			
			//	Now this needs to check if there are any commands or anything.
			
			bytesRead = 0;
			threadloops++;
			turnssincemessage++;
			
			//Logging.Write("Setting up timestamp");
			
			if(Engine.TimeStamp() - lastmessagetime > 30)
			{
				//Logging.Write("Closing Connection, time: {0}, {1}, {2}", TimeStamp(), lastmessagetime, TimeStamp() - lastmessagetime);
				Close();
				
				return;
			}
			
			//	This needs to receive any data that is incoming, and then send it to the
			//	interface to take care of it.  Afterwards, it needs to check any outgoing
			//	data from the interface and send it out.
			
			if(clientStream.DataAvailable == true)
			{
				threadloops = 0;
				
				
				try
				{
					//blocks until a client sends a message
					//Logging.Write("Reading incoming message");
					bytesRead = clientStream.Read(message, 0, 4096);
					pingtimeforwebsite = Engine.TimeStamp();
					lastmessagetime = Engine.TimeStamp();						
				}
				catch
				{
					//a socket error has occured
					Logging.Write("Some sort of socket error has happened.");
					
					Close();
					return;
				}
	
				if (bytesRead == 0)
				{
					//the client has disconnected from the server
					//	we need to make that server be in
					//	logged off status
					//Logging.Write("Client Has disconnected.");
					Close();
					
					return;
					//break;
				}
				
				alreadypinged = false;
				
				currentmsgin = encoder.GetString(message, 0, bytesRead);
				//inputbuffer = inputbuffer + currentmsgin;

				TheInterface.ReceivedData(currentmsgin);
				turnssincemessage = 0;
			}
			
			TheInterface.TakeTurn();
			
			if(TheInterface.UseAsciiOutput && TheInterface.OutgoingBuffer != "")
			{
				//Logging.Write("Encoding and sending page");
				//Logging.Write(TheInterface.OutgoingBuffer);
				clientStream.Write(encoder.GetBytes(TheInterface.OutgoingBuffer), 0, encoder.GetBytes(TheInterface.OutgoingBuffer).Length);
				clientStream.Flush();
				TheInterface.OutgoingBuffer = "";
			}
			else if(TheInterface.UseAsciiOutput == false)
			{
				try
				{
					clientStream.Write(encoder.GetBytes(TheInterface.OutgoingBuffer), 0, encoder.GetBytes(TheInterface.OutgoingBuffer).Length);
					clientStream.Write(TheInterface.OutgoingByteBuffer, 0, TheInterface.OutgoingByteBuffer.Length);
					clientStream.Flush();
					
					TheInterface.OutgoingBuffer = "";
					TheInterface.OutgoingByteBuffer = null;
				}
				catch(Exception ex)
				{
					Logging.Write("btEngine: Connections: Binary output just failed!!");
					Logging.Write("\t" + ex.Message);
				}
			}
			
			if(TheInterface.TerminateAfterSend)
			{
				Close();	
			}
			
				
		}
		
		
	}
}

