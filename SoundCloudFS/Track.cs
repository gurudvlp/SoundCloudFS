
using System;
using System.Xml;
using System.Xml.Serialization;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using btEngine;

namespace SoundCloudFS
{

	[XmlRoot("track")]
	public class Track
	{
		[XmlElement("id")] public long ID = 0;
		[XmlElement("created-at")] public string CreatedAt = "";
		[XmlElement("user-id")] public long UserID = 0;
		[XmlElement("duration")] public int Duration = 0;
		[XmlElement("commentable")] public bool Commentable = false;
		[XmlElement("state")] public string State = "";
		[XmlElement("sharing")] public string Sharing = "";
		[XmlElement("tag-list")] public string TagList = "";
		[XmlElement("permalink")] public string Permalink = "";
		[XmlElement("description")] public string Description = "";
		[XmlElement("streamable")] public bool Streamable = false;
		[XmlElement("downloadable")] public bool Downloadable = false;
		[XmlElement("genre")] public string Genre = "";
		[XmlElement("release")] public string Release = "";
		[XmlElement("purchase-url")] public string PurchaseURL = "";
		[XmlElement("label-id")] public string LabelID = "";
		[XmlElement("label-name")] public string LabelName = "";
		[XmlElement("isrc")] public string Isrc = ""; /* ?? */
		[XmlElement("video-url")] public string VideoURL = "";
		[XmlElement("track-type")] public string TrackType = "";
		[XmlElement("key-signature")] public string KeySignature = "";
		[XmlElement("bpm")] public string BPM = "";
		[XmlElement("title")] public string Title = "";
		[XmlElement("release-year")] public string ReleaseYear = "";
		[XmlElement("release-month")] public string ReleaseMonth = "";
		[XmlElement("release-day")] public string ReleaseDay = "";
		[XmlElement("original-format")] public string OriginalFormat = "";
		[XmlElement("license")] public string License = "";
		[XmlElement("uri")] public string Uri = "";
		[XmlElement("permalink-url")] public string PermalinkURL = "";
		[XmlElement("artwork-url")] public string ArtworkURL = "";
		[XmlElement("waveform-url")] public string WaveformURL = "";
		[XmlElement("stream-url")] public string StreamURL = "";
		[XmlElement("playback-count")] public int PlaybackCount = 0;
		[XmlElement("download-count")] public int DownloadCount = 0;
		[XmlElement("favoritings-count")] public int FavoritingsCount = 0;
		[XmlElement("comment-count")] public int CommentCount = 0;
		[XmlElement("attachments-uri")] public string AttachmentsUri = "";
		[XmlElement("user")] public User SCUser = new User();
		[XmlIgnore()] public int Filesize = 0;
		[XmlIgnore()] public byte[] RawData = null;
		[XmlIgnore()] public bool Retrieved = false;
		[XmlIgnore()] public bool RetrievalStarted = false;
		[XmlIgnore()] public int BytesRetrieved = 0;
		
		[XmlIgnoreAttribute()] private long TimeCreated = 0;
		[XmlIgnoreAttribute()] private long TimeAccessed = 0;
		
		public Track ()
		{
		}
		
		[XmlIgnoreAttribute()] public string Filename
		{
			get
			{
				string working = Engine.Config.FilenameFormat;
				working = working.Replace("[TRACKTITLE]", this.Title);
				working = working.Replace("[USERNAME]", this.SCUser.Username);
				working = working.Replace("[USERID]", this.UserID.ToString());
				working = working.Replace("[GENRE]", this.Genre);
				
				if(working == "") { working = this.Title; }
				
				working = working.Replace("*", "-");
				working = working.Replace(":", "-");
				working = working.Replace("\\", "-");
				working = working.Replace("/", "-");
				working = working.Replace("<", "-");
				working = working.Replace(">", "-");
				working = working.Replace("|", "-");
				working = working.Replace("\"", "-");
				working = working.Replace("?", "-");
				
				if(working.Length > 251) { working = working.Substring(0, 251); }
				return working + ".mp3";
			}
		}
		
