namespace crmApi.Models
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string PermissionType { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }
}