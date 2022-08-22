using RestSharp;
using OAuth;
using Newtonsoft.Json;
namespace switter
{
    public static class TwitterAPI
    {
        static string apik, apis, acct, accs;
        public static void init(string apikey, string apisecret, string accesstoken, string accesssecret)
        {
            apik = apikey;
            apis = apisecret;
            acct = accesstoken;
            accs = accesssecret;
        }
        public static TwitterError SendTweet(string tweet)
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
            var dict = new Dictionary<string, string>();
            dict.Add("text", tweet);
            var serialized = JsonConvert.SerializeObject(dict);
            var body = serialized;
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            System.Diagnostics.Debug.WriteLine(response.Content);
            if (response.Content.Substring(0,8).Contains("data"))
            {
                return new TwitterError(true, "Success");
            }
            else return new TwitterError(false, JsonConvert.DeserializeObject<Dictionary<string,string>>(response.Content).GetValueOrDefault("detail"));
        }
        public static bool VerifyTweet(string tweet)
        {
            return true;
        }
    }
    public class TwitterError
    {
        public bool success;
        public string message;
        public TwitterError(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }
    }
}
