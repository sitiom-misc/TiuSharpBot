using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace MitsukuApi
{
	public class MitsukuChatBot
	{
		public string ClientName { get; private set; }
		public string SessionId { get; private set; }
		public string Channel { get; private set; }
		public string BotKey { get; private set; }

		public MitsukuChatBot()
		{
			// To allow receiving images
			SendMessage("xintrooldsite");
		}

		public MitsukuChatBot(string sessionId, string channel, string botKey)
		{
			SessionId = sessionId;
			Channel = channel;
			BotKey = botKey;
			
			// To allow receiving images
			SendMessage("xintrooldsite");
		}

		public MitsukuResponse SendMessage(string message)
		{
			message = string.Join(" ", message.Split('\r', '\n'));
			message = Regex.Replace(message, @"\s+", " ").Trim();

			// Mitsuku does not check your client_name really carefully (smirk) as long as the length is 13.
			ClientName ??= $"cw1756d{new Random().Next(1337, 969133):D6}";
			BotKey ??= GetBotKey();

			// Get reply
			RestClient client = new RestClient("https://miapi.pandorabots.com");
			RestRequest request = new RestRequest("/talk", Method.POST);
			request.AddHeaders(new Dictionary<string, string>
			{
				{"Host","miapi.pandorabots.com"},
				{"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36"},
				{"Referer","http://www.square-bear.co.uk/"}
			});
			request.AddParameter("botkey", BotKey);
			request.AddParameter("input", message);
			request.AddParameter("client_name", ClientName);
			request.AddParameter("sessionid", SessionId ?? "null");
			request.AddParameter("channel", Channel ?? "6");

			// Deserialize response
			MitsukuResponse reply = JsonConvert.DeserializeObject<MitsukuResponse>((client.Execute(request)).Content);
			SessionId ??= reply.SessionId;
			Channel ??= reply.Channel;
			// Remove HTML tags
			for (var i = 0; i < reply.Responses.Count; i++)
			{
				reply.Responses[i] = Regex.Replace(reply.Responses[i], @"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>", " ").Trim();
			}

			return reply;
		}

		public async Task<MitsukuResponse> SendMessageAsync(string message)
		{
			message = string.Join(" ", message.Split('\r', '\n'));
			message = Regex.Replace(message, @"\s+", " ").Trim();

			// Mitsuku does not check your client_name really carefully (smirk) as long as the length is 13.
			ClientName ??= $"cw1756d{new Random().Next(1337, 969133):D6}";
			BotKey ??= await GetBotKeyAsync();

			// Get reply
			RestClient client = new RestClient("https://miapi.pandorabots.com");
			RestRequest request = new RestRequest("/talk", Method.POST);
			request.AddHeaders(new Dictionary<string, string>
			{
				{"Host","miapi.pandorabots.com"},
				{"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36"},
				{"Referer","http://www.square-bear.co.uk/"}
			});
			request.AddParameter("botkey", BotKey);
			request.AddParameter("input", message);
			request.AddParameter("client_name", ClientName);
			request.AddParameter("sessionid", SessionId ?? "null");
			request.AddParameter("channel", Channel ?? "6");

			// Deserialize response
			MitsukuResponse reply = JsonConvert.DeserializeObject<MitsukuResponse>((await client.ExecuteAsync(request)).Content);
			SessionId ??= reply.SessionId;
			Channel ??= reply.Channel;
			// Remove HTML tags
			for (var i = 0; i < reply.Responses.Count; i++)
			{
				reply.Responses[i] = Regex.Replace(reply.Responses[i], @"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>", " ").Trim();
			}

			return reply;
		}

		private string GetBotKey()
		{
			string url = "https://www.pandorabots.com/mitsuku/";
			RestClient client = new RestClient(url);
			RestRequest request = new RestRequest(Method.GET);
			request.AddHeaders(new Dictionary<string, string>
			{
				{"User-Agent", string.Join(" ", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_2)",
					"AppleWebKit/537.36 (KHTML, like Gecko)",
					"Chrome/72.0.3626.119 Safari/537.36")
				},
				{"Referer",url}
			});
			IRestResponse response = client.Execute(request);
			string botKey = Regex.Match(response.Content, "PB_BOTKEY: \"(.*)\"").Groups[0].Value;
			return string.Join(" ", botKey.Split('"').Where((x, i) => i % 2 != 0));
		}
		
		private async Task<string> GetBotKeyAsync()
		{
			string url = "https://www.pandorabots.com/mitsuku/";
			RestClient client = new RestClient(url);
			RestRequest request = new RestRequest(Method.GET);
			request.AddHeaders(new Dictionary<string, string>
			{
				{"User-Agent", string.Join(" ", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_2)",
					"AppleWebKit/537.36 (KHTML, like Gecko)",
					"Chrome/72.0.3626.119 Safari/537.36")
				},
				{"Referer",url}
			});
			IRestResponse response = await client.ExecuteAsync(request);
			string botKey = Regex.Match(response.Content, "PB_BOTKEY: \"(.*)\"").Groups[0].Value;
			return string.Join(" ", botKey.Split('"').Where((x, i) => i % 2 != 0));
		}
	}
}
