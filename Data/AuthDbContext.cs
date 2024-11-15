using IdentityServer4.EntityFramework.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdSvr4.Data
{
  public class AuthDbContext : IdentityDbContext<IdentityUser>
  {

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }
  }
}
