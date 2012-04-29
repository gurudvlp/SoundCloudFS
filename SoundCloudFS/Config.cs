
using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using btEngine;

namespace SoundCloudFS
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
				XmlSerializer s = new XmlSerializer( typeof(SoundCloudFS.Config) );
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
	}
}
