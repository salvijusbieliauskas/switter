using RestSharp;
using OAuth;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Net;
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace switter
{
    public static class TwitterAPI
    {
        static string apik, apis, acct, accs,bearer;
        public static IList<Pages.LeaderboardEntry> completedEntries;
        public static List<Pages.Cooldown> cooldowns = new List<Pages.Cooldown>();
        public static void init(string apikey, string apisecret, string accesstoken, string accesssecret, string bearertoken)
        {
            apik = apikey;
            apis = apisecret;
            acct = accesstoken;
            accs = accesssecret;
            bearer = bearertoken;
        }
        public static TwitterError SendTweet(string tweet, string mediaID)
        {
            bool success = false;
            OAuthRequest oAclient = OAuthRequest.ForProtectedResource("POST", apik, apis, acct, accs);
            oAclient.RequestUrl = "https://api.twitter.com/2/tweets";
            string auth = oAclient.GetAuthorizationHeader();
            var client = new RestClient("https://api.twitter.com/2/tweets");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", auth);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Cookie", "guest_id=v1%3A164647635225822612");
            var dict = new Dictionary<string, object>();
            if (tweet != null && tweet == "")
                tweet = null;
            if (mediaID != null && mediaID == "")
                mediaID = null;
            if (mediaID == null && tweet == null)
            {
                return new TwitterError(false, "Your tweet has no content", "");
            }
            if (mediaID!=null)
            {
                JArray jo2 = new JArray();
                jo2.Add(mediaID);
                JObject jo = new JObject();
                jo.Add("media_ids", jo2);
                dict.Add("media", jo);
            }
            if(tweet!=null)
                dict.Add("text", tweet);
            var serialized = JsonConvert.SerializeObject(dict);
            var body = serialized;
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            System.Diagnostics.Debug.WriteLine(response.Content);
            if (response.Content.Substring(0, 8).Contains("data"))
            {
                return new TwitterError(true, "Success", JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string,string>>>(response.Content).GetValueOrDefault("data").GetValueOrDefault("id"));
            }
            else
            {
                return new TwitterError(false, JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content).GetValueOrDefault("detail", JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content).GetValueOrDefault("error", "An unknown error has occured")), "");
            }
        }
        public static List<Tweet2> GetTweets(List<string> tweetids)
        {
            var list = new List<Tweet2>();

            string auth = "Bearer "+bearer;
            var client = new RestClient("https://api.twitter.com/2/tweets?ids="+tweetids.Join(",")+ "&tweet.fields=public_metrics");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", auth);
            request.AddHeader("User-Agent", "SUDAS");
            IRestResponse response = client.Execute(request);
            System.Diagnostics.Debug.WriteLine("response: "+response.Content);

            if(response.Content.Substring(0, 8).Contains("data"))
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                if (data == null)
                    return null;
                JArray tweets = (JArray)data.GetValueOrDefault("data");
                if (tweets.Count==0)
                    return null;
                foreach(var tweet in tweets)
                {
                    var HAHAHAHAHAH = JsonConvert.DeserializeObject<Dictionary<string, object>>(tweet.ToString());
                    var id = (string)HAHAHAHAHAH.GetValueOrDefault("id", "");
                    if (id == "")
                    {
                        System.Diagnostics.Debug.WriteLine("Failed getting tweet ID");
                        continue;
                    }
                    var pubmetrics = JsonConvert.DeserializeObject<Dictionary<string,int>>(((JObject)HAHAHAHAHAH.GetValueOrDefault("public_metrics","")).ToString());
                    int retweets = pubmetrics.GetValueOrDefault("retweet_count", -1);
                    int likes = pubmetrics.GetValueOrDefault("like_count", -1);
                    if (likes == -1 || retweets == -1)
                    {
                        System.Diagnostics.Debug.WriteLine("Failed getting likes and/or retweets");
                        continue;
                    }
                    list.Add(new Tweet2(id, likes + retweets));
                }
            }
            return list;
        }
        public static List<string> forbiddenWords = new List<string>() { "nigger", "nlgger", "n1gger", "nigg3r", "n1gg3r", "nlg", "nig", "n|g", "n\\g", "n/g", "n//g", "n\\\\g" };
        public static TwitterError VerifyTweet(string tweet)
        {
            if (tweet == null || tweet.Length == 0)
            {
                return new TwitterError(true, "Success", "");
            }
            else if (tweet.Length > 280)
            {
                return new TwitterError(false, "The maximum length of a tweet is 280 characters", "");
            }
            if (tweet.Contains('@'))
            {
                return new TwitterError(false, "Tagging other people is not supported by the Twitter API", "");
            }
            foreach (var word in forbiddenWords)
            {
                if (tweet.ToLower().Contains(word))
                {
                    return new TwitterError(false, "The tweet contains forbidden words", "");
                }
            }
            return new TwitterError(true, "Success", "");
        }
        public static string UploadImage(byte[] data) //returns the media ID or the word failed
        {
            var oauth = new OAuthInfo();
            oauth.AccessToken = acct;
            oauth.AccessSecret = accs;
            oauth.ConsumerKey = apik;
            oauth.ConsumerSecret = apis;

            return GetMediaId(new TinyTwitter(oauth), data);
        }
        //SUDAS
        public static string GetMediaId(TinyTwitter twit, byte[] bytes)
        {
            string Base64File = Convert.ToBase64String(bytes);
            string response = twit.UpdateMedia(Base64File);
            var what = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            System.Diagnostics.Debug.WriteLine(response);
            return (string)what.GetValueOrDefault("media_id_string","failed");
            //return "";
        }
        //
    }
    public class Tweet2
    {
        public string ID;
        public int likes;
        public Tweet2(string id, int likes)
        {
            this.ID = id;
            this.likes = likes;
        }
    }
    public class TwitterError
    {
        public bool success;
        public string message;
        public string ID;
        public TwitterError(bool success, string message, string iD)
        {
            this.success = success;
            this.message = message;
            this.ID = iD;
        }
    }
    //sudas
    
