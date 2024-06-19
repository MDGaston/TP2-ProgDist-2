using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Data.Common;
using System.Text;
using System.Threading.Channels;
using TrackingService.API.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TrackingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class trackingService : ControllerBase
    {
        private readonly IMessageProducer _messageProducer;
        public trackingService()
        {
            _messageProducer = new MessageProducer();
        }

        // GET api/<ValuesController>/5
        [HttpGet("hola")]
        public string Get()
        {
            return "value";
        }
        [HttpPost("event")]
        public IActionResult TrackEvent([FromBody] TrackingEvent trackingEvent)
        {
            if(!ModelState.IsValid) return BadRequest();
            _messageProducer.SendingMessage(trackingEvent);

            // Si es un evento 'click', enviar también un 'visit_url' con la URL recibida
            if (trackingEvent.EventType == "click" && !string.IsNullOrEmpty(trackingEvent.Url))
            {
                var visitUrlEvent = new TrackingEvent
                {
                    EventType = "visit_url",
                    Url = trackingEvent.Url
                };
                _messageProducer.SendingMessage(visitUrlEvent);
            }

            return Ok(new { message = "Tracking event successfully registered" });
        }
    }
    public class TrackingEvent
    {
        public string EventType { get; set; }
        public string Url { get; set; }
    }
}
