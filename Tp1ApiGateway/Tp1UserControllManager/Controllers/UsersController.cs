using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiUserManagement.Context;
using WebApiUserManagement.Models;
using BCrypt.Net;

namespace WebApiUserManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context)
        {
            _context = context;
        }
        // GET: api/<UsersController>
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            var result = await _context.Users.ToListAsync();
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        // GET api/<UsersController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = _context.Users.SingleOrDefault(x => x.Id == id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        // POST api/<UsersController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User user)
        {
            //Genero un hash para la password
            string hashedPassword = HashPassword(user.PasswordHash);
            user.PasswordHash = hashedPassword;
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }
        //Metodo para generar una contraseña hasheada
        public string HashPassword(string password)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            return hashedPassword;
        }
        // Método para verificar la contraseña
        private bool VerifyPassword(string password, string hashedPassword)
        {
            bool passwordMatch = BCrypt.Net.BCrypt.Verify(password, hashedPassword);

            return passwordMatch;
        }

        // PUT api/<UsersController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] User user)
        {
            var userInfo = _context.Users.SingleOrDefault(x => x.Id == id);
            if (userInfo == null)
                return NotFound();

            userInfo.Username = user.Username;
            userInfo.Role = user.Role;
            userInfo.CreationTime = DateTime.Now;
            _context.Attach(userInfo);
            await _context.SaveChangesAsync();

            return Ok(userInfo);
        }

        // DELETE api/<UsersController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userInfo = _context.Users.SingleOrDefault(x => x.Id == id);
            if (userInfo == null)
                return NotFound();
            userInfo.IsDeleted = true;
            userInfo.DeleteTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(userInfo);
        }
        // Login de usuario
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticationRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(x => x.Username == request.Username);

            if (user == null)
                return BadRequest(new { message = "Usuario o contraseña incorrectos." });
            if (!VerifyPassword(request.Password, user.PasswordHash))
                return BadRequest(new { message = "Usuario o contraseña incorrectos." });

            return Ok(new { success = true });
        }
        public class AuthenticationRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        //Chekeo el role del usuario
        [HttpGet("isAdmin/{username}")]
        public async Task<IActionResult> isAdmin(string username)
        {
            var userInfo = _context.Users.SingleOrDefault(x => x.Username == username);
            if (userInfo == null)
                return NotFound();

            return Ok(userInfo.Role);
        }
    }
}