		public int CalculateFilesize()
		{
			//	Determine the size of the track file.  This is normally a stream, and SoundCloud does not
			//	support the HTTP HEAD request method.  So it's pretty damn gay that I need to open the
			//	stream to determine the filesize.
			
			btEngine.Scrapers.StreamHeaders sth = new btEngine.Scrapers.StreamHeaders();
			sth.FollowRedirects = false;
			sth.ScrapeURL = StreamURL + "?client_id=" + Engine.Config.ClientID;
			sth.TakeTurn();
			
			string templocation = sth.response.GetResponseHeader("Location");
			
			sth = new btEngine.Scrapers.StreamHeaders();
			sth.FollowRedirects = false;
			sth.ScrapeURL = templocation;
			sth.LimitRange = true;
			sth.LimitRangeLower = 0;
			sth.LimitRangeUpper = 0;
			sth.TakeTurn();
			
			templocation = sth.response.GetResponseHeader("Content-Range");
			templocation = templocation.Replace("bytes 0-0/", "").Trim();
			int contlength = Int32.Parse(templocation);
			this.Filesize = contlength;
			
			//RawData = new byte[contlength];
			
			return contlength;
		}
		
		public bool Retrieve()
		{
			if(Retrieved) { return true; }
			
			if(this.Filesize == 0) { return false; }
			
			btEngine.Scrapers.StreamHeaders sth = new btEngine.Scrapers.StreamHeaders();
			sth.FollowRedirects = false;
			sth.ScrapeURL = StreamURL + "?client_id=" + Engine.Config.ClientID;
			sth.TakeTurn();
			
			string templocation = sth.response.GetResponseHeader("Location");
			Logging.Write("Stream Retrieval redirecting to " + templocation);
			sth = new btEngine.Scrapers.StreamHeaders();
			sth.FollowRedirects = false;
			sth.ScrapeURL = templocation;
			sth.GetStreamAtRip = false;
			sth.TakeTurn();
			
			sth.KnownMaxLength = this.Filesize;
			this.RawData = new byte[this.Filesize];
			this.RawData = sth.GetStreamBytes();
			this.Retrieved = true;
			
			Logging.Write("Track: " + this.Title + " retrieved and ready for usage.");
			sth = null;
			
			return true;
		}
		
		public long UnixTimeCreated()
		{
			try
			{
				if(TimeCreated > 0) { return TimeCreated; }
				
				string tmpat = this.CreatedAt;
				if(tmpat == null || tmpat == "" || tmpat == "null")
				{
					TimeCreated = (long)(DateTime.UtcNow - new DateTime(1970, 1,1,0,0,0)).TotalSeconds;
					return TimeCreated;
				}
				
				//	SoundCloud returns a date sort of like:
				//		2011/01/08 01:01:25 +0000
				//tmpat = tmpat.Substring(0, tmpat.Length - 5).Trim();
				int year = Int32.Parse(tmpat.Substring(0, 4));
				int month = Int32.Parse(tmpat.Substring(5, 2));
				int day = Int32.Parse(tmpat.Substring(8, 2));
				int hour = Int32.Parse(tmpat.Substring(11, 2));
				int minute = Int32.Parse(tmpat.Substring(14, 2));
				int second = Int32.Parse(tmpat.Substring(17, 2));
				
				DateTime whenat = new DateTime(year, month, day, hour, minute, second);
				TimeCreated = (long)(whenat - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
				
				//TimeCreated = (new DateTime(year, month, day, hour, minute, second).TotalSeconds) - (new DateTime(1970, 1, 1, 0, 0, 0).TotalSeconds);
			}
			catch(Exception ex)
			{
				Logging.Write("Exception figuring out the UnixTimeCreated from " + this.CreatedAt);
				Logging.Write(ex.Message);
			}
			
			return TimeCreated;
		}
		
		public long UnixTimeAccessed()
		{
			if(TimeAccessed > 0) { return TimeAccessed; }
			TimeAccessed = UnixTimeCreated();
			return TimeAccessed;
		}
		
		public void Touch()
		{
			TimeAccessed = (long)(DateTime.UtcNow - new DateTime(1970, 1,1,0,0,0)).TotalSeconds;
		}
	}
}
