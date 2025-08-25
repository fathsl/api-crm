using System.ComponentModel.DataAnnotations;

namespace crmApi.Models
{
    public class OrderInfo
    {
        [Key]
        public int OrderId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; }
        
        [StringLength(100)]
        public string CustomerName { get; set; }
        
        [StringLength(100)]
        public string Country { get; set; }
        
        public decimal TotalPrice { get; set; }
        
        public decimal? PaidAmount { get; set; }
        
        public decimal AdvancePercentage { get; set; }
        
        [StringLength(50)]
        public string DeliveryType { get; set; }
        
        [StringLength(10)]
        public string CurrencyType { get; set; }
        
        [StringLength(50)]
        public string PaymentStatus { get; set; }
        
        [StringLength(50)]
        public string OrderType { get; set; }
        
        [StringLength(50)]
        public string ControlStatus { get; set; }
        
        public DateTime? OrderDate { get; set; }
    }

    public class OrderDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string Country { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositPrice { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal AdvancePercentage { get; set; }
        public string DeliveryType { get; set; }
        public string CurrencyType { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderType { get; set; }
        public string OrderDate { get; set; }
    }
}