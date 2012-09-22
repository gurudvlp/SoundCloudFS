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
				else if(CurrentCommand == "exit")
				{
					
					base.TerminateAfterSend = true;
				}
				else if(CurrentCommand == "quit")
				{
					base.OutgoingBuffer = "BYE\n";
					base.TerminateAfterSend = true;
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
				else if(CurrentCommand == "lsgenres")
				{
					int nodeid = SoundCloudFS.FileTree.Node.FindNode(CurrentDir);
					if(Engine.FSNodes[nodeid].NodeType == SoundCloudFS.FileTree.Node.NodeTypeTree)
					{
						base.OutgoingBuffer = "FAIL Node is a tree, not a search.\n";
					}
					else
					{
						if(Engine.FSNodes[nodeid].QueryGenres == null)
						{
							base.OutgoingBuffer = "FAIL Genre list is null.\n";
						}
						else
						{
							for(int eg = 0; eg < Engine.FSNodes[nodeid].QueryGenres.Length; eg++)
							{
								base.OutgoingBuffer = base.OutgoingBuffer + Engine.FSNodes[nodeid].QueryGenres[eg] + "\n";
							}
							base.OutgoingBuffer = base.OutgoingBuffer + "\n";
						}
					}
				}
				else if(CurrentCommand == "lslimit")
				{
					int nodeid = SoundCloudFS.FileTree.Node.FindNode(CurrentDir);
					if(Engine.FSNodes[nodeid].NodeType == SoundCloudFS.FileTree.Node.NodeTypeTree)
					{
						base.OutgoingBuffer = "FAIL Node is a tree, not a search.\n";
					}
					else
					{
						base.OutgoingBuffer = Engine.FSNodes[nodeid].QueryLimit.ToString() + "\n";
					}
				}
				else if(CurrentCommand == "lsoffset")
				{
					int nodeid = SoundCloudFS.FileTree.Node.FindNode(CurrentDir);
					if(Engine.FSNodes[nodeid].NodeType == SoundCloudFS.FileTree.Node.NodeTypeTree)
					{
						base.OutgoingBuffer = "FAIL Node is a tree, not a search.\n";
					}
					else
					{
						base.OutgoingBuffer = Engine.FSNodes[nodeid].QueryOffset.ToString() + "\n";
					}
				}
				else if(CurrentCommand == "nodetype")
				{
					int nodeid = SoundCloudFS.FileTree.Node.FindNode(CurrentDir);
					if(Engine.FSNodes[nodeid] == SoundCloudFS.FileTree.Node.NodeTypeSearch) { base.OutgoingBuffer = "search\n"; }
					else { base.OutgoingBuffer = "tree"; }
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
						
						if(cmdparts[1] == "genres")
						{
							string genlist = CurrentCommand.Replace("query genres ", "");
							
							char[] spat = new char[]{','};
							string[] genres = genlist.Split(spat);
							for(int ep = 0; ep < genres.Length; ep++)
							{
								genres[ep] = genres[ep].Trim();
								if(genres[ep].Contains(" "))
								{
									genres[ep] = genres[ep].Substring(0, genres[ep].IndexOf(" ") - 1);
								}
								if(genres[ep].Contains("'"))
								{
									genres[ep] = genres[ep].Substring(0, genres[ep].IndexOf("'") - 1);
								}
								
							}
							
							Engine.FSNodes[nodeid].QueryGenres = genres;
						}
						else if(cmdparts[1] == "limit")
						{
							Engine.FSNodes[nodeid].QueryLimit = Int32.Parse(cmdparts[2]);
						}
						else if(cmdparts[1] == "offset")
						{
							Engine.FSNodes[nodeid].QueryOffset = Int32.Parse(cmdparts[2]);
						}
						
						
						if(Engine.Config.AutoSaveNodes)
						{
							SoundCloudFS.FileTree.Node.SaveNodes();
						}
						
						base.OutgoingBuffer = "OK\n";
					}
					else if(cmdparts[0] == "mkdir")
					{
						string dirname = cmdparts[1];
						int nodeid = SoundCloudFS.FileTree.Node.FindNode(CurrentDir);
						
						int newnode = Engine.FSNodes[nodeid].AddSubNode(dirname);
						if(newnode < 0) { base.OutgoingBuffer = "FAIL New node failed to be created.\n"; }
						else
						{
							Engine.FSNodes[nodeid].NodeType = SoundCloudFS.FileTree.Node.NodeTypeTree;
							base.OutgoingBuffer = "OK\n";
						}
						
					}
					else if(cmdparts[0] == "rmdir")
					{
						string dirname = cmdparts[1];
						int nodeid = SoundCloudFS.FileTree.Node.FindNode(CurrentDir + "/" + dirname);
						int parentnode = SoundCloudFS.FileTree.Node.FindNode(CurrentDir);
						
						if(nodeid < 0) { base.OutgoingBuffer = "FAIL Node not found.\n"; }
						else
						{
							if(SoundCloudFS.FileTree.Node.RemoveNode(nodeid)
							&& Engine.FSNodes[parentnode].UnlinkSubNode(nodeid))
							{
								base.OutgoingBuffer = "OK\n";
							}
							else
							{
								base.OutgoingBuffer = "FAIL Node was not removed.";
							}
						}
					}
					else if(cmdparts[0] == "autosavenodes")
					{
						if(cmdparts[1] == "true"
						   || cmdparts[1] == "1"
						   || cmdparts[1] == "yes")
						{
							Engine.Config.AutoSaveNodes = true;
							base.OutgoingBuffer = "OK\n";
						}
						else if(cmdparts[1] == "false"
						        || cmdparts[1] == "0"
						        || cmdparts[1] == "no")
						{
							Engine.Config.AutoSaveNodes = false;
							base.OutgoingBuffer = "OK\n";
						}
						else
						{
							base.OutgoingBuffer = "FAIL Command expected as autosavenodes true/false.\n";
						}
					}
					else if(cmdparts[0] == "decay")
					{
						string decaytime = cmdparts[1];
						if(decaytime == "nodecay") { decaytime = "-1"; }
						
						int dtime = 0;
						if(Int32.TryParse(decaytime, out dtime))
						{
							if(dtime < 0) { dtime = -1; }
							Engine.Config.DecayTime = dtime;
							base.OutgoingBuffer = "OK\n";
						}
						else
						{
							base.OutgoingBuffer = "FAIL Expected an integer or nodecay.\n";
						}
					}
				}
			}
			return true;
		}
		
	}
}

