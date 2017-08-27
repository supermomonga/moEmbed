﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace MoEmbed.Models.Metadata
{
    /// <summary>
    /// Represents the <see cref="Metadata"/> fetching from remote oEmbed providers.
    /// </summary>
    [Serializable]
    public class OEmbedProxyMetadata : Metadata
    {
        /// <summary>
        /// Gets or sets the requested URL.
        /// </summary>
        [DefaultValue(null)]
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the oEmbed servide URL.
        /// </summary>
        [DefaultValue(null)]
        public string OEmbedUrl { get; set; }

        [DefaultValue(null)]
        public EmbedData Data { get; set; }

        [NonSerialized]
        private Task<EmbedData> _FetchTask;

        /// <inheritdoc />
        public override Task<EmbedData> FetchAsync(RequestContext context)
        {
            lock (this)
            {
                if (_FetchTask == null)
                {
                    if (Data != null)
                    {
                        _FetchTask = Task.FromResult<EmbedData>(Data);
                    }
                    else
                    {
                        _FetchTask = FetchAsyncCore(context);
                        _FetchTask.ConfigureAwait(false);
                    }
                }
                return _FetchTask;
            }
        }

        private async Task<EmbedData> FetchAsyncCore(RequestContext context)
        {
            var hc = context.Service.HttpClient;

            var redirection = await hc.FollowRedirectAsync(OEmbedUrl).ConfigureAwait(false);

            var r = redirection.Message;
            r.EnsureSuccessStatusCode();

            var txt = await r.Content.ReadAsStringAsync().ConfigureAwait(false);

            Dictionary<string, object> values;
            if (r.Content.Headers.ContentType.MediaType == "text/xml")
            {
                values = new Dictionary<string, object>();
                var d = new XmlDocument();
                d.LoadXml(txt);

                foreach (XmlNode xn in d.DocumentElement.ChildNodes)
                {
                    if (xn.NodeType == XmlNodeType.Element)
                    {
                        var e = (XmlElement)xn;
                        // TODO: parse number
                        values[e.LocalName] = e.InnerText;
                    }
                }
            }
            else
            {
                var jo = JObject.Parse(txt);
                values = jo.ToObject<Dictionary<string, object>>();
            }

            Data = new EmbedData()
            {
                Url = new Uri(Uri)
            };
            if (values.ContainsKey("title"))
            {
                Data.Title = values["title"].ToString();
            }
            if (values.ContainsKey("author_name"))
            {
                Data.AuthorName = values["author_name"].ToString();
            }
            if (values.ContainsKey("author_url"))
            {
                Data.AuthorUrl = new Uri(values["author_url"].ToString());
            }
            if (values.ContainsKey("provider_name"))
            {
                Data.ProviderName = values["provider_name"].ToString();
            }
            if (values.ContainsKey("provider_url"))
            {
                Data.ProviderUrl = new Uri(values["provider_url"].ToString());
            }
            if (values.ContainsKey("cache_age"))
            {
                Data.CacheAge = (values["cache_age"] as IConvertible).ToInt32(null);
            }
            if (values.ContainsKey("thumbnail_url"))
            {
                Data.MetadataImage = new Media {
                    Thumbnail = new ImageInfo {
                        Url = new Uri(values["thumbnail_url"].ToString())
                    }
                };
            }
            if (values.ContainsKey("thumbnail_width") && Data.MetadataImage?.Thumbnail != null)
            {
                Data.MetadataImage.Thumbnail.Width = (values["thumbnail_width"] as IConvertible).ToInt32(null);
            }
            if (values.ContainsKey("thumbnail_height") && Data.MetadataImage?.Thumbnail != null)
            {
                Data.MetadataImage.Thumbnail.Height = (values["thumbnail_height"] as IConvertible).ToInt32(null);
            }

            switch (values["type"])
            {
                case "photo":
                    Data.Medias.Add(new Media()
                    {
                        Type = MediaTypes.Image,
                        Thumbnail = new ImageInfo {
                            Url = new Uri(values["url"].ToString())
                        },
                        RawUrl = new Uri(values["url"].ToString()),
                        Location = new Uri(values["url"].ToString())
                    });
                    break;

                case "video":
                    // TODO: parse video url from html parameter
                    if (values.ContainsKey("thumbnail_url"))
                    {
                        Data.Medias.Add(new Media()
                        {
                            Type = MediaTypes.Video,
                            Thumbnail = new ImageInfo {
                                Url = new Uri(values["thumbnail_url"].ToString()),
                            },
                            RawUrl = new Uri(Uri),
                            Location = new Uri(Uri)
                        });
                    }
                    break;

                case "link":
                    // Nothing to do, for now.
                    break;

                case "rich":
                    // Nothing to do, for now.
                    break;
            }
            return Data;
        }
    }
}
