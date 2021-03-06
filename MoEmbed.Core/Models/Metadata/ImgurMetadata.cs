using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MoEmbed.Models.Imgur;
using MoEmbed.Providers;
using Portable.Xaml.Markup;

namespace MoEmbed.Models.Metadata
{
    /// <summary>
    /// Represents the <see cref="Metadata"/> for the <see href="imgur.com"/>.
    /// </summary>
    [Serializable]
    [ContentProperty(nameof(Data))]
    public class ImgurMetadata : UnknownMetadata
    {
        internal ImgurMetadataProvider Provider { get; set; }

        /// <summary>
        /// Gets or sets a type of imgur.com URL.
        /// </summary>
        [DefaultValue(default(ImgurType))]
        public ImgurType Type { get; set; }

        /// <summary>
        /// Gets or sets a URL hash in the URL.
        /// </summary>
        [DefaultValue(null)]
        public string Hash { get; set; }

        /// <inheritdoc />
        public override void OnDeserialized(MetadataService service)
        {
            Provider = service.Providers.OfType<ImgurMetadataProvider>().FirstOrDefault();
        }

        /// <inheritdoc />
        protected override async Task<EmbedData> FetchOnceAsync(RequestContext context)
        {
            var clientId = Provider?.ClientId;

            if (!string.IsNullOrEmpty(clientId)
                && DateTime.Now > Provider.LastFaulted + context.Service.ErrorResponseCacheAge)
            {
                if (Type == ImgurType.Unknown || string.IsNullOrEmpty(Hash))
                {
                    if (!ImgurMetadataProvider.ParseUrl(this))
                    {
                        Type = ImgurType.Unknown;
                        Hash = null;
                    }
                }

                switch (Type)
                {
                    case ImgurType.Image:
                        Data = await FetchImageAsync(context, clientId).ConfigureAwait(false);
                        break;

                    case ImgurType.Album:
                        Data = await FetchAlbumAsync(context, clientId).ConfigureAwait(false);
                        break;

                    case ImgurType.Gallery:
                        Data = await FetchGalleryAsync(context, clientId).ConfigureAwait(false);
                        break;

                    default:
                        Data = null;
                        break;
                }

                if (Data != null)
                {
                    return Data;
                }
            }

            var data = await base.FetchOnceAsync(context);
            data.Type = EmbedDataTypes.SingleImage;
            data.Medias.Add(data.MetadataImage);
            return data;
        }

        private async Task<EmbedData> FetchImageAsync(RequestContext context, string clientId)
        {
            var hc = context.Service.HttpClient;

            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.imgur.com/3/image/" + Hash);
            req.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", clientId);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var res = await hc.SendAsync(req).ConfigureAwait(false);

            res.EnsureSuccessStatusCode();

            var img = (await res.Content.ReadAsAsync<ImgurResponse<ImgurImage>>())?.Data;

            if (img == null)
            {
                return null;
            }

            return GetEmbedData(img);
        }

        private async Task<EmbedData> FetchAlbumAsync(RequestContext context, string clientId)
        {
            var hc = context.Service.HttpClient;

            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.imgur.com/3/album/" + Hash);
            req.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", clientId);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var res = await hc.SendAsync(req).ConfigureAwait(false);

            res.EnsureSuccessStatusCode();

            var album = (await res.Content.ReadAsAsync<ImgurResponse<ImgurAlbum>>())?.Data;

            if (!(album?.Images?.Length > 0))
            {
                return null;
            }

            return GetEmbedData(album);
        }

        private async Task<EmbedData> FetchGalleryAsync(RequestContext context, string clientId)
        {
            var hc = context.Service.HttpClient;

            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.imgur.com/3/gallery/album/" + Hash);
            req.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", clientId);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var res = await hc.SendAsync(req).ConfigureAwait(false);

            if (res.StatusCode != HttpStatusCode.NotFound)
            {
                res.EnsureSuccessStatusCode();

                var album = (await res.Content.ReadAsAsync<ImgurResponse<ImgurAlbum>>())?.Data;

                if (!(album?.Images?.Length > 0))
                {
                    return null;
                }

                return GetEmbedData(album);
            }
            else
            {
                var imgReq = new HttpRequestMessage(HttpMethod.Get, "https://api.imgur.com/3/gallery/image/" + Hash);
                imgReq.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", clientId);
                imgReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var imgRes = await hc.SendAsync(imgReq).ConfigureAwait(false);

                imgRes.EnsureSuccessStatusCode();

                var img = (await imgRes.Content.ReadAsAsync<ImgurResponse<ImgurImage>>())?.Data;

                if (img == null)
                {
                    return null;
                }

                return GetEmbedData(img);
            }
        }

        private static EmbedData GetEmbedData(ImgurAlbum album)
        {
            var ci = album.Images.FirstOrDefault(im => im.Id == album.Cover) ?? album.Images[0];
            var d = GetEmbedData(ci);
            d.Url = "https://imgur.com/a/" + album.Id;
            d.Title = album.Title ?? d.Title;
            d.Description = album.Description ?? d.Description;

            if (album.Images.Length > 1)
            {
                d.Type = EmbedDataTypes.MixedContent;

                foreach (var img in album.Images)
                {
                    if (img != ci)
                    {
                        d.Medias.Add(img.ToMedia());
                    }
                }
            }

            return d;
        }

        private static EmbedData GetEmbedData(ImgurImage img)
        {
            var media = img.ToMedia();

            return new EmbedData()
            {
                Url = media.Location,
                Title = img.Title,
                Description = img.Description,
                Type = img.IsAnimated ? EmbedDataTypes.SingleVideo : EmbedDataTypes.SingleImage,
                ProviderName = "Imgur",
                ProviderUrl = "https://imgur.com",
                RestrictionPolicy = media.RestrictionPolicy,
                MetadataImage = media,
                Medias = new[] { media }
            };
        }
    }
}