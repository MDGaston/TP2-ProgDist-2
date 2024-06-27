using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackingService.Models
{
    // Modelo de eventos de seguimiento
    public class TrackingEventDb
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de evento es obligatorio")]
        [MaxLength(50, ErrorMessage = "La longitud máxima para el tipo de evento es de 50 caracteres")]
        public string EventType { get; set; }

        [MaxLength(255, ErrorMessage = "La longitud máxima para la URL es de 255 caracteres")]
        public string Url { get; set; }

        public DateTime Timestamp { get; set; }
    }
}