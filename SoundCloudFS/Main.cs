using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using System.Threading;

namespace btEngine
{
	class Engine
	{
		public static string EngineName = "SoundCloudFS";
		public static string EngineVersion = "v0.12.09.03";
		public static SoundCloudFS.Config.Config Config = new SoundCloudFS.Config.Config();
		//public static bool DisplayDebug = true;
		public static Dictionary<string, string> Filters = new Dictionary<string, string>();
		//public static string MountPoint = "";
		//public static string ClientID = "c1d33421d52855340be6a6f3bfac146e";
		//public static string BaseSearchURL = "http://api.soundcloud.com/tracks?client_id=[CLIENTID][SEARCHPARAMETERS]";
		//public static SoundCloudFS.Track[] Tracks;
		//public static int QueryLimit = 20;
		//public static string FilenameFormat = "[TRACKTITLE]";
		public static string ConfigPath = "";
		public static SoundCloudFS.FileTree.Node[] FSNodes;
		public static Listeners[] Listener = new Listeners[1];
		public static IncomingConnections[] Connections;
		public static bool KeepRunning = true;
		
		public static void Main(string[] args)
		{
			//	Launch the daemon for controlling SoundCloudFS mounts.  The daemon listens for
			//	connections, and handles the organization of the filesystem.
			string SearchParameters = "";
			string FindUser = "";
			
			btEngine.Engine.Config.BaseSearchURL = btEngine.Engine.Config.BaseSearchURL.Replace("[CLIENTID]", btEngine.Engine.Config.ClientID);
			
			if(args.Length == 0)
			{
				Console.WriteLine(Engine.EngineName + " " + Engine.EngineVersion);
				Console.WriteLine("Copyright 2012, Brian Murphy");
				Console.WriteLine("www.gurudigitalsolutions.com");
				Console.WriteLine("");
				Console.WriteLine("ERROR: No arguments specified.");
				Console.WriteLine("");
				Console.WriteLine("USAGE:");
				Console.WriteLine("\tsoundcloudfs <mountpoint> [options]");
				//Console.WriteLine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "soundcloudfs"));
				
				
				Environment.Exit(0);
			}
			
			Engine.ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "soundcloudfs");
			if(!System.IO.Directory.Exists(Engine.ConfigPath))
			{
				try
				{
					System.IO.Directory.CreateDirectory(Engine.ConfigPath);
				}
				catch(Exception ex)
				{
					Console.WriteLine("Could not create the configuration directory.");
					Console.WriteLine("\t{0}", ex.Message);
					Engine.ConfigPath = "";
				}
			}
			else
			{
				//	Attempt to load the save configuration.
				string configfile = Path.Combine(Engine.ConfigPath, "config.xml");
				
				if(File.Exists(configfile))
				{
					try
					{
						XmlSerializer s = new XmlSerializer(typeof(SoundCloudFS.Config.Config));
						TextReader tr = new StreamReader(configfile);
						Engine.Config = (SoundCloudFS.Config.Config)s.Deserialize(tr);
						tr.Close();
					}
					catch(Exception ex)
					{
						Logging.Write("Could not load configuration file.");
						Logging.Write("\t" + ex.Message);
					}
				}
				else
				{
					Engine.Config.Save();
				}
			}
			
