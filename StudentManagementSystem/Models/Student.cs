using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2,
           ErrorMessage = "Name must be between 2 and 100 characters.")]
        [Display(Name = "Full Name")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(97|98)\d{8}$",
            ErrorMessage = "Enter a valid 10-digit Nepali mobile number.")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, MinimumLength = 3,
            ErrorMessage = "Address must be between 3 and 200 characters.")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Please select a gender.")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        [CustomValidation(typeof(Student), nameof(ValidateDateOfBirth))]
        public DateTime DateOfBirth { get; set; }

        public static ValidationResult ValidateDateOfBirth(
       DateTime dateOfBirth,
       ValidationContext context)
        {
            if (dateOfBirth > DateTime.Today)
            {
                return new ValidationResult(
                    "Date of birth cannot be in the future.");
            }

            if (dateOfBirth < DateTime.Today.AddYears(-100))
            {
                return new ValidationResult(
                    "Please enter a valid date of birth.");
            }

            return ValidationResult.Success;
        }

        [Required(ErrorMessage = "Department is required.")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
    }
}

