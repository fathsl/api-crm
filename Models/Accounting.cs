using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace crmApi.Models
{
    public class AccountingTransaction
    {
        [Key]
        public int TransactionID { get; set; }

        [ForeignKey("OrderInfo")]
        public int? OrderID { get; set; }

        [ForeignKey("UserInfo")]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(20)]
        public string CurrencyType { get; set; }

        public string Description { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; } = "Approved";

        public virtual OrderInfo Order { get; set; }
        public virtual UserInfo User { get; set; }
    }
}