			if(!System.IO.Directory.Exists(Path.Combine(Engine.ConfigPath, "nodes")))
			{
				try
				{
					System.IO.Directory.CreateDirectory(Path.Combine(Engine.ConfigPath, "nodes"));
				}
				catch(Exception ex)
				{
					Logging.Write("Could not create FileNode directory.");
					Logging.Write("------------------------------------");
					Logging.Write(ex.Message);
					Logging.Write("------------------------------------");
				}
			}
			
			
			foreach(string earg in args)
			{
				if(Config.MountPoint == "") { Config.MountPoint = earg; }
				else
				{
					//	Mount point is the first argument, and it is already set.
					if(earg.IndexOf("=") > 0)
					{
						string[] splitat = new string[]{"="};
						string[] parts = earg.Split(splitat, StringSplitOptions.None);
						
						//	Check if this is an option for SoundCloudFS, the search or for FUSE
						if(parts[0].ToLower() == "filenameformat" || parts[0].ToLower() == "fnf")
						{
							Engine.Config.FilenameFormat = parts[1];
						}
						else if(parts[0].ToLower() == "byuser")
						{
							FindUser = parts[1];
						}
						else if(parts[0].ToLower() == "autosavenodes")
						{
							if(parts[1].ToLower() == "false"
							   || parts[1].ToLower() == "no"
							   || parts[1].ToLower() == "0")
							{
								Engine.Config.AutoSaveNodes = false;
							}
							else { Engine.Config.AutoSaveNodes = true; }
						}
						else if(parts[0].ToLower() == "decay")
						{
							if(parts[1].ToLower() == "nodecay")
							{
								Engine.Config.DecayTime = -1;
							}
							else
							{
								int dtime = 0;
								if(!Int32.TryParse(parts[1], out dtime))
								{
									Logging.Write("The decay time specified was invalid.");
								}
								else
								{
									if(dtime > 0) { Engine.Config.DecayTime = dtime; }
									else { Logging.Write("The decay time specified was invalid."); }
								}
							}
						}
						else if(parts[0].ToLower() == "mountuid"
						        || parts[0].ToLower() == "uid")
						{
							int tuid = 0;
							if(Int32.TryParse(parts[1], out tuid))
							{
								Engine.Config.UserID = tuid;
							}
						}
						else if(parts[0].ToLower() == "mountgid"
						        || parts[0].ToLower() == "gid")
						{
							int tgid = 0;
							if(Int32.TryParse(parts[1], out tgid))
							{
								Engine.Config.GroupID = tgid;
							}
						}
						else if(parts[0].ToLower() == "mountuser"
						        || parts[0].ToLower() == "uname")
						{
							Engine.Config.MountAsUser = parts[1];
						}
						else if(parts[0].ToLower() == "mountgroup"
						        || parts[0].ToLower() == "gname")
						{
							Engine.Config.MountAsGroup = parts[1];
						}
						else
						{
							Filters.Add(parts[0], parts[1]);
							Logging.Write(parts[0] + " = " + parts[1]);
							if(parts[0].ToLower() == "limit")
							{
								int limit = 0;
								if(Int32.TryParse(parts[1], out limit))
								{
									Engine.Config.QueryLimit = limit;
								}
							}
							else if(parts[0].ToLower() == "offset")
							{
								int limit = 0;
								if(Int32.TryParse(parts[1], out limit))
								{
									Engine.Config.QueryOffset = limit;
								}
							}
							else
							{
								SearchParameters = SearchParameters + "&" + parts[0] + "=" + parts[1];
							}
						}
						
					}
					else
					{
						if(earg.ToLower() == "nodecay")
						{
							Engine.Config.DecayTime = -1;
						}
						else if(earg.ToLower() == "allowother"
						        || earg.ToLower() == "allowothers"
						        || earg.ToLower() == "allow_other"
						        || earg.ToLower() == "allow_others")
						{
							Engine.Config.AllowOthers = true;
						}
						else if(earg.ToLower() == "noother"
						        || earg.ToLower() == "noothers"
						        || earg.ToLower() == "no_other"
						        || earg.ToLower() == "no_others")
						{
							Engine.Config.AllowOthers = false;
						}
					}
					
				}
			}
			
			
			SearchParameters = SearchParameters + "&" + "limit=" + Engine.Config.QueryLimit.ToString();
			SearchParameters = SearchParameters + "&" + "offset=" + Engine.Config.QueryOffset.ToString();
			
			if(FindUser != null && FindUser != "")
			{
				string FindUserURL = "http://api.soundcloud.com/users?client_id=" + Engine.Config.ClientID + "&q='" + FindUser + "'&limit=1";
				Scrapers.SoundCloudSearch FindUserScrape = new Scrapers.SoundCloudSearch();
				FindUserScrape.ScrapeURL = FindUserURL;
				
				if(FindUserScrape.TakeTurn())
				{
					string workwith = FindUserScrape.PageText;
					workwith = workwith.Substring(workwith.IndexOf("<user>"));
					workwith = workwith.Replace("<user>", "");
					string[] splitat = new string[]{"</user>"};
					string[] parts = workwith.Split(splitat, StringSplitOptions.None);
					workwith = parts[0].Trim();
					
					Logging.Write("---");
					Logging.Write(workwith);
					Logging.Write("---");
					
					if(workwith.Length > 10)
					{
						System.IO.File.WriteAllText("tmpresult.xml", "<user>" + workwith + "</user>");
						XmlSerializer s = new XmlSerializer(typeof(SoundCloudFS.User));
						TextReader tr = new StreamReader("tmpresult.xml");
						SoundCloudFS.User tmpuser = new SoundCloudFS.User();
						tmpuser = (SoundCloudFS.User)s.Deserialize(tr);
						tr.Close();
						System.IO.File.Delete("tmpresult.xml");
						
						SearchParameters = SearchParameters + "&user_id=" + tmpuser.ID.ToString();
						Logging.Write("SearchParams: " + SearchParameters);
					}
				}
			}
			
			
			//	Build a temporary FileTree node system thing.
			/*Engine.FSNodes = new SoundCloudFS.FileTree.Node[Engine.Config.MaxFSNodes];
			Engine.FSNodes[0] = new SoundCloudFS.FileTree.Node(0, "/");
			Engine.FSNodes[0].NodeType = SoundCloudFS.FileTree.Node.NodeTypeTree;
			//int drumstepnode = Engine.FSNodes[0].AddSubNode("drumstep");
			//Engine.FSNodes[drumstepnode].NodeType = SoundCloudFS.FileTree.Node.NodeTypeSearch;
			//Engine.FSNodes[drumstepnode].SearchParameters = SearchParameters;
			
			int psynode = Engine.FSNodes[0].AddSubNode("psytrance");
			Engine.FSNodes[psynode].NodeType = SoundCloudFS.FileTree.Node.NodeTypeSearch;
			
			int countrynode = Engine.FSNodes[0].AddSubNode("country");
			Engine.FSNodes[countrynode].NodeType = SoundCloudFS.FileTree.Node.NodeTypeSearch;
			Engine.FSNodes[countrynode].SearchParameters = SearchParameters;
			
			SoundCloudFS.FileTree.Node.SaveNodes();
			*/
			
			
			SoundCloudFS.FileTree.Node.LoadNodes();
			
