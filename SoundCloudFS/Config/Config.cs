
using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using btEngine;

namespace SoundCloudFS.Config
{

	[XmlRoot("Configuration")]
	public class Config
	{
		[XmlElement("DisplayDebug")] public bool DisplayDebug = true;
		
		[XmlElement("SearchFilters")] public string[] Filters;
		//public Dictionary<string, string> Filters = new Dictionary<string, string>();
		[XmlElement("MountPoint")] public string MountPoint = "";
		[XmlElement("ClientID")] public string ClientID = "c1d33421d52855340be6a6f3bfac146e";
		[XmlElement("BaseSearchURL")] public string BaseSearchURL = "http://api.soundcloud.com/tracks?client_id=[CLIENTID][SEARCHPARAMETERS]";
		[XmlElement("QueryLimit")] public int QueryLimit = 20;
		[XmlElement("QueryOffset")] public int QueryOffset = 0;
		[XmlElement("FilenameFormat")] public string FilenameFormat = "[TRACKTITLE]";
		[XmlElement("DecayTime")] public int DecayTime = 1800;
		
		[XmlIgnore()] private int userid = -1;
		[XmlIgnore()] private int groupid = -1;
		[XmlElement("MountAsUser")] public string MountAsUser = "";
		[XmlElement("MountAsGroup")] public string MountAsGroup = "";
		
		[XmlElement("AllowOthers")] public bool AllowOthers = true;
		[XmlElement("MaxFSNodes")] public int MaxFSNodes = 256;
		[XmlElement("AutoSaveNodes")] public bool AutoSaveNodes = true;
		
		[XmlElement("MaxDaemonConnections")] public int MaxDaemonConnections = 100;
		[XmlElement("DaemonPort")] public int DaemonPort = 6034;
		
		public Config ()
		{
		}
		
		public bool Save()
		{
			if(btEngine.Engine.ConfigPath == "")
			{
				Logging.Write("Could not save the configuration.  There is no configuration path.");
				return false;
			}
			
			string configfile = System.IO.Path.Combine(Engine.ConfigPath, "config.xml");
			try
			{
				XmlSerializer s = new XmlSerializer( typeof(SoundCloudFS.Config.Config) );
				TextWriter w = new StreamWriter( @configfile);
				s.Serialize( w, this );
				w.Close();
			}
			catch(Exception ex)
			{
				Logging.Write("Error saving configuration.");
				Logging.Write("\t" + ex.Message);
				
				return false;
			}
			
			return true;
		}
		
		[XmlElement("MountAsUID")]
		public int UserID
		{
			get
			{
				//Logging.Write("Getting User ID");
				if(userid < 0)
				{
					if(this.MountAsUser != null && this.MountAsUser != "")
					{
						try
						{
							userid = Int32.Parse(RunCommand("id", "-u " + this.MountAsUser, true));
						}
						catch(Exception ex)
						{
							Logging.Write("Failed to parse the supplied username.");
						}
					}
					else
					{
						try
						{
							userid = Int32.Parse(RunCommand("id", "-u", true));
							//Logging.Write("UID determined to be " + userid.ToString());
						}
						catch(Exception ex)
						{
							Logging.Write("Failed to parse your uid on this system.");
						}
					}
				}
				
				return userid;
			}
			set
			{
				userid = value;
			}
		}
		
		[XmlElement("MountAsGID")]
		public int GroupID
		{
			get
			{
				if(groupid < 0)
				{
					if(this.MountAsGroup != null && this.MountAsGroup != "")
					{
						try
						{
							//groupid = Int32.Parse(RunCommand("id", "-g " + this.MountAsGroup, true));
							groupid = Int32.Parse(RunCommand("getent", "group " + this.MountAsGroup + " | cut -d: -f3", true));
						}
						catch(Exception ex)
						{
							Logging.Write("Failed to parse the supplied group name.");
						}
					}
					else
					{
						try
						{
							groupid = Int32.Parse(RunCommand("id", "-g", true));
							//Logging.Write("GID determined to be " + groupid.ToString());
						}
						catch(Exception ex)
						{
							Logging.Write("Failed to parse your gid on this system.");
						}
					}
				}
				
				return groupid;
			}
			set
			{
				groupid = value;
			}
		}
		
		
		
		public string RunCommand( string szCmd, string szArgs, bool wait )
		{
			if( szCmd == null ) return "";
			
			System.Diagnostics.Process cmdprocess = new System.Diagnostics.Process( );
			
			cmdprocess.EnableRaisingEvents = false;
			cmdprocess.StartInfo.FileName = szCmd;
			cmdprocess.StartInfo.Arguments = szArgs;
			cmdprocess.StartInfo.UseShellExecute = false;
			cmdprocess.StartInfo.RedirectStandardOutput = true;
			
			if( cmdprocess.Start( )  )
			{
				
				if(wait) 
				{
					cmdprocess.WaitForExit();
					
					return cmdprocess.StandardOutput.ReadToEnd().Trim();
				}
				else
				{
					cmdprocess.Close( );
				}
				return "";
			}
			
			return "";
		}
	}
}
