using System.ComponentModel.DataAnnotations;

namespace IdSvr4.Data
{
  public class LoginInputModel
  {
    [Required]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; }

    public string ReturnUrl { get; set; }
  }
}
