using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth;
using RestSharp;
using switter.Pages;

namespace switter;

public static class TwitterApi
{
    private static string _apik, _apis, _acct, _accs, _bearer;
    public static IList<LeaderboardEntry> CompletedEntries;
    public static List<Cooldown> Cooldowns = new();

    private static List<string> ForbiddenWords = new()
        { "" };

    public static void Init(string apikey, string apisecret, string accesstoken, string accesssecret,
        string bearertoken)
    {
        _apik = apikey;
        _apis = apisecret;
        _acct = accesstoken;
        _accs = accesssecret;
        _bearer = bearertoken;
    }

    public static TwitterError SendTweet(string tweet, string mediaId)
    {
        var success = false;
        var oAclient = OAuthRequest.ForProtectedResource("POST", _apik, _apis, _acct, _accs);
        oAclient.RequestUrl = "https://api.twitter.com/2/tweets";
        var auth = oAclient.GetAuthorizationHeader();
        var client = new RestClient("https://api.twitter.com/2/tweets");
        client.Timeout = -1;
        var request = new RestRequest(Method.POST);
        request.AddHeader("Authorization", auth);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Cookie", "guest_id=v1%3A164647635225822612");
        var dict = new Dictionary<string, object>();
        if (tweet != null && tweet == "")
            tweet = null;
        if (mediaId != null && mediaId == "")
            mediaId = null;
        if (mediaId == null && tweet == null) return new TwitterError(false, "Your tweet has no content", "");
        if (mediaId != null)
        {
            var jo2 = new JArray();
            jo2.Add(mediaId);
            var jo = new JObject();
            jo.Add("media_ids", jo2);
            dict.Add("media", jo);
        }

        if (tweet != null)
            dict.Add("text", tweet);
        var serialized = JsonConvert.SerializeObject(dict);
        var body = serialized;
        request.AddParameter("application/json", body, ParameterType.RequestBody);
        var response = client.Execute(request);
        Debug.WriteLine(response.Content);
        if (response.Content.Substring(0, 8).Contains("data"))
            return new TwitterError(true, "Success",
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(response.Content)
                    .GetValueOrDefault("data").GetValueOrDefault("id"));

        return new TwitterError(false,
            JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content).GetValueOrDefault("detail",
                JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content)
                    .GetValueOrDefault("error", "An unknown error has occured")), "");
    }

    public static string GetCatFact()
    {
        var client = new RestClient("https://catfact.ninja/fact?max_length=140");
        var request = new RestRequest(Method.GET);
        request.AddHeader("Accept", "application/json");
        var response = client.Execute(request);

        return JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content)
            .GetValueOrDefault("fact", "pamirsau");
    }

    public static List<Tweet2> GetTweets(List<string> tweetids)
    {
        var list = new List<Tweet2>();

        var auth = "Bearer " + _bearer;
        var client = new RestClient("https://api.twitter.com/2/tweets?ids=" + tweetids.Join(",") +
                                    "&tweet.fields=public_metrics");
        client.Timeout = -1;
        var request = new RestRequest(Method.GET);
        request.AddHeader("Authorization", auth);
        request.AddHeader("User-Agent", "agent");
        var response = client.Execute(request);
        Debug.WriteLine("response: " + response.Content);

        if (response.Content.Substring(0, 8).Contains("data"))
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
            if (data == null)
                return null;
            var tweets = (JArray)data.GetValueOrDefault("data");
            if (tweets.Count == 0)
                return null;
            foreach (var tweet in tweets)
            {
                var deserializedTweet = JsonConvert.DeserializeObject<Dictionary<string, object>>(tweet.ToString());
                var id = (string)deserializedTweet.GetValueOrDefault("id", "");
                if (id == "")
                {
                    Debug.WriteLine("Failed getting tweet ID");
                    continue;
                }

                var pubmetrics =
                    JsonConvert.DeserializeObject<Dictionary<string, int>>(
                        ((JObject)deserializedTweet.GetValueOrDefault("public_metrics", "")).ToString());
                var retweets = pubmetrics.GetValueOrDefault("retweet_count", -1);
                var likes = pubmetrics.GetValueOrDefault("like_count", -1);
                if (likes == -1 || retweets == -1)
                {
                    Debug.WriteLine("Failed getting likes and/or retweets");
                    continue;
                }

                list.Add(new Tweet2(id, likes + retweets));
            }
        }

        return list;
    }

    public static TwitterError VerifyTweet(string tweet)
    {
        if (tweet == null || tweet.Length == 0) return new TwitterError(true, "Success", "");

        if (tweet.Length > 280) return new TwitterError(false, "The maximum length of a tweet is 280 characters", "");
        if (tweet.Contains('@'))
            return new TwitterError(false, "Tagging other people is not supported by the Twitter API", "");
        foreach (var word in ForbiddenWords)
            if (tweet.ToLower().Contains(word))
                return new TwitterError(false, "The tweet contains forbidden words", "");

        return new TwitterError(true, "Success", "");
    }

    public static string UploadImage(byte[] data) //returns the media ID or the word failed
    {
        var oauth = new OAuthInfo();
        oauth.AccessToken = _acct;
        oauth.AccessSecret = _accs;
        oauth.ConsumerKey = _apik;
        oauth.ConsumerSecret = _apis;

        return GetMediaId(new TinyTwitter(oauth), data);
    }

    public static string GetMediaId(TinyTwitter twit, byte[] bytes)
    {
        var base64File = Convert.ToBase64String(bytes);
        var response = twit.UpdateMedia(base64File);
        var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
        Debug.WriteLine("resonse:a " + response);
        return (string)deserialized.GetValueOrDefault("media_id_string", "failed");
    }
}

