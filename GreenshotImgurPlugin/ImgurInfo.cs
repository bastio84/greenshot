﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.Xml;

namespace GreenshotImgurPlugin
{
	/// <summary>
	/// Description of ImgurInfo.
	/// </summary>
	public class ImgurInfo : IDisposable
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurInfo));

		public string Hash
		{
			get;
			set;
		}

		private string deleteHash;
		public string DeleteHash
		{
			get { return deleteHash; }
			set
			{
				deleteHash = value;
				DeletePage = "https://imgur.com/delete/" + value;
			}
		}

		public string Title
		{
			get;
			set;
		}

		public string ImageType
		{
			get;
			set;
		}

		public DateTime Timestamp
		{
			get;
			set;
		}

		public string Original
		{
			get;
			set;
		}

		public string Page
		{
			get;
			set;
		}

		public string SmallSquare
		{
			get;
			set;
		}

		public string LargeThumbnail
		{
			get;
			set;
		}

		public string DeletePage
		{
			get;
			set;
		}

		private Image image;
		public Image Image
		{
			get { return image; }
			set
			{
				if (image != null)
				{
					image.Dispose();
				}
				image = value;
			}
		}

		public ImgurInfo()
		{
		}

		/// <summary>
		/// The public accessible Dispose
		/// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// This Dispose is called from the Dispose and the Destructor.
		/// When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (image != null)
				{
					image.Dispose();
				}
			}
			image = null;
		}
		public static ImgurInfo ParseResponse(string response)
		{
			LOG.Debug(response);
			// This is actually a hack for BUG-1695
			// The problem is the (C) sign, we send it HTML encoded "&reg;" to Imgur and get it HTML encoded in the XML back 
			// Added all the encodings I found quickly, I guess these are not all... but it should fix the issue for now.
			response = response.Replace("&cent;", "&#162;");
			response = response.Replace("&pound;", "&#163;");
			response = response.Replace("&yen;", "&#165;");
			response = response.Replace("&euro;", "&#8364;");
			response = response.Replace("&copy;", "&#169;");
			response = response.Replace("&reg;", "&#174;");

			ImgurInfo imgurInfo = new ImgurInfo();
			try
			{
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(response);
				XmlNodeList nodes = doc.GetElementsByTagName("id");
				if (nodes.Count > 0)
				{
					imgurInfo.Hash = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("hash");
				if (nodes.Count > 0)
				{
					imgurInfo.Hash = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("deletehash");
				if (nodes.Count > 0)
				{
					imgurInfo.DeleteHash = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("type");
				if (nodes.Count > 0)
				{
					imgurInfo.ImageType = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("title");
				if (nodes.Count > 0)
				{
					imgurInfo.Title = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("datetime");
				if (nodes.Count > 0)
				{
					// Version 3 has seconds since Epoch
					double secondsSince;
					if (double.TryParse(nodes.Item(0).InnerText, out secondsSince))
					{
						var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
						imgurInfo.Timestamp = epoch.AddSeconds(secondsSince).DateTime;
					}
				}
				nodes = doc.GetElementsByTagName("original");
				if (nodes.Count > 0)
				{
					imgurInfo.Original = nodes.Item(0).InnerText.Replace("http:", "https:");
				}
				// Version 3 API only has Link
				nodes = doc.GetElementsByTagName("link");
				if (nodes.Count > 0)
				{
					imgurInfo.Original = nodes.Item(0).InnerText.Replace("http:", "https:");
				}
				nodes = doc.GetElementsByTagName("imgur_page");
				if (nodes.Count > 0)
				{
					imgurInfo.Page = nodes.Item(0).InnerText.Replace("http:", "https:");
				}
				else
				{
					// Version 3 doesn't have a page link in the response
					imgurInfo.Page = string.Format("https://imgur.com/{0}", imgurInfo.Hash);
				}
				nodes = doc.GetElementsByTagName("small_square");
				if (nodes.Count > 0)
				{
					imgurInfo.SmallSquare = nodes.Item(0).InnerText;
				}
				else
				{
					imgurInfo.SmallSquare = string.Format("http://i.imgur.com/{0}s.png",imgurInfo.Hash);
				}
			}
			catch (Exception e)
			{
				LOG.ErrorFormat("Could not parse Imgur response due to error {0}, response was: {1}", e.Message, response);
			}
			return imgurInfo;
		}
	}
}
