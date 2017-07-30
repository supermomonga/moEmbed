using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Tweetinvi;
using Tweetinvi.Models;

namespace MoEmbed.Models
{
    [Serializable]
    public class TwitterMetadata : Metadata
    {
        public TwitterMetadata(string uri, ITwitterCredentials credentials)
            : this(new Uri(uri), credentials)
        {
        }

        public TwitterMetadata(Uri uri, ITwitterCredentials credentials)
        {
            Tweetinvi.Auth.SetCredentials(credentials);
            Uri = uri;
        }

        private static Regex regex = new Regex(@"https:\/\/twitter\.com\/(?<screenName>[^\/]+)\/status\/(?<statusId>\d+)");

        private string _ScreenName;
        /// <summary>
        /// Gets the tweet's screen name.
        /// </summary>
        [DefaultValue(null)]
        public string ScreenName
        {
            get
            {
                return _ScreenName;
            }
        }

        private long _TweetId;
        /// <summary>
        /// Gets the tweet ID.
        /// </summary>
        [DefaultValue(null)]
        public long TweetId
        {
            get
            {
                return _TweetId;
            }
        }

        private Uri _Uri;
        /// <summary>
        /// Gets or sets the requested URL.
        /// </summary>
        [DefaultValue(null)]
        public Uri Uri
        {
            get
            {
                return _Uri;
            }
            set
            {
                var groups = regex.Match(value.ToString()).Groups;
                _TweetId = Int64.Parse(groups["statusId"].Value);
                _ScreenName = groups["screenName"].Value;
                _Uri = value;
            }
        }

        /// <summary>
        /// Gets or sets the URL the <see cref="Uri" /> moved to.
        /// </summary>
        [DefaultValue(null)]
        public Uri MovedTo { get; set; }

        /// <inheritdoc />
        public async override Task<IEmbedData> FetchAsync()
        {
            var tweet = Tweet.GetTweet(TweetId);
            var user = User.GetUserFromScreenName(ScreenName);

            return new EmbedData()
            {
                Type = Types.Rich,

                AuthorName = $"{ user.Name }(@{ ScreenName })",
                AuthorUrl = new Uri($"https://twitter.com/{ ScreenName }/"),

                // TODO: Insert Fav, RT
                Title = $"{ user.Name }(@{ ScreenName })",

                // TODO: Insert media
                Html = tweet.FullText,

                // TODO: Use request parameter
                Width = 400,
                Height = 150,

                ProviderName = "Twitter",
                ProviderUrl = new Uri("https://twitter.com/"),
            };
        }
    }
}













