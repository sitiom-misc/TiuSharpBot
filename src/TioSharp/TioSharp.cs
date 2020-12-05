using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static System.Text.Encoding;

namespace TioSharp
{
	public class TioApi
	{
		public string Backend { get; }
		public string Json { get; }
		public string[] Languages { private set; get; }

		public TioApi(string backend = "https://tio.run/cgi-bin/run/api/", string json = "https://tio.run/languages.json")
		{
			Backend = backend;
			Json = json;
			RefreshLanguages();
		}

		public void RefreshLanguages()
		{
			List<string> newList = new List<string>();

			JObject file = JObject.Parse(new WebClient().DownloadString(Json));

			foreach (JToken content in file.Children())
			{
				JProperty jProperty = content.ToObject<JProperty>();
				if (jProperty != null) newList.Add(jProperty.Name);
			}

			Languages = newList.ToArray();
		}

		public async Task RefreshLanguagesAsync()
		{
			List<string> newList = new List<string>();

			JObject file = JObject.Parse(await new WebClient().DownloadStringTaskAsync(new Uri(Json)));

			foreach (JToken content in file.Children())
			{
				JProperty jProperty = content.ToObject<JProperty>();
				if (jProperty != null) newList.Add(jProperty.Name);
			}
			Languages = newList.ToArray();
		}

		// <summary>
		// Returns a DEFLATE compressed byte array ready to be sent
		// </summary>
		public byte[] CreateRequestData(string language, string code, string[] inputs = null, string[] cFlags = null, string[] options = null, string[] args = null)
		{
			Dictionary<string, string[]> strings = new Dictionary<string, string[]> {
				{ "lang", new[] { language } },
				{ ".code.tio", new[] { code } },
				{ ".input.tio", inputs != null? new[] { string.Join('\n', inputs) } : null },
				{ "TIO_CFLAGS", cFlags },
				{ "TIO_OPTIONS", options },
				{ "args", args }
 			};

			List<byte> bytes = new List<byte>();

			foreach (KeyValuePair<string, string[]> pair in strings)
			{
				bytes.AddRange(ToTioBytes(pair));
			}
			bytes.Add((byte)'R');
			return Ionic.Zlib.DeflateStream.CompressBuffer(bytes.ToArray());
		}

		// <summary>
		// Sends given request and returns TIO output
		// </summary>
		public async Task<string> SendAsync(byte[] requestData)
		{
			HttpClient client = new HttpClient();
			HttpResponseMessage response = await client.PostAsync("https://tio.run/cgi-bin/run/api/", new ByteArrayContent(requestData));
			response.EnsureSuccessStatusCode();
			byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
			string output = Ionic.Zlib.GZipStream.UncompressString(responseBytes);

			return output.Replace(output[..16], ""); // Remove token
		}

		// <summary>
		// Generates a valid TIO byte array (UTF-8) for a variable or a file
		// </summary>
		private static byte[] ToTioBytes(KeyValuePair<string, string[]> pair)
		{
			var (name, value) = pair;

			if (value == null) return new byte[0];

			return value.Length switch
			{
				0 => new byte[0],
				1 => name switch
				{
					"lang" => UTF8.GetBytes(string.Join('\0', 'V' + name, value.Length, value[0]) + '\0'), // Language
					_ => UTF8.GetBytes(string.Join('\0', 'F' + name, UTF8.GetBytes(value[0]).Length, value[0]) +
									   '\0') // Code, and Input
				},
				_ => UTF8.GetBytes(string.Join('\0', 'V' + name, value.Length, string.Join('\0', value)) +
								   "\0") // Compiler flags, Options, and Args
			};
		}
	}
}
