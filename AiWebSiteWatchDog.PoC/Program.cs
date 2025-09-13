using System.Net.Http.Json;

class Program
{
	static async Task Main()
	{
		var url = "https://tampere.cloudnc.fi/fi-FI/Viranhaltijat/Suunnittelupaumlaumlllikkouml_Kaupunkiympaumlristoumln_palvelualue_liikennejaumlrjestelmaumln_suunnittelu";
		var interest = "anything containing information about parking restrictions";

		// 1. Fetch site
		using var http = new HttpClient();
		var html = await http.GetStringAsync(url);

		// 2. Strip HTML to plain text
		var doc = new HtmlAgilityPack.HtmlDocument();
		doc.LoadHtml(html);
		var text = HtmlAgilityPack.HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);

		// 3. Ask Gemini if it’s interesting
		var apiKey = "<YOUR_API_KEY>";
		var geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" + apiKey;

		var geminiBody = new
		{
			contents = new[]
			{
				new {
					parts = new[] {
						new { text = $"Here is website text:\n{text}\n\nMy interest is: {interest}\n\nDoes this contain anything interesting? Answer yes or no and explain shortly." }
					}
				}
			}
		};

		var request = new HttpRequestMessage(HttpMethod.Post, geminiUrl)
		{
			Content = JsonContent.Create(geminiBody)
		};
		request.Headers.Add("X-goog-api-key", apiKey);
		request.Headers.Add("Accept", "application/json");

		var response = await http.SendAsync(request);
		var json = await response.Content.ReadAsStringAsync();
		Console.WriteLine(json); // TODO: parse and act
	}
}