			Listener[0] = new Listeners(Engine.Config.DaemonPort, "SoundCloudFS Daemon", "scfsd");
			Connections = new IncomingConnections[Config.MaxDaemonConnections];
			for(int ec = 0; ec < Config.MaxDaemonConnections; ec++)
			{
				Connections[ec] = new IncomingConnections(ec);
			}
			
			Thread daemonthread = new Thread(DaemonHeart);
			daemonthread.Start();
			
			using (Mono.Fuse.SoundCloud.FS fs = new Mono.Fuse.SoundCloud.FS ())
			{
				
				
				/*string[] unhandled = fs.ParseFuseArguments (args);
				foreach (string key in fs.FuseOptions.Keys) {
					Console.WriteLine ("Option: {0}={1}", key, fs.FuseOptions [key]);
				}
				if (!fs.ParseArguments (unhandled))
					return;*/
				//fs.MountAt ("path" /* , args? */);
				
				//fs.FuseOptions.Add
				
				
				if(Engine.Config.AllowOthers)
				{
					string[] fuseopts = new string[]{"-o", "allow_other"};
					string[] unhandled = fs.ParseFuseArguments(fuseopts);
				}
				
				if(Engine.Config.MountPoint == null || Engine.Config.MountPoint == "") { fs.MountPoint = args[0]; }
				else { fs.MountPoint = Engine.Config.MountPoint; }
				if(fs.MountPoint.LastIndexOf("/") == fs.MountPoint.Length - 1) { fs.MountPoint = fs.MountPoint.Substring(0, fs.MountPoint.Length - 1); }
				fs.Start ();
				
			}
			
			
		}
		
		public static void DaemonHeart()
		{
			int caught = 0;

			long starttime = (TimeStamp() * 1000) + System.DateTime.Now.Millisecond;
			long endtime = 0;
			long cycletime = 0;
			
			int theminute = 0;
			int lastminute = 0;
			
			Listener[0].Listen();
			
			while(btEngine.Engine.KeepRunning)
			{
				lastminute = theminute;
				theminute = System.DateTime.Now.Minute;
				
				starttime = (TimeStamp() * 1000) + System.DateTime.Now.Millisecond;
				
				for(int ec = 0; ec < Config.MaxDaemonConnections; ec++)
				{
					if(Connections[ec] != null)
					{
						if(Connections[ec].IsActive())
						{
							//Logging.Write("Connection " + ec.ToString() + " is active.");
							Connections[ec].TakeTurn();
						}
					}
				}
				
				//Thread.Sleep(0);
				
				endtime = (TimeStamp() * 1000) + System.DateTime.Now.Millisecond;
				cycletime = endtime - starttime;
				//totalcycletime = totalcycletime + cycletime;
				//totalcycles++;
				
				Thread.Sleep(1);
				//	End of Daemon Heart Loop
			}
		}
		
		public static long TimeStamp()
		{
			/*
			 * 	Legacy btEngine TimeStamp
			 * int year = System.DateTime.Now.Year;
			int dayofyear = System.DateTime.Now.DayOfYear;
			int min = System.DateTime.Now.Minute;
			int hour = System.DateTime.Now.Hour;
			int second = System.DateTime.Now.Second;
			
			long workingtime = 0;
			
			workingtime = (year -2009) * 365 * 24 * 60 * 60;
			workingtime = workingtime + (dayofyear * 24 * 60 * 60);
			workingtime = workingtime + (hour * 60 * 60);
			workingtime = workingtime + (min * 60) + second;
			
			return workingtime;
			
			*/
			
			return (long)UnixTimeStamp();
		}
		
		public static double UnixTimeStamp()
		{
		    //TimeSpan unix_time = (System.DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
		    //return unix_time.TotalSeconds;
			return UnixTimeStamp(System.DateTime.UtcNow);
		}
		
		public static double UnixTimeStamp(System.DateTime datetime)
		{
			TimeSpan unix_time = (datetime - new DateTime(1970, 1, 1, 0, 0, 0));
			return unix_time.TotalSeconds;
		}

		public static DateTime UnixTimeStampToDateTime(long unixTimeStamp )
		{
		    // Unix timestamp is seconds past epoch
		    System.DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0);
		    dtDateTime = dtDateTime.AddSeconds((double)unixTimeStamp ).ToLocalTime();
		    return dtDateTime;
		}

		
		public static string MD5(string password)
		{
			System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
			byte[] bs = System.Text.Encoding.UTF8.GetBytes(password);
			bs = x.ComputeHash(bs);
			System.Text.StringBuilder s = new System.Text.StringBuilder();
			foreach (byte b in bs)
			{
			   s.Append(b.ToString("x2").ToLower());
			}
			return s.ToString();
		}
	}
}
