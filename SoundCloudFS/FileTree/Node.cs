// 
//  Node.cs
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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using btEngine;

namespace SoundCloudFS.FileTree
{
	[XmlRoot("TreeNode")]
	public class Node
	{
		[XmlElement("ID")] public int ID = 0;
		[XmlElement("Name")] public string Name = "/";
		[XmlElement("NodeType")] public int NodeType = SoundCloudFS.FileTree.Node.NodeTypeTree;
		[XmlElement("SearchParameters")] public string SearchParameters = "";
		[XmlElement("SubNodes")] public int[] SubNodes = new int[32];
		
		[XmlIgnore()] private int userid = -1;
		[XmlIgnore()] private int groupid = -1;
		[XmlElement("MountAsUser")] public string MountAsUser = "";
		[XmlElement("MountAsGroup")] public string MountAsGroup = "";
		
		[XmlIgnoreAttribute()] public SoundCloudFS.Track[] Tracks;
		[XmlIgnoreAttribute()] public bool HasSearched = false;
		[XmlElement("QueryLimit")] public int QueryLimit = Engine.Config.QueryLimit;
		[XmlElement("QueryOffset")] public int QueryOffset = Engine.Config.QueryOffset;
		[XmlElement("QueryGenres")] public string[] QueryGenres = new string[1];
		
		[XmlIgnoreAttribute()] public static int NodeTypeTree = 0;
		[XmlIgnoreAttribute()] public static int NodeTypeSearch = 1;
		
		public Node ()
		{
			for(int enode = 0; enode < SubNodes.Length; enode++)
			{
				SubNodes[enode] = -1;
			}
		}
		
		public Node(int id, string nodename)
		{
			for(int enode = 0; enode < SubNodes.Length; enode++)
			{
				SubNodes[enode] = -1;
			}
			
			this.ID = id;
			this.Name = nodename;
		}
		
		public int AddSubNode(string nodename)
		{
			if(this.NodeType != NodeTypeTree) { return -1; }
			
			int usenode = -1;
			int usesubnode = NextSubNode();
			if(usesubnode < 0) { return -1; }
			
			for(int enode = 0; enode < btEngine.Engine.FSNodes.Length; enode++)
			{
				if(btEngine.Engine.FSNodes[enode] == null) { usenode = enode; break; }
			}
			
			if(usenode < 0) { return usenode; }
			
			Engine.FSNodes[usenode] = new Node(usenode, nodename);
			SubNodes[usesubnode] = usenode;
			
			return usenode;
		}
		
		public static int FindNode(string nodename)
		{
			int tnode = -1;
			string npath = nodename;
			//Logging.Write("FindNode: " + nodename);
			if(npath.Substring(0, 1) == "/") { npath = npath.Substring(1); }
			
			if(npath.Length > 0 && npath.Substring(npath.Length - 1) == "/") { npath = npath.Substring(0, npath.Length - 1); }
			
			
			if(nodename == "/") { return tnode = 0; }
			else { tnode = Engine.FSNodes[0].FindNodeInSubNodes(npath); }
			
			return tnode;
		}
		
		public int FindNodeInSubNodes(string nodepath)
		{
			//Logging.Write("FindNodeInSubNodes: " + this.ID.ToString() + " :: " + nodepath);
			char[] splitat = new char[]{'/'};
			bool gofurther = true;
			
			if(!nodepath.Contains("/"))
			{
				gofurther = false;
				//if(this.Name != nodepath) { return -1; }
				//return this.ID;
			}
			
			string[] dirnames;
			//if(gofurther) { dirnames = nodepath.Split(splitat, 2); }
			
			for(int esubn = 0; esubn < SubNodes.Length; esubn++)
			{
				if(SubNodes[esubn] == -1) { return -1; }
				
				if(gofurther)
				{
					dirnames = nodepath.Split(splitat, 2);
					if(Engine.FSNodes[SubNodes[esubn]].Name == dirnames[0])
					{
						return Engine.FSNodes[SubNodes[esubn]].FindNodeInSubNodes(dirnames[1]);
					}
				}
				else
				{
					if(Engine.FSNodes[SubNodes[esubn]].Name == nodepath)
					{
						return SubNodes[esubn];
					}
				}
			}
			
			return -1;
		}
		
		public int NextSubNode()
		{
			for(int esubn = 0; esubn < SubNodes.Length; esubn++)
			{
				if(SubNodes[esubn] < 0) { return esubn; }
			}
			
			return -1;
		}
		
