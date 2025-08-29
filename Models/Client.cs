using System;
using System.Collections.Generic;

namespace crmApi.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string First_name { get; set; } = null!;
        public string Last_name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Details { get; set; }
        public string? Country { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public int? ModifiedBy { get; set; }

        public ICollection<ClientProject> ClientProjects { get; set; } = new List<ClientProject>();
    }
}
