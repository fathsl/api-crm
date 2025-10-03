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
        public string? City { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public string? ZipCode { get; set; }
        public string? VATNumber { get; set; }
        public string? ImageUrl { get; set; }
        public int? projectId { get; set; }
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public int? ModifiedBy { get; set; }

        public ICollection<ClientProject> ClientProjects { get; set; } = new List<ClientProject>();
        public List<int>? ProjectIds { get; set; }
    }

    public class ResourceResponse
    {
        public int Id { get; set; }
        public string? FileUrl { get; set; }
        public string? VoiceUrl { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddResourceDto
    {
        public int ClientId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
        public IFormFile? File { get; set; }
        public IFormFile? VoiceMessage { get; set; }
    }

    public class TaskResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public string? EstimatedTime { get; set; }
        public int SortOrder { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AssignedUser> AssignedUsers { get; set; } = new List<AssignedUser>();
    }

    public class AssignedUser
    {
        public int KullaniciID { get; set; }
        public int UserId { get; set; }
        public string KullaniciAdi { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string Email { get; set; }
        public string Telefon { get; set; }
        public string Durum { get; set; }
        public string YetkiTuru { get; set; }
        public string FullName { get; set; }
    }

    public class UpdateClientDto
    {
        public string First_name { get; set; } = string.Empty;
        public string Last_name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Details { get; set; }
        public string? ZipCode { get; set; }
        public string? VATNumber { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public int? projectId { get; set; }
        public List<int>? projectIds { get; set; }
        public int modifiedBy { get; set; }
    }

    public class ClientResponse
    {
        public int Id { get; set; }
        public string First_name { get; set; } = string.Empty;
        public string Last_name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Details { get; set; }
        public string? ZipCode { get; set; }
        public string? VATNumber { get; set; }
        public string? ImageUrl { get; set; }
        public int? CreatedBy { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public List<int>? projectIds { get; set; }
        public DateTime CreatedAt { get; set; }
    }


}
