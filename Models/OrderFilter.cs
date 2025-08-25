namespace crmApi.Models
{
    public class OrderFilterDto
    {
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string Country { get; set; }
        public int? UserId { get; set; }
        public string PaymentStatus { get; set; }
        public string ProcessStatus { get; set; }
        public DateTime? DeliveryDateStart { get; set; }
        public DateTime? DeliveryDateEnd { get; set; }
    }

    public class FilteredOrderDto
    {
        public string Number { get; set; }
        public string CustomerName { get; set; }
        public string Country { get; set; }
        public string User { get; set; }
        public string Stage { get; set; }
        public string Control { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositPrice { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal AdvancePercentage { get; set; }
        public string DeliveryType { get; set; }
        public string CurrencyType { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? ProductionDate { get; set; }
    }
}