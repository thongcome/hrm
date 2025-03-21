using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HRM.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(250)]
        public string? FirstName { get; set; }

        [MaxLength(250)]
        public string? LastName { get; set; }
    }

}
