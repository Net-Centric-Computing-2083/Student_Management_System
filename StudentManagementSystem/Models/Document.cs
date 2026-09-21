using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required(ErrorMessage = "Student is required.")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        public Student? Student { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Document type is required.")]
        [StringLength(50)]
        [Display(Name = "Document Type")]
        public string DocumentType { get; set; } = "Certificate";

        [Required]
        [Display(Name = "File")]
        public string FilePath { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        [Display(Name = "Uploaded Date")]
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        // Identity user (Admin) who uploaded the document.
        [Required]
        public string UploadedByUserId { get; set; } = string.Empty;
    }
}
