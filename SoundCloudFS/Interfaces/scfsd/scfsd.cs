// 
//  scfsd.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using System.Xml;
using System.Xml.Serialization;


namespace btEngine
{
	public class scfsd : Interface
	{
		public string CurrentCommand = "";
		public string CurrentDir = "/";
		
		public scfsd ()
		{
		}
		
		public override bool Run (string commandstring)
		{
			throw new NotImplementedException ();
		}
		
		public override bool TakeTurn ()
		{
			base.TerminateAfterSend = false;
			CurrentCommand = "";
			
			if(base.IncomingBuffer.Contains("\n"))
			{
				CurrentCommand = base.IncomingBuffer.Substring(0, base.IncomingBuffer.IndexOf("\n"));
				CurrentCommand = CurrentCommand.Replace("\n", "");
				base.IncomingBuffer = base.IncomingBuffer.Substring(base.IncomingBuffer.IndexOf("\n") + 1);
				
			}
			
			CurrentCommand = CurrentCommand.Trim();
			
			if(CurrentCommand != "")
			{
				Logging.Write("scfsd: CurrentCommand: " + CurrentCommand);
				if(CurrentCommand == "killdaemon" || CurrentCommand == "killdemon")
				{
					Logging.Write("Received the 'killdaemon' command.  Shutting down...");
					Engine.KeepRunning = false; 
				}
				else if(CurrentCommand == "pwd")
				{
					base.OutgoingBuffer = this.CurrentDir + "\n";
				}
				else if(CurrentCommand == "version")
				{
					base.OutgoingBuffer = Engine.EngineName + " " + Engine.EngineVersion + "\n";
				}
				else if(CurrentCommand == "savenodes")
				{
					if(SoundCloudFS.FileTree.Node.SaveNodes()) { base.OutgoingBuffer = "OK\n"; }
					else { base.OutgoingBuffer = "FAIL Path Nodes failed to save."; }
				}
				else if(CurrentCommand == "ls")
				{
					int nodeid = SoundCloudFS.FileTree.Node.FindNode(CurrentDir);
					
					if(Engine.FSNodes[nodeid].NodeType == SoundCloudFS.FileTree.Node.NodeTypeTree)
					{
						for(int esn = 0; esn < Engine.FSNodes[nodeid].SubNodes.Length; esn++)
						{
							if(Engine.FSNodes[nodeid].SubNodes[esn] > -1)
							{
								base.OutgoingBuffer = base.OutgoingBuffer + "DIR:" + Engine.FSNodes[Engine.FSNodes[nodeid].SubNodes[esn]].Name + "\n";
							}
						}
					}
					else
					{
						if(Engine.FSNodes[nodeid].Tracks != null)
						{
							for(int et = 0; et < Engine.FSNodes[nodeid].Tracks.Length; et++)
							{
								if(Engine.FSNodes[nodeid].Tracks[et] != null)
								{
									base.OutgoingBuffer = base.OutgoingBuffer + "FILE:" + Engine.FSNodes[nodeid].Tracks[et].Filename + "\n";
								}
							}
						}
					}
					
					base.OutgoingBuffer = base.OutgoingBuffer + "\n";
				}
				
				if(CurrentCommand.Contains(" "))
				{
					char[] splitat = new char[]{' '};
					string[] cmdparts = CurrentCommand.Split(splitat);
					
					if(cmdparts[0] == "cd")
					{
						string newpath = CurrentDir;
						if(!newpath.EndsWith("/")) { newpath = newpath + "/"; }
						newpath = newpath + cmdparts[1];
						
						int nodeid = SoundCloudFS.FileTree.Node.FindNode(newpath);
						
						if(nodeid > 0)
						{
							this.CurrentDir = newpath;
							
							if(Engine.FSNodes[nodeid].NodeType == SoundCloudFS.FileTree.Node.NodeTypeSearch)
							{
								if(Engine.FSNodes[nodeid].HasSearched == false)
								{
									Engine.FSNodes[nodeid].RunSearch();
									Engine.FSNodes[nodeid].HasSearched = true;
								}
							}
							base.OutgoingBuffer = "OK\n";
						}
						else
						{
							base.OutgoingBuffer = "FAIL Path not found (" + newpath + ")\n";
						}
					}
					else if(cmdparts[0] == "query")
					{
						int nodeid = SoundCloudFS.FileTree.Node.FindNode(CurrentDir);
						Engine.FSNodes[nodeid].NodeType = SoundCloudFS.FileTree.Node.NodeTypeSearch;
						Engine.FSNodes[nodeid].HasSearched = false;
						
						string searchp = "";
						bool haslimit = false;
						bool hasoffset = false;
						
						char[] spat = new char[]{'='};
						for(int ep = 1; ep < cmdparts.Length; ep++)
						{
							if(cmdparts[ep].Contains("="))
							{
								string[] eqprts = cmdparts[ep].Split(spat, 2);
								searchp = searchp + "&" + eqprts[0] + "=" + eqprts[1];
								if(eqprts[0] == "limit") { haslimit = true; }
								if(eqprts[0] == "offset") { hasoffset = true; }
							}
						}
						
						if(!haslimit) { searchp = searchp + "&limit=" + Engine.Config.QueryLimit.ToString(); }
						if(!hasoffset) { searchp = searchp + "&offset=" + Engine.Config.QueryOffset.ToString(); }
						
						Engine.FSNodes[nodeid].SearchParameters = searchp;
						
						if(Engine.Config.AutoSaveNodes)
						{
							SoundCloudFS.FileTree.Node.SaveNodes();
						}
						
						base.OutgoingBuffer = "OK\n";
					}
				}
			}
			return true;
		}
		
	}
}

