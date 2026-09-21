using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required(ErrorMessage = "Fee record is required.")]
        [Display(Name = "Fee")]
        public int FeeId { get; set; }

        public Fee? Fee { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 10000000)]
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Payment method is required.")]
        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        [Required]
        [StringLength(50)]
        [Display(Name = "Reference / Receipt No.")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Completed";
    }
}
