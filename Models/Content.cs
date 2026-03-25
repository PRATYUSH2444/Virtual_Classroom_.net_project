using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtualClassroom2.Models
{
    public class Content
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Required]
        public int ClassroomId { get; set; }

        [ForeignKey("ClassroomId")]
        public virtual Classroom Classroom { get; set; } = null!;

        [Required]
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey("UploadedById")]
        public virtual ApplicationUser UploadedBy { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}