using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tp1ApiGateway.Controllers
{
    public class ProxyController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _userApiBaseUrl = "http://tp1usercontrollmanager:8000/api";

        public ProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }
        //Endpoint de entrada para las request a mi api de usuarios
        [HttpGet("{*path}")]
        [HttpPost("{*path}")]
        [HttpPut("{*path}")]
        [HttpDelete("{*path}")]
        [HttpPatch("{*path}")]
        public async Task<IActionResult> ProxyRequest(string path)
        {
            // Construir la URL completa para la API de usuarios
            var requestUrl = $"{_userApiBaseUrl}/{path}";

            // Copiar el método, el cuerpo y los encabezados de la solicitud entrante
            var requestMethod = HttpContext.Request.Method;
            var requestContent = new StreamContent(HttpContext.Request.Body);
            foreach (var header in HttpContext.Request.Headers)
            {
                requestContent.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            // Enviar la solicitud a la API de usuarios
            var response = await _httpClient.SendAsync(new HttpRequestMessage(new HttpMethod(requestMethod), requestUrl)
            {
                Content = requestContent
            });

            // Copiar la respuesta de la API de usuarios a la respuesta de la solicitud entrante
            var responseContent = await response.Content.ReadAsStringAsync();
            var proxyResponse = new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = responseContent,
                ContentType = response.Content.Headers.ContentType?.ToString()
            };

            return proxyResponse;
        }
    }
}
