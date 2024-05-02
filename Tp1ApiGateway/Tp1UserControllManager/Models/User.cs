using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApiUserManagement.Context;

namespace WebApiUserManagement.Models
{
    //Modelo de la db de Usuarios
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public  int Id { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [MaxLength(50, ErrorMessage = "El nombre de usuario no puede tener más de 50 caracteres")]
        [UniqueUsername(ErrorMessage = "Este nombre de usuario ya está en uso")]
        public  string  Username { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MaxLength(100, ErrorMessage = "La longitud máxima de la contraseña es de 100 caracteres")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        [StringLength(5, ErrorMessage = "La longitud máxima del rol es de 5 caracteres")]
        [ValidRole(ErrorMessage = "El rol debe ser 'ADMIN' o 'USER'")]
        public string Role { get; set; }
        public DateTime CreationTime { get; set;}

        public bool IsDeleted { get; set; } = false;
        public DateTime DeleteTime { get; set;} = DateTime.MinValue;
    }
    //Verificacion de no repetidos para username
    public class UniqueUsername : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var dbContext = (AppDbContext)validationContext.GetService(typeof(AppDbContext));
            var username = (string)value;

            var existingUser = dbContext.Users.FirstOrDefault(u => u.Username == username);
            if (existingUser != null)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
    //Verificacion de que el rol ingresado sea valido
    public class ValidRoleAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var role = value as string;
            if (role != "ADMIN" && role != "USER")
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
}
