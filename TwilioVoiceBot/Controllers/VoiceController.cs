using Microsoft.AspNetCore.Mvc;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace TwilioVoiceBot.Controllers
{
    [Route("voice")]
    [ApiController]
    public class VoiceController : ControllerBase
    {
        [HttpPost]
        public IActionResult ReceiveCall()
        {
            var response = new VoiceResponse();

            var gather = new Gather(
               input: new[] { Gather.InputEnum.Speech },
                action: new Uri("/voice/process", UriKind.Relative),
                method: "POST",
                language: "ar-SA",
                speechTimeout: "auto"
            );

            gather.Say("أهلاً وسهلاً بيك، وياك الرد الآلي من شركة الكفيل أمنية. شنو تحب تعرف؟", language: "ar-SA");

            response.Append(gather);

            // fallback if no input is received
            response.Say("ما وصلنا رد، يرجى المحاولة لاحقًا.", language: "ar-SA");

            return Content(response.ToString(), "text/xml");
        }

        [HttpPost("process")]
        public IActionResult ProcessSpeech()
        {
            var speechResult = Request.Form["SpeechResult"].ToString().ToLower();
            var response = new VoiceResponse();

            if (speechResult.Contains("دوام"))
                response.Say("دوامنا من تسعة الصبح للخمسة العصر، من الأحد للخميس.", language: "ar-SA");
            else if (speechResult.Contains("موقع"))
                response.Say("مقرنا الرئيسي في النجف الأشرف، شارع كذا.", language: "ar-SA");
            else if (speechResult.Contains("وظيفة"))
                response.Say("تگدر تقدم من خلال الموقع الرسمي أو عن طريق مندوبينا.", language: "ar-SA");
            else
                response.Say("ما گدرت أفتهم سؤالك. تواصل ويه مركز الخدمة على الرقم 123456789.", language: "ar-SA");

            return Content(response.ToString(), "text/xml");
        }
    }
}
