using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using WebApiUserManagement.Models;

namespace WebApiUserManagement.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            try
            {   //Creacion y conexion de la db 
                var dbCreator = Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
                if (dbCreator != null)
                {
                    if (!dbCreator.CanConnect())
                    {
                        dbCreator.Create();
                    }
                    if (!dbCreator.HasTables())
                    {
                        dbCreator.CreateTables();
                    }
                }
                // Verificar si no existe un usuario con el nombre de usuario "admin"
                var adminUser = Users.FirstOrDefault(u => u.Username == "admin");
                if (adminUser == null)
                {
                    // Crear el usuario "admin" por defecto si no existe
                    Users.Add(new User
                    {
                        Username = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                        Role = "ADMIN"
                    });
                    SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
