using System.ComponentModel.DataAnnotations;

namespace crmApi.Models
{
    public class DeliveryInfo
    {
        [Key]
        public int DeliveryId { get; set; }

        public int CustomerId { get; set; }

        public int Quantity { get; set; }

        // Navigation property
        public virtual CustomerInfo Customer { get; set; }
    }

    public class DeliveryDto
    {
        public int CustomerId { get; set; }
        public int Quantity { get; set; }
        public string CustomerName { get; set; }
    }
}