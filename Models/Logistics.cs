using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace crmApi.Models
{
    public class LogisticsOperation
    {
        [Key]
        public int LogisticsID { get; set; }

        [ForeignKey("OrderInfo")]
        public int? OrderID { get; set; }

        [ForeignKey("UserInfo")]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string OperationType { get; set; }

        public string Description { get; set; }

        public DateTime OperationDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; }

        public virtual OrderInfo Order { get; set; }
        public virtual UserInfo User { get; set; }
    }
}