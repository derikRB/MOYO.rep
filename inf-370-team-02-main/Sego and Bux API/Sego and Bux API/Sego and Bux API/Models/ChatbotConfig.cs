using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sego_and__Bux.Models
{
    public class ChatbotConfig
    {
        [Key] public int Id { get; set; }

        [Required] public string WhatsAppNumber { get; set; } = "";
        [Required, EmailAddress] public string SupportEmail { get; set; } = "";

        // NEW – used for distance/fees
        [Required] public string CompanyAddress { get; set; } = "";
        /// <summary>Max distance (km) for company/hand-to-hand delivery.</summary>
        public int DeliveryRadiusKm { get; set; } = 20;

        [Column(TypeName = "decimal(10,2)")]
        public decimal CourierFlatFee { get; set; } = 100m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal HandToHandFee { get; set; } = 0m;
    }
}