public class OAuthInfo
    {
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string AccessToken { get; set; }
        public string AccessSecret { get; set; }
    }

    public class Tweet
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; }
        public string ScreenName { get; set; }
        public string Text { get; set; }
    }

    public class TinyTwitter
    {
        private readonly OAuthInfo oauth;

        public TinyTwitter(OAuthInfo oauth)
        {
            this.oauth = oauth;
        }

        public string UpdateStatus(string message)
        {
            string web = new RequestBuilder(oauth, "POST", "https://api.twitter.com/1.1/statuses/update.json")
                .AddParameter("status", message)
                .Execute();
            return web;
        }
        public string UpdateStatuswithmedia(string message, string media)
        {
            string web = new RequestBuilder(oauth, "POST", "https://api.twitter.com/1.1/statuses/update.json")
                .AddParameter("status", message)
                .AddParameter("media_ids", media)
                .Execute();
            return web;
        }
        public string UpdateMedia(string message)
        {
            string web = new RequestBuilder(oauth, "POST", "https://upload.twitter.com/1.1/media/upload.json")
                .AddParameter("media", message)
                .Execute();
            return web;
        }

        #region RequestBuilder

        public class RequestBuilder
        {
            private const string VERSION = "1.0";
            private const string SIGNATURE_METHOD = "HMAC-SHA1";

            private readonly OAuthInfo oauth;
            private readonly string method;
            private readonly IDictionary<string, string> customParameters;
            private readonly string url;

            public RequestBuilder(OAuthInfo oauth, string method, string url)
            {
                this.oauth = oauth;
                this.method = method;
                this.url = url;
                customParameters = new Dictionary<string, string>();
            }

            public RequestBuilder AddParameter(string name, string value)
            {
                customParameters.Add(name, value.EscapeUriDataStringRfc3986());
                return this;
            }

            public string Execute()
            {
                string content;
                Execute(out content);
                return content;
            }

            public WebResponse Execute(out string content)
            {
                try
                {
                    var timespan = GetTimestamp();
                    var nonce = CreateNonce();

                    var parameters = new Dictionary<string, string>(customParameters);
                    AddOAuthParameters(parameters, timespan, nonce);

                    var signature = GenerateSignature(parameters);
                    var headerValue = GenerateAuthorizationHeaderValue(parameters, signature);

                    var request = (HttpWebRequest)WebRequest.Create(GetRequestUrl());
                    request.Method = method;
                    request.ContentType = "application/x-www-form-urlencoded";

                    request.Headers.Add("Authorization", headerValue);

                    WriteRequestBody(request);

                    // It looks like a bug in HttpWebRequest. It throws random TimeoutExceptions
                    // after some requests. Abort the request seems to work. More info: 
                    // http://stackoverflow.com/questions/2252762/getrequeststream-throws-timeout-exception-randomly

                    var response = request.GetResponse();

                    using (var stream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            content = reader.ReadToEnd();
                        }
                    }

                    request.Abort();

                    return response;
                }
                catch (Exception ex)
                {
                    content = "";
                    return null;
                }
            }

            private void WriteRequestBody(HttpWebRequest request)
            {
                if (method == "GET")
                    return;

                var requestBody = Encoding.ASCII.GetBytes(GetCustomParametersString());
                using (var stream = request.GetRequestStream())
                    stream.Write(requestBody, 0, requestBody.Length);
            }

            private string GetRequestUrl()
            {
                if (method != "GET" || customParameters.Count == 0)
                    return url;

                return string.Format("{0}?{1}", url, GetCustomParametersString());
            }

            private string GetCustomParametersString()
            {
                return customParameters.Select(x => string.Format("{0}={1}", x.Key, x.Value)).Join("&");
            }

            private string GenerateAuthorizationHeaderValue(IEnumerable<KeyValuePair<string, string>> parameters, string signature)
            {
                return new StringBuilder("OAuth ")
                    .Append(parameters.Concat(new KeyValuePair<string, string>("oauth_signature", signature))
                                .Where(x => x.Key.StartsWith("oauth_"))
                                .Select(x => string.Format("{0}=\"{1}\"", x.Key, x.Value.EscapeUriDataStringRfc3986()))
                                .Join(","))
                    .ToString();
            }

            private string GenerateSignature(IEnumerable<KeyValuePair<string, string>> parameters)
            {
                var dataToSign = new StringBuilder()
                    .Append(method).Append("&")
                    .Append(url.EscapeUriDataStringRfc3986()).Append("&")
                    .Append(parameters
                                .OrderBy(x => x.Key)
                                .Select(x => string.Format("{0}={1}", x.Key, x.Value))
                                .Join("&")
                                .EscapeUriDataStringRfc3986());

                var signatureKey = string.Format("{0}&{1}", oauth.ConsumerSecret.EscapeUriDataStringRfc3986(), oauth.AccessSecret.EscapeUriDataStringRfc3986());
                var sha1 = new HMACSHA1(Encoding.ASCII.GetBytes(signatureKey));

                var signatureBytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(dataToSign.ToString()));
                return Convert.ToBase64String(signatureBytes);
            }

            private void AddOAuthParameters(IDictionary<string, string> parameters, string timestamp, string nonce)
            {
                parameters.Add("oauth_version", VERSION);
                parameters.Add("oauth_consumer_key", oauth.ConsumerKey);
                parameters.Add("oauth_nonce", nonce);
                parameters.Add("oauth_signature_method", SIGNATURE_METHOD);
                parameters.Add("oauth_timestamp", timestamp);
                parameters.Add("oauth_token", oauth.AccessToken);
            }

            private static string GetTimestamp()
            {
                return ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
            }

            private static string CreateNonce()
            {
                return new Random().Next(0x0000000, 0x7fffffff).ToString("X8");
            }
        }

        #endregion
    }

    public static class TinyTwitterHelperExtensions
    {
        public static string Join<T>(this IEnumerable<T> items, string separator)
        {
            return string.Join(separator, items.ToArray());
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> items, T value)
        {
            return items.Concat(new[] { value });
        }

        public static string EncodeRFC3986(this string value)
        {
            // From Twitterizer http://www.twitterizer.net/

            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var encoded = Uri.EscapeDataString(value);

            return Regex
                .Replace(encoded, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper())
                .Replace("(", "%28")
                .Replace(")", "%29")
                .Replace("$", "%24")
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27")
                .Replace("%7E", "~");
        }

        public static string EscapeUriDataStringRfc3986(this string value)
        {
            StringBuilder escaped = new StringBuilder();

            string validChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-._~";

            foreach (char c in value)
            {
                if (validChars.Contains(c.ToString()))
                {
                    escaped.Append(c);
                }
                else
                {
                    escaped.Append("%" + Convert.ToByte(c).ToString("x2").ToUpper());
                }
            }

            // Return the fully-RFC3986-escaped string.
            return escaped.ToString();
        }
    }
}