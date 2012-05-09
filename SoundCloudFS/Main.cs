using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.IO;

namespace btEngine
{
	class Engine
	{
		public static string EngineName = "SoundCloudFS";
		public static string EngineVersion = "v0.12.02.07";
		public static SoundCloudFS.Config Config = new SoundCloudFS.Config();
		//public static bool DisplayDebug = true;
		public static Dictionary<string, string> Filters = new Dictionary<string, string>();
		//public static string MountPoint = "";
		//public static string ClientID = "c1d33421d52855340be6a6f3bfac146e";
		//public static string BaseSearchURL = "http://api.soundcloud.com/tracks?client_id=[CLIENTID][SEARCHPARAMETERS]";
		public static SoundCloudFS.Track[] Tracks;
		//public static int QueryLimit = 20;
		//public static string FilenameFormat = "[TRACKTITLE]";
		public static string ConfigPath = "";
		
		public static void Main(string[] args)
		{
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
						XmlSerializer s = new XmlSerializer(typeof(SoundCloudFS.Config));
						TextReader tr = new StreamReader(configfile);
						Engine.Config = (SoundCloudFS.Config)s.Deserialize(tr);
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
					}
				}
			}
			
			using (Mono.Fuse.SoundCloud.SoundCloudFS fs = new Mono.Fuse.SoundCloud.SoundCloudFS ())
			{
				
				string SearchURL = btEngine.Engine.Config.BaseSearchURL.Replace("[SEARCHPARAMETERS]", SearchParameters);
				Scrapers.SoundCloudSearch SearchScrape = new Scrapers.SoundCloudSearch();
				SearchScrape.ScrapeURL = SearchURL;
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
							
							Tracks[etrack].CalculateFilesize();
						}
					}
					
				}
				else
				{
					Logging.Write("Scraping failed.");
					Environment.Exit(0);
				}
				
				/*string[] unhandled = fs.ParseFuseArguments (args);
				foreach (string key in fs.FuseOptions.Keys) {
					Console.WriteLine ("Option: {0}={1}", key, fs.FuseOptions [key]);
				}
				if (!fs.ParseArguments (unhandled))
					return;*/
				//fs.MountAt ("path" /* , args? */);
				
				//fs.FuseOptions.Add
				if(Engine.Config.MountPoint == null || Engine.Config.MountPoint == "") { fs.MountPoint = args[0]; }
				else { fs.MountPoint = Engine.Config.MountPoint; }
				if(fs.MountPoint.LastIndexOf("/") == fs.MountPoint.Length - 1) { fs.MountPoint = fs.MountPoint.Substring(0, fs.MountPoint.Length - 1); }
				fs.Start ();
				
			}
		}
		
		
	}
}
