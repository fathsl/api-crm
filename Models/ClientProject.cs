namespace crmApi.Models
{
    public class ClientProject
    {
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
    }
}
