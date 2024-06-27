using TrackingService.Controllers;

namespace TrackingService.API.Services
{
    public interface IMessageProducer
    {
        public void SendingMessage(TrackingEvent trackingEvent);
    }
}
