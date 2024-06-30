using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Data.Common;
using System.Text;
using System.Threading.Channels;
using TrackingService.API.Services;
using TrackingService.Models;
using TrackingService.Context;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TrackingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class trackingService : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMessageProducer _messageProducer;
        public trackingService(AppDbContext context)
        {
            _context = context;
            _messageProducer = new MessageProducer();
        }

        // GET api/<ValuesController>/5
        [HttpPost("event")]
        public async Task<IActionResult> TrackEvent([FromBody] TrackingEvent trackingEvent)
        {
            if(!ModelState.IsValid) return BadRequest();
            // Verificar que el EventType sea válido
            if (trackingEvent.EventType != "click" && trackingEvent.EventType != "visit_url")
            {
                return BadRequest(new { error = $"Unsupported event type: {trackingEvent.EventType}. Please provide a valid event type." });
            }

            _messageProducer.SendingMessage(trackingEvent);

            var trackingEventDb = new TrackingEventDb { EventType = trackingEvent.EventType, Url = trackingEvent.Url };
            await _context.TrackingEvents.AddAsync(trackingEventDb);
            await _context.SaveChangesAsync();

            // Si es un evento 'click', enviar también un 'visit_url' con la URL recibida
            if (trackingEvent.EventType == "click" && !string.IsNullOrEmpty(trackingEvent.Url))
            {
                var visitUrlEvent = new TrackingEvent
                {
                    EventType = "visit_url",
                    Url = trackingEvent.Url
                };
                _messageProducer.SendingMessage(visitUrlEvent);
                var visitUrlEventDb = new TrackingEventDb { EventType = visitUrlEvent.EventType, Url = visitUrlEvent.Url };
                await _context.TrackingEvents.AddAsync(visitUrlEventDb);
                await _context.SaveChangesAsync();
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
