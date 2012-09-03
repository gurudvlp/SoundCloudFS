// 
//  Config.cs
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
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace scfscli
{
	[XmlRoot("scfscConfig")]
	public class Configuration
	{
		[XmlElement("RemotePort")] public int RemotePort = 6034;
		[XmlElement("RemoteIP")] public string RemoteIP = "127.0.0.1";
		
		public Configuration ()
		{
		}
		
		
		public bool Save()
		{
			if(scfsc.ConfigPath == "")
			{
				Console.WriteLine("Could not save the configuration.  There is no configuration path.");
				return false;
			}
			
			string configfile = System.IO.Path.Combine(scfsc.ConfigPath, "config.xml");
			try
			{
				XmlSerializer s = new XmlSerializer( typeof(Configuration) );
				TextWriter w = new StreamWriter( @configfile);
				s.Serialize( w, this );
				w.Close();
			}
			catch(Exception ex)
			{
				Console.WriteLine("Error saving configuration.");
				Console.WriteLine("\t" + ex.Message);
				
				return false;
			}
			
			return true;
		}
		
	}
}

