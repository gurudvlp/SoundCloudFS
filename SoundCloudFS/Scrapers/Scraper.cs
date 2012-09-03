
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
//using MySql.Data;
//using MySql.Data.MySqlClient;

namespace btEngine.Scrapers
{

	[XmlRoot("Scraper")]
	public abstract class Scraper
	{
		[XmlElement("Name")] public string Name = "[Untitled Scraper]";
		[XmlElement("SourceName")] public string SourceName = "";
		[XmlElement("UserAgent")] public string UserAgent = btEngine.Engine.EngineName + " " + btEngine.Engine.EngineVersion + " (www.gurudigitalsolutions.com)";
		[XmlElement("Cookies")] public CookieContainer Cookies;

		[XmlElement("ResponseContentLength")] public int ResponseContentLength = 0;
		[XmlElement("FollowRedirects")] public bool FollowRedirects = true;
		[XmlElement("MaxRedirects")] public int MaxRedirects = 3;
		
		[XmlIgnoreAttribute()] public HttpWebResponse response;
		[XmlIgnoreAttribute()] public Stream resStream;
		[XmlElement("LimitRange")] public bool LimitRange = false;
		[XmlElement("LimitRangeLower")] public int LimitRangeLower = 0;
		[XmlElement("LimitRangeUpper")] public int LimitRangeUpper = 0;
		
		[XmlElement("GetStreamAtRip")] public bool GetStreamAtRip = true;
		[XmlElement("KnownMaxLength")] public int KnownMaxLength = -1;
		
		[XmlIgnoreAttribute()] public byte[] RawBinary;
		
		public Scraper ()
		{
			this.Cookies = new CookieContainer();
		}
		
		public virtual bool TakeTurn()
		{
			return true;
		}
		
		public virtual string RipPage(string Method, string Url)
		{
			//Logging.Write("BaseScraper:RipPage:Url: " + Url);
			// prepare the web page we will be asking for
			HttpWebRequest  request;
			
			try
			{
				request = (HttpWebRequest)WebRequest.Create(Url);
			
	
				// execute the request
				request.AllowAutoRedirect = true;
				request.Method = Method;
				request.UserAgent = this.UserAgent;
				request.CookieContainer = this.Cookies;
				
				if(FollowRedirects) { request.AllowAutoRedirect = true; request.MaximumAutomaticRedirections = MaxRedirects; } else { request.AllowAutoRedirect = false; }
				if(LimitRange && !(LimitRangeUpper < LimitRangeLower)) { request.AddRange(LimitRangeLower, LimitRangeUpper); }
				
				//Logging.Write("btEngine: Scrapers: Cookie Count: " + request.CookieContainer.Count.ToString());
			}
			catch(Exception ex)
			{
				Logging.Write("A fatal error occurred while ripping the page:");
				Logging.Write(Method + " " + Url);
				Logging.Write("-----");
				Logging.Write(ex.Message);
				Logging.Write(ex.StackTrace);
				//Logging.Write(ex.InnerException.StackTrace);
				Environment.Exit(0);
			}
			
			
			
			try
			{
				response = (HttpWebResponse)request.GetResponse();
		
				if(GetStreamAtRip)
				{
					//Logging.Write("BaseScraper:RipPage:GetStreamAtRip");
					return GetStreamAscii(); 
				}
				
			}
			catch(Exception ex)
			{
				Logging.Write("btEngine: Scraper: Page ripping exception");
				Logging.Write(ex.Message);
				return "";
			}
			
			return "";

		}
		
		public string GetStreamAscii()
		{
			// used to build entire input
			StringBuilder sb  = new StringBuilder();
	
			// used on each read operation
			byte[] buf = new byte[8192];
			
			resStream = response.GetResponseStream();
			string tempString = null;
			int count = 0;
	
			do
			{
				// fill the buffer with data
				count = resStream.Read(buf, 0, buf.Length);
	
				// make sure we read some data
				if (count != 0)
				{
					// translate from bytes to ASCII text
					tempString = Encoding.ASCII.GetString(buf, 0, count);
	
					// continue building the string
					sb.Append(tempString);
				}
			}
			while (count > 0); // any more data to read?
	
			return sb.ToString();
		}
		
		public byte[] GetStreamBytes()
		{
			return GetStreamBytes(true);
		}
		
