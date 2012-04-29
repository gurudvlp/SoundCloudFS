using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
//using MySql.Data;
//using MySql.Data.MySqlClient;

namespace btEngine.Scrapers
{

	[XmlRoot("SoundCloudSearch")]
	public class SoundCloudSearch : Scraper
	{
		
		[XmlElement("ScrapeURL")] public string ScrapeURL = "";
		[XmlElement("PageText")] public string PageText = "";
		
		public SoundCloudSearch ()
		{
		}
		
		public override bool TakeTurn ()
		{
			if(this.ScrapeURL == null || this.ScrapeURL == "")
			{
				Logging.Write("SoundCloudSearch: No URL was specified to scrape.");
				return false;
			}
			
			PageText = base.RipPage("GET", this.ScrapeURL);
			return true;
		}

	}
}