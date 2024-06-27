using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Net.Http;

namespace WebGateway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationFilterService : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly static Dictionary<string, TokenInfo> _activeTokens = new Dictionary<string, TokenInfo>();
        private readonly string _jwtSecret;
        private readonly static int _expTime = 24; // Variable global de tiempode expiracion del token en horas

        public AuthenticationFilterService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _jwtSecret = "703b1f10dd00d23ff136c1360a6e32c7455458ba6d0294c1e91d5dccfc1291acf76975210e16f1c4a5f2352d8c2b0bb8616e39e505e41c3abe0aaebf83d295a08074f340fdbe8e2857f0a69ef7257b8167e7f10e7c96903ce2e3b15c1f500d2ed1603dbeee9032842cc613eaa6be6b1ed391665916f227b5a34649c22971b3e5a83d1b6e83ce4836a076b2f1189d7ad9d02d809afc10350adf106b97750a79522e02e8bfdc6b04c26564f67468328bcd65889be460eac395538db757100bc33aa7b2eb74805b6be11a7df7a2e9eaef180da19f49dd21bd6e0e1db2274c1b990d842832729b8d9cec687d62d73150d69268c6de0b13b567fb298d3fa3f78dcbfb";
        }

        // Endpoint de login de usuarios que devuelve un token si el usuario se encuentra en la db
        // Guarda los tokens activos en un diccionario que se usara tambien para verificar si el user es admin o no
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthenticationRequest request)
        {
            try
            {
                var client          = _httpClientFactory.CreateClient();
                var json            = JsonSerializer.Serialize(request);
                var httpRequest     = new HttpRequestMessage(HttpMethod.Post, "http://tp1usercontrollmanager:8000/api/Users/authenticate");
                httpRequest.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response        = await client.SendAsync(httpRequest);

                //Si el login fue exitoso genero el token con el tiempo de expiracion y me guardo junto con el token el tipo de usuario
                if (response.IsSuccessStatusCode)
                {
                    var jwtoken = GenerateJwtToken(request.Username);
                    var tokenInfo = new TokenInfo
                    {
                        Token = jwtoken,
                        ExpirationTime = DateTime.UtcNow.AddHours(_expTime),
                        UserRole = GetUserRoleAsync(request.Username).Result
                    };
                    _activeTokens[jwtoken] = tokenInfo;
                    return Ok(new { Token = jwtoken });
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return BadRequest($"Error: {response.StatusCode}, {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        //Generacion de token
        private string GenerateJwtToken(string username)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
                Expires = DateTime.UtcNow.AddHours(_expTime),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public class TokenInfo
        {
            public string Token { get; set; }
            public DateTime ExpirationTime { get; set; }
            public string UserRole { get; set; }
        }
        private async Task<string> GetUserRoleAsync(string username)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://tp1usercontrollmanager:8000/api/Users/isAdmin/{username}");
            var content = await response.Content.ReadAsStringAsync();
            return content.Trim();
        }
        public static TokenInfo GetTokenInfo(string token)
        {
            if (_activeTokens.TryGetValue(token, out var tokenInfo))
            {
                // Verificar si el token ha expirado
                if (DateTime.UtcNow >= tokenInfo.ExpirationTime)
                {
                    // Eliminar el token si ha expirado
                    _activeTokens.Remove(token);
                    return null;
                }
                return tokenInfo;
            }
            return null;
        }
    }

    public class AuthenticationRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}