		public bool RunSearch()
		{
			BuildSearchParameters();
			Logging.Write("RunSearch with Parms: " + SearchParameters);
			string SearchURL = btEngine.Engine.Config.BaseSearchURL.Replace("[SEARCHPARAMETERS]", SearchParameters);
			Logging.Write("SearchURL: " + SearchURL);
			
			btEngine.Scrapers.SoundCloudSearch SearchScrape = new btEngine.Scrapers.SoundCloudSearch();
			SearchScrape.ScrapeURL = SearchURL;
			//Logging.Write("Scrape/SearchURL: " + SearchScrape.ScrapeURL);
			SearchScrape.Name = "SoundCloudScraper";
			SearchScrape.SourceName = "SoundCloud";
			
			if(SearchScrape.TakeTurn())
			{
				//Console.Write(SearchScrape.PageText);
				string workwith = SearchScrape.PageText;
				workwith = workwith.Substring(workwith.IndexOf("<track>"));
				workwith = workwith.Replace("</tracks>", "");
				workwith = workwith.Trim();
				
				string[] parts;
				string[] splitat = new string[]{"</track>"};
				parts = workwith.Split(splitat, StringSplitOptions.None);
				Tracks = new SoundCloudFS.Track[parts.Length];
				for(int etrack = 0; etrack < parts.Length; etrack++)
				{
					if(parts[etrack].Trim().Length > 10)
					{
						System.IO.File.WriteAllText("tmpresult.xml", parts[etrack] + "</track>");
						XmlSerializer s = new XmlSerializer(typeof(SoundCloudFS.Track));
						TextReader tr = new StreamReader("tmpresult.xml");
						Tracks[etrack] = (SoundCloudFS.Track)s.Deserialize(tr);
						tr.Close();
						System.IO.File.Delete("tmpresult.xml");
						
						if(Tracks[etrack].StreamURL == null || Tracks[etrack].StreamURL == "")
						{
							Logging.Write("A track without a stream URL was found!");
							Logging.Write("It is " + Tracks[etrack].Title + " by " + Tracks[etrack].UserID.ToString());
							Tracks[etrack] = null;
						}
						else
						{
							Tracks[etrack].CalculateFilesize();
						}
					}
				}
				
			}
			else
			{
				Logging.Write("Scraping failed.");
				return false;
			}
			
			return true;
		}
		
		public static string ParseNodeName(string nodepath)
		{
			if(nodepath.EndsWith(".mp3"))
			{
				//	Looking for status on a file, not a path node
				//return nodepath.Substring(path.LastIndexOf('/') + 1);
				string npath = nodepath.Substring(0, nodepath.LastIndexOf('/'));
				if(npath.EndsWith("/")) { npath = npath.Substring(0, npath.Length - 1); }
				return npath;
			}
			
			return nodepath;
		}
		
		public static string ParseTrackFilename(string nodepath)
		{
			if(nodepath.EndsWith(".mp3"))
			{
				//	Looking for status on a file, not a path node
				return nodepath.Substring(nodepath.LastIndexOf('/') + 1);
				//path = path.Substring(0, path.LastIndexOf('/'));
			}
			
			return "";
		}
		
		public static bool SaveNodes()
		{
			if(btEngine.Engine.ConfigPath == "")
			{
				Logging.Write("Could not save the file nodes.  There is no configuration path.");
				return false;
			}
			
			string nodepath = System.IO.Path.Combine(Engine.ConfigPath, "nodes");
			if(!System.IO.Directory.Exists(nodepath))
			{
				Logging.Write("Could not save the file nodes.  File node directory does not exist.");
				return false;
			}
			
			//string configfile = System.IO.Path.Combine(Engine.ConfigPath, "nodes.xml");
			
			for(int eno = 0; eno < Engine.FSNodes.Length; eno++)
			{
				string tnfile = System.IO.Path.Combine(nodepath, eno.ToString() + ".xml");
				if(Engine.FSNodes[eno] != null)
				{
					try
					{
						XmlSerializer s = new XmlSerializer( typeof(SoundCloudFS.FileTree.Node) );
						TextWriter w = new StreamWriter( @tnfile);
						s.Serialize( w, Engine.FSNodes[eno] );
						w.Close();
					}
					catch(Exception ex)
					{
						Logging.Write("Error saving node.");
						Logging.Write("\t" + ex.Message);
						
						//return false;
					}
				}
			}
			
			return true;
		}
		
		public static bool LoadNodes()
		{
			if(btEngine.Engine.ConfigPath == "")
			{
				Logging.Write("Could not load the file nodes.  There is no configuration path.");
				return false;
			}
			
			string nodepath = System.IO.Path.Combine(Engine.ConfigPath, "nodes");
			if(!System.IO.Directory.Exists(nodepath))
			{
				Logging.Write("Could not load the file nodes.  File node directory does not exist.");
				return false;
			}
			
			Engine.FSNodes = new Node[Engine.Config.MaxFSNodes];
			Engine.FSNodes[0] = new Node();
			Engine.FSNodes[0].NodeType = SoundCloudFS.FileTree.Node.NodeTypeTree;
			Engine.FSNodes[0].Name = "/";
			
			
			for(int eno = 0; eno < Engine.FSNodes.Length; eno++)
			{
				string nodefile = Path.Combine(nodepath, eno.ToString() + ".xml");
				
				if(File.Exists(nodefile))
				{
					XmlSerializer s = new XmlSerializer(typeof(SoundCloudFS.FileTree.Node));
					TextReader tr = new StreamReader(nodefile);
					Engine.FSNodes[eno] = new Node();
					Engine.FSNodes[eno] = (SoundCloudFS.FileTree.Node)s.Deserialize(tr);
					tr.Close();
				}
			}
			
			return true;
		}
		
		public void BuildSearchParameters()
		{
			string sparm = "";
			if(this.QueryGenres != null)
			{
				sparm = sparm + "&genres='";
				for(int eg = 0; eg < this.QueryGenres.Length; eg++)
				{
					sparm = sparm + this.QueryGenres[eg] + ",";
				}
				sparm = sparm.Substring(0, sparm.Length - 1);
				sparm = sparm + "'";
			}
			
			sparm = sparm + "&limit=" + this.QueryLimit.ToString() + "&offset=" + this.QueryOffset.ToString();
			this.SearchParameters = sparm;
		}
	}
}

