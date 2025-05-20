// File: Controllers/VoiceController.cs
using Google.Cloud.Dialogflow.V2;
using Google.Cloud.Speech.V1;
using Google.Cloud.TextToSpeech.V1;
using Grpc.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using AudioEncoding = Google.Cloud.TextToSpeech.V1.AudioEncoding;
using SsmlVoiceGender = Google.Cloud.TextToSpeech.V1.SsmlVoiceGender;
using VoiceSelectionParams = Google.Cloud.TextToSpeech.V1.VoiceSelectionParams;

namespace TwilioVoiceBot.Controllers
{
    [Route("voice")]
    [ApiController]
    public class VoiceController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VoiceController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveCall()
        {
            // Google TTS للرسالة الترحيبية
            var ttsClient = TextToSpeechClient.Create();
            var ttsInput = new SynthesisInput
            {
                Text = "أهلاً وسهلاً بك، شنو تحب تعرف؟"
            };
            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = "ar-XA",
                SsmlGender = SsmlVoiceGender.Female
            };
            var audioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3
            };

            var ttsResponse = ttsClient.SynthesizeSpeech(ttsInput, voiceSelection, audioConfig);
            var base64Audio = System.Convert.ToBase64String(ttsResponse.AudioContent.ToByteArray());

            var response = new VoiceResponse();

            // Play welcome message
            response.Play(new Uri($"data:audio/mpeg;base64,{base64Audio}"));

            // Then gather speech
            var gather = new Gather(
                input: new[] { Gather.InputEnum.Speech },
                action: new Uri("/voice/process", UriKind.Relative),
                method: "POST",
                language: "ar-SA",
                speechTimeout: "auto"
            );

            response.Append(gather);
            response.Say("ما وصلنا رد، يرجى المحاولة لاحقًا.", language: "ar-SA");

            return Content(response.ToString(), "text/xml");
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessSpeech()
        {
            var speechResult = Request.Form["SpeechResult"].ToString();
            var response = new VoiceResponse();

            // Dialogflow session setup
            var sessionClient = await SessionsClient.CreateAsync();
            var sessionName = new SessionName("your-project-id", "unique-session-id");
            var queryInput = new QueryInput
            {
                Text = new TextInput
                {
                    Text = speechResult,
                    LanguageCode = "ar"
                }
            };

            var dialogflowResponse = await sessionClient.DetectIntentAsync(sessionName, queryInput);
            var fulfillmentText = dialogflowResponse.QueryResult.FulfillmentText;

            // Text-to-Speech with Google
            var ttsClient = TextToSpeechClient.Create();
            var ttsInput = new SynthesisInput
            {
                Text = fulfillmentText
            };
            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = "ar-XA",
                SsmlGender = SsmlVoiceGender.Female
            };
            var audioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3
            };

            var ttsResponse = ttsClient.SynthesizeSpeech(ttsInput, voiceSelection, audioConfig);
            var base64Audio = System.Convert.ToBase64String(ttsResponse.AudioContent.ToByteArray());

            var twiml = $"<Response><Play>data:audio/mpeg;base64,{base64Audio}</Play></Response>";
            return Content(twiml, "text/xml");
        }

        [HttpPost("recording-done")]
        public async Task<IActionResult> HandleRecording([FromForm] string RecordingUrl)
        {
            if (string.IsNullOrEmpty(RecordingUrl))
                return Content("<Response><Say>لم يتم استلام التسجيل</Say></Response>", "text/xml");

            var client = _httpClientFactory.CreateClient();
            var audioBytes = await client.GetByteArrayAsync(RecordingUrl + ".mp3");

            // Google Speech-to-Text
            var speechClient = SpeechClient.Create();
            var recognitionAudio = RecognitionAudio.FromBytes(audioBytes);
            var config = new RecognitionConfig
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.Mp3,
                LanguageCode = "ar-XA"
            };
            var result = await speechClient.RecognizeAsync(config, recognitionAudio);
            var transcript = result.Results.Count > 0 ? result.Results[0].Alternatives[0].Transcript : "";

            // Dialogflow بعد تحويل الصوت إلى نص
            var sessionClient = await SessionsClient.CreateAsync();
            var sessionName = new SessionName("your-project-id", "recording-session");
            var queryInput = new QueryInput
            {
                Text = new TextInput
                {
                    Text = transcript,
                    LanguageCode = "ar"
                }
            };
            var dialogflowResponse = await sessionClient.DetectIntentAsync(sessionName, queryInput);
            var reply = dialogflowResponse.QueryResult.FulfillmentText;

            // Google TTS للرد
            var ttsClient = TextToSpeechClient.Create();
            var ttsInput = new SynthesisInput { Text = reply };
            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = "ar-XA",
                SsmlGender = SsmlVoiceGender.Female
            };
            var audioConfig = new AudioConfig { AudioEncoding = AudioEncoding.Mp3 };
            var ttsResponse = ttsClient.SynthesizeSpeech(ttsInput, voiceSelection, audioConfig);
            var base64Audio = System.Convert.ToBase64String(ttsResponse.AudioContent.ToByteArray());

            var twiml = $"<Response><Play>data:audio/mpeg;base64,{base64Audio}</Play></Response>";
            return Content(twiml, "text/xml");
        }
    }
}
