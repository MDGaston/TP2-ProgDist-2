using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Tp1ApiGateway.Controllers
{
    public class ProxyController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProxyController> _logger;
        private readonly string _userApiBaseUrl = "http://tp1usercontrollmanager:8000/api"; // URL base para la API de gestión de usuarios
        private readonly string _trackingApiBaseUrl = "http://tp2trackingservice:8001/api"; // URL base para la API de tracking
        private readonly string _blacklistBaseUrl = "http://blacklistapi:8003/api"; // URL base para la API de tracking

        public ProxyController(IHttpClientFactory httpClientFactory, ILogger<ProxyController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }
        //Endpoint de entrada para las request a mi api de usuarios
        [HttpGet("{*path}")]
        [HttpPost("{*path}")]
        [HttpPut("{*path}")]
        [HttpDelete("{*path}")]
        [HttpPatch("{*path}")]
        public async Task<IActionResult> ProxyRequest(string path)
        {
            string requestUrl;

            // Determinar a qué API debe ir la solicitud basada en el prefijo del path
            if (path.StartsWith("users"))
            {
                // Construir la URL completa para la API de usuarios
                requestUrl = $"{_userApiBaseUrl}/{path}";
            }
            else if (path.StartsWith("tracking"))
            {
                // Construir la URL completa para la API de tracking
                requestUrl = $"{_trackingApiBaseUrl}/{path}";
            }
            else if (path.StartsWith("Blacklist"))
            {
                // Construir la URL completa para la API de tracking
                requestUrl = $"{_blacklistBaseUrl}/{path}";
            }
            else
            {
                // Si el path no corresponde a ninguna API conocida, devolver 404
                _logger.LogWarning("Unknown endpoint: {Path}", path);
                return NotFound();
            }

            // Copiar el método, el cuerpo y los encabezados de la solicitud entrante
            var requestMethod = HttpContext.Request.Method;
            var requestContent = new StreamContent(HttpContext.Request.Body);
            foreach (var header in HttpContext.Request.Headers)
            {
                requestContent.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            _logger.LogInformation("Forwarding request: {Method} {Url}", requestMethod, requestUrl);
            try 
            {
                // Enviar la solicitud a la API de usuarios
                var response = await _httpClient.SendAsync(new HttpRequestMessage(new HttpMethod(requestMethod), requestUrl)
                {
                    Content = requestContent
                });

                // Copiar la respuesta de la API de usuarios a la respuesta de la solicitud entrante
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Received response: {StatusCode} from {Url}", response.StatusCode, requestUrl);
                var proxyResponse = new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    Content = responseContent,
                    ContentType = response.Content.Headers.ContentType?.ToString()
                };

                return proxyResponse;
            }
            catch (HttpRequestException httpRequestException)
            {
                _logger.LogError(httpRequestException, "Error forwarding the request to {Url}", requestUrl);
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Error forwarding the request", details = httpRequestException.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request to {Url}", requestUrl);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred", details = ex.Message });
            }
        }
    }
}
