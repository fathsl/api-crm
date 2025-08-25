using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace crmApi.Models
{
    // User Models

    namespace crmApi.Models
{
    [Table("KullaniciBilgileri")]
    public class User
    {
        [Key]
        [Column("KullaniciID")]
        public int KullaniciID { get; set; }

        [Required]
        [StringLength(50)]
        [Column("KullaniciAdi")]
        public string KullaniciAdi { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Ad")]
        public string Ad { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Soyad")]
        public string Soyad { get; set; }

        [Required]
        [StringLength(100)]
        [Column("Sifre")]
        public string Sifre { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Column("Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        [Column("Telefon")]
        public string Telefon { get; set; }

        [Required]
        [StringLength(20)]
        [Column("Durum")]
        public string Durum { get; set; }

        [Required]
        [StringLength(30)]
        [Column("YetkiTuru")]
        public string YetkiTuru { get; set; }

        [NotMapped]
        public string FullName => $"{Ad} {Soyad}";
    }

    // DTO for creating new users (without ID)
    public class CreateUserDto
    {
        [Required]
        [StringLength(50)]
        public string KullaniciAdi { get; set; }

        [Required]
        [StringLength(50)]
        public string Ad { get; set; }

        [Required]
        [StringLength(50)]
        public string Soyad { get; set; }

        [Required]
        [StringLength(100)]
        public string Sifre { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefon { get; set; }

        [Required]
        [StringLength(20)]
        public string Durum { get; set; }

        [Required]
        [StringLength(30)]
        public string YetkiTuru { get; set; }
    }

    // DTO for updating users
    public class UpdateUserDto
    {
        [StringLength(50)]
        public string KullaniciAdi { get; set; }

        [StringLength(50)]
        public string Ad { get; set; }

        [StringLength(50)]
        public string Soyad { get; set; }

        [StringLength(100)]
        public string Sifre { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Telefon { get; set; }

        [StringLength(20)]
        public string Durum { get; set; }

        [StringLength(30)]
        public string YetkiTuru { get; set; }
    }

    // DTO for user response (without sensitive data like password)
    public class UserResponseDto
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
}

    public class UserInfo
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [StringLength(255)]
        public string Password { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}