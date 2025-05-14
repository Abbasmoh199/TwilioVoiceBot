using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TwilioVoiceBot.Model
{
    public class OpenAiService
    {
        private readonly string _apiKey = "sk-...";
        public async Task<string> AskChatGPT(string input)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "أنت مساعد افتراضي تتحدث باللهجة العراقية." },
                    new { role = "user", content = input }
                }
            };

            var jsonBody = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await http.PostAsync("https://api.openai.com/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic parsed = JsonConvert.DeserializeObject(jsonResponse);
                return parsed?.choices[0]?.message?.content?.ToString().Trim() ?? "ما گدرت أجاوب.";
            }
            catch (Exception ex)
            {
                // سجل الخطأ لو تحب
                return $"صار خطأ: {ex.Message}";
            }
        }
    }
}
