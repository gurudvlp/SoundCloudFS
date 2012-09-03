// 
//  Main.cs
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
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Serialization;

namespace scfscli
{
	class scfsc
	{
		public static Connector Connection;
		public static Configuration Config;
		public static string ConfigPath;
		
		public static void Main (string[] args)
		{
			Config = new Configuration();
			ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scfsc");
			
			if(!System.IO.Directory.Exists(scfsc.ConfigPath))
			{
				try
				{
					System.IO.Directory.CreateDirectory(scfsc.ConfigPath);
				}
				catch(Exception ex)
				{
					Console.WriteLine("Could not create the configuration directory.");
					Console.WriteLine("\t{0}", ex.Message);
					scfsc.ConfigPath = "";
				}
			}
			else
			{
				//	Attempt to load the save configuration.
				string configfile = Path.Combine(scfsc.ConfigPath, "config.xml");
				
				if(File.Exists(configfile))
				{
					try
					{
						XmlSerializer s = new XmlSerializer(typeof(scfscli.Configuration));
						TextReader tr = new StreamReader(configfile);
						scfsc.Config = (Configuration)s.Deserialize(tr);
						tr.Close();
					}
					catch(Exception ex)
					{
						Console.WriteLine("Could not load configuration file.");
						Console.WriteLine("\t" + ex.Message);
					}
				}
				else
				{
					scfsc.Config.Save();
				}
			}
			
			//	With the default config loaded, let's parse the command, make a connection,
			//	and do it up.
			
			for(int earg = 0; earg < args.Length; earg++)
			{
				Console.WriteLine("arg[{0}]: {1}", earg, args[earg]);
			}
		}
	}
}