		public byte[] GetStreamBytes(bool DisposeAtEnd)
		{
			//Logging.Write("Getting stream bytes for scraper.");
			
			List<BinaryChunks> BufferList = new List<BinaryChunks>();
			int ttlcount = 0;
			resStream = response.GetResponseStream();
			int count = 0;
			int timesaround = 0;
			//Logging.Write("Entering stream byte loop");
			do
			{
				// fill the buffer with data
				//Logging.Write("Reading buffer");
				byte[] buf = new byte[8192];
				count = resStream.Read(buf, 0, buf.Length);
	
				// make sure we read some data
				if (count != 0)
				{
					try
					{
						
						//streamlocation = this.RawBinary.Length;
						/*Logging.Write("1");
						int firstlen = 0;
						if(this.RawBinary == null) { firstlen = count; } else { firstlen = this.RawBinary.Length + count; }
						byte[] tmpbytes = new byte[firstlen];
						Logging.Write("2");
						if(this.RawBinary != null)
						{
							System.Buffer.BlockCopy(this.RawBinary, 0, tmpbytes, 0, this.RawBinary.Length);
						}
						
						Logging.Write("3");
						//RawBinary = new byte[tmpbytes.Length];
						if(tmpbytes.Length < firstlen) { Logging.Write("tmpbytes length is less than firstlen (" + tmpbytes.Length.ToString() + " vs " + firstlen.ToString() + ")"); }
						Logging.Write("tmpbytes: " + tmpbytes.Length.ToString() + ", firstlen: " + firstlen.ToString() + ", count: " + count.ToString());
						int useoff = 0;
						if(tmpbytes.Length == firstlen) { firstlen = 0; }// else { firstlen = firstlen - 1; }
						System.Buffer.BlockCopy(buf, 0, tmpbytes, firstlen, count);
						Logging.Write("4");
						this.RawBinary = tmpbytes;
						Logging.Write("5");
						//System.IO.File.WriteAllBytes("/home/guru/test.mp3", this.RawBinary);
						Logging.Write("End of data being in buffer");*/
						//Logging.Write("Adding " + count.ToString() + " bytes to buffer. (" + (ttlcount + count).ToString() + " total)");
						//Logging.Write("Retrieved " + (ttlcount + count).ToString() + "/" + this.KnownMaxLength.ToString() + " bytes.");
						BinaryChunks binch = new BinaryChunks();
						binch.BinBytes = buf;
						binch.ByteLength = count;
						BufferList.Add(binch);
						ttlcount = ttlcount + count;
						
						//System.IO.File.WriteAllBytes("parts/" + timesaround.ToString() + ".mp3", binch.BinBytes);
					}
					catch(Exception ex)
					{
						Logging.Write("Exception!!!");
						Logging.Write(ex.Message);
					}
				}
				
				timesaround++;
				
			}
			while (count > 0); // any more data to read?
			
			try
			{
				this.RawBinary = new byte[ttlcount];

				int ttl = 0;

				//var s = new MemoryStream();
				foreach(BinaryChunks echunk in BufferList)
				{
					//Logging.Write("echunk: " + echunk.Length.ToString() + ", ttl: " + ttl.ToString());
					//s.Write(echunk.BinBytes, 0, echunk.ByteLength);
					System.Buffer.BlockCopy(echunk.BinBytes, 0, this.RawBinary, ttl, echunk.ByteLength);

					ttl = ttl + echunk.ByteLength;
				}
				//this.RawBinary = s.ToArray();
				//System.IO.File.WriteAllBytes("/home/guru/test.mp3", s.ToArray());
				//Logging.Write("Returning binary bytes for stream");
			}
			catch(Exception ex)
			{
				Logging.Write("Exception!! :(");
				Logging.Write(ex.Message);
			}
			
			BufferList = null;
			if(DisposeAtEnd)
			{
				this.resStream.Dispose();
			}
			
			return this.RawBinary;
		}
		
		public bool Save(string profile, int id)
		{
			//	Attempt to save this engine's configuration
			/*try
			{
				if(!System.IO.Directory.Exists(Engine.ConfigPath + "Engines"))
				{
					System.IO.Directory.CreateDirectory(Engine.ConfigPath + "Engines");
				}
				if(!System.IO.Directory.Exists(Engine.ConfigPath + "Engines/" + profile))
				{
					System.IO.Directory.CreateDirectory(Engine.ConfigPath + "Engines/" + profile);
				}
				if(!System.IO.Directory.Exists(Engine.ConfigPath + "Engines/" + profile + "/Scrapers"))
				{
					System.IO.Directory.CreateDirectory(Engine.ConfigPath + "Engines/" + profile + "/Scrapers");
				}
				
				XmlSerializer s = new XmlSerializer(this.GetType());
				TextWriter w = new StreamWriter(@Engine.ConfigPath + "Engines/" + profile + "/Scrapers/" + id.ToString() + ".xml");
				s.Serialize( w, this );
				w.Close();
			}
			catch(Exception ex)
			{
				Logging.Write("btEngine: Engine: Scrapers: Save: Save failed.");
				Logging.Write(ex.Message);
				Logging.Write("-----------------------");
				Logging.Write(ex.InnerException.Message);
				return false;
			}*/
			Logging.Write("Saving scrapers is not currently implemented.");
			return true;
		}
	}
}
