
using System;
using System.Xml;
using System.Xml.Serialization;
using System.Data;
using System.Collections;
using System.Collections.Generic;

namespace SoundCloudFS
{

	[XmlRoot("user")]
	public class User
	{
		[XmlElement("id")] public int ID = 0;
		[XmlElement("permalink")] public string Permalink = "";
		[XmlElement("username")] public string Username = "";
		[XmlElement("uri")] public string Uri = "";
		[XmlElement("permalink-url")] public string PermalinkURL = "";
		[XmlElement("avatar-url")] public string AvatarURL = "";
		[XmlElement("country")] public string Country = "";
		[XmlElement("full_name")] public string FullName = "";
		[XmlElement("city")] public string City = "";
		[XmlElement("description")] public string Description = "";
		[XmlElement("discogs_name")] public string DiscogsName = "";
		[XmlElement("myspace_name")] public string MySpaceName = "";
		[XmlElement("website")] public string Website = "";
		[XmlElement("website_title")] public string WebsiteTitle = "";
		[XmlElement("online")] public bool Online = false;
		[XmlElement("track_count")] public int TrackCount = 0;
		[XmlElement("playlist_count")] public int PlaylistCount = 0;
		[XmlElement("public_favorites_count")] public int PublicFavoritesCount = 0;
		[XmlElement("followers_count")] public int FollowersCount = 0;
		[XmlElement("followings_count")] public int FollowingsCount = 0;
		
		public User ()
		{
		}
	}
}