public class Tweet2
{
    public string Id;
    public int Likes;

    public Tweet2(string id, int likes)
    {
        Id = id;
        this.Likes = likes;
    }
}

public class TwitterError
{
    public string Id;
    public string Message;
    public bool Success;

    public TwitterError(bool success, string message, string iD)
    {
        this.Success = success;
        this.Message = message;
        Id = iD;
    }
}

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
    private readonly OAuthInfo _oauth;

    public TinyTwitter(OAuthInfo oauth)
    {
        this._oauth = oauth;
    }

    public string UpdateStatus(string message)
    {
        var web = new RequestBuilder(_oauth, "POST", "https://api.twitter.com/1.1/statuses/update.json")
            .AddParameter("status", message)
            .Execute();
        return web;
    }

    public string UpdateStatuswithmedia(string message, string media)
    {
        var web = new RequestBuilder(_oauth, "POST", "https://api.twitter.com/1.1/statuses/update.json")
            .AddParameter("status", message)
            .AddParameter("media_ids", media)
            .Execute();
        return web;
    }

    public string UpdateMedia(string message)
    {
        var web = new RequestBuilder(_oauth, "POST", "https://upload.twitter.com/1.1/media/upload.json")
            .AddParameter("media", message)
            .Execute();
        return web;
    }

    #region RequestBuilder

    public class RequestBuilder
    {
        private const string Version = "1.0";
        private const string SignatureMethod = "HMAC-SHA1";
        private readonly IDictionary<string, string> _customParameters;
        private readonly string _method;

        private readonly OAuthInfo _oauth;
        private readonly string _url;

        public RequestBuilder(OAuthInfo oauth, string method, string url)
        {
            this._oauth = oauth;
            this._method = method;
            this._url = url;
            _customParameters = new Dictionary<string, string>();
        }

        public RequestBuilder AddParameter(string name, string value)
        {
            _customParameters.Add(name, value.EscapeUriDataStringRfc3986());
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

                var parameters = new Dictionary<string, string>(_customParameters);
                AddOAuthParameters(parameters, timespan, nonce);

                var signature = GenerateSignature(parameters);
                var headerValue = GenerateAuthorizationHeaderValue(parameters, signature);

                var request = (HttpWebRequest)WebRequest.Create(GetRequestUrl());
                request.Method = _method;
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
            if (_method == "GET")
                return;

            var requestBody = Encoding.ASCII.GetBytes(GetCustomParametersString());
            using (var stream = request.GetRequestStream())
            {
                stream.Write(requestBody, 0, requestBody.Length);
            }
        }

        private string GetRequestUrl()
        {
            if (_method != "GET" || _customParameters.Count == 0)
                return _url;

            return string.Format("{0}?{1}", _url, GetCustomParametersString());
        }

        private string GetCustomParametersString()
        {
            return _customParameters.Select(x => string.Format("{0}={1}", x.Key, x.Value)).Join("&");
        }

        private string GenerateAuthorizationHeaderValue(IEnumerable<KeyValuePair<string, string>> parameters,
            string signature)
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
                .Append(_method).Append("&")
                .Append(_url.EscapeUriDataStringRfc3986()).Append("&")
                .Append(parameters
                    .OrderBy(x => x.Key)
                    .Select(x => string.Format("{0}={1}", x.Key, x.Value))
                    .Join("&")
                    .EscapeUriDataStringRfc3986());

            var signatureKey = string.Format("{0}&{1}", _oauth.ConsumerSecret.EscapeUriDataStringRfc3986(),
                _oauth.AccessSecret.EscapeUriDataStringRfc3986());
            var sha1 = new HMACSHA1(Encoding.ASCII.GetBytes(signatureKey));

            var signatureBytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(dataToSign.ToString()));
            return Convert.ToBase64String(signatureBytes);
        }

        private void AddOAuthParameters(IDictionary<string, string> parameters, string timestamp, string nonce)
        {
            parameters.Add("oauth_version", Version);
            parameters.Add("oauth_consumer_key", _oauth.ConsumerKey);
            parameters.Add("oauth_nonce", nonce);
            parameters.Add("oauth_signature_method", SignatureMethod);
            parameters.Add("oauth_timestamp", timestamp);
            parameters.Add("oauth_token", _oauth.AccessToken);
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

    public static string EncodeRfc3986(this string value)
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
        var escaped = new StringBuilder();

        var validChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-._~";

        foreach (var c in value)
            if (validChars.Contains(c.ToString()))
                escaped.Append(c);
            else
                escaped.Append("%" + Convert.ToByte(c).ToString("x2").ToUpper());

        // Return the fully-RFC3986-escaped string.
        return escaped.ToString();
    }
}