using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class Fee
    {
        [Key]
        public int FeeId { get; set; }

        [Required(ErrorMessage = "Student is required.")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        public Student? Student { get; set; }

        [Required(ErrorMessage = "Semester is required.")]
        [Display(Name = "Semester")]
        public int SemesterId { get; set; }

        public Semester? Semester { get; set; }

        [Required(ErrorMessage = "Total amount is required.")]
        [Range(0, 10000000)]
        [Display(Name = "Total Fee")]
        public decimal TotalAmount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddMonths(1);

        [DataType(DataType.DateTime)]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        [NotMapped]
        public decimal PaidAmount => Payments
            .Where(p => p.Status == "Completed")
            .Sum(p => p.AmountPaid);

        [NotMapped]
        public decimal RemainingBalance => TotalAmount - PaidAmount;
    }
}
