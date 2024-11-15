using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdSvr4.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace IdSvr4
{
  public class Startup
  {

    public List<string> allAuthURLs = new List<string>
    {
      "https://localhost:5001",
      "https://localhost:5001/",
      "https://localhost:5001/signin-callback.html",     
      "https://localhost:5001/signin-silent-callback.html"
    };

    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }


    public void ConfigureServices(IServiceCollection services)
    {
      services.AddDbContext<AuthDbContext>(options =>
      {
        options.UseSqlite("Filename=AuthDb.db");
      });

      services.AddIdentity<IdentityUser, IdentityRole>()
          .AddEntityFrameworkStores<AuthDbContext>()
          .AddDefaultTokenProviders();

      services.Configure<IdentityOptions>(x =>
      {
        x.Password.RequireNonAlphanumeric = false;
        x.Password.RequireDigit = false;
        x.Password.RequireLowercase = false;
      });

      // IdentityServer4
      services.AddIdentityServer((options) =>
      {
        options.UserInteraction = new UserInteractionOptions
        {
          LoginUrl = "/login.html",
          LogoutUrl = "/api/Account/Logout"
        };

        // Sliding cookie on 24 hrs
        options.Authentication.CookieLifetime = TimeSpan.FromHours(24);
        options.Authentication.CookieSlidingExpiration = true;
      })
          .AddDeveloperSigningCredential()
          .AddInMemoryIdentityResources(new List<IdentityResource>()
          {
                  new IdentityResources.OpenId(),
                  new IdentityResources.Profile(),
                  new IdentityResources.Email()
          })
          .AddInMemoryApiScopes(new List<ApiScope>() { new ApiScope("spa_using_implicit") })
          .AddInMemoryClients(new List<Client>()
          {
            new Client
            {
              ClientId = "spa_client",
              ClientName = "SPA Client",
              AllowAccessTokensViaBrowser = true,
              RequireConsent = false,
              AccessTokenLifetime = 90, // Short for Dev testing
              RedirectUris = allAuthURLs,
              PostLogoutRedirectUris = allAuthURLs,
              AlwaysIncludeUserClaimsInIdToken = false,
              AlwaysSendClientClaims = true,
              AllowedCorsOrigins = new List<string> { "https://localhost:5001" },
              AllowedGrantTypes = GrantTypes.Implicit,
              AllowedScopes = {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "spa_using_implicit"
              }
            }
          })
          .AddAspNetIdentity<IdentityUser>();

      services.AddAuthentication();
      services.AddCors();
      services.AddControllers();
      
      services.AddSpaStaticFiles(configuration =>
      {
        configuration.RootPath = "wwwroot";
      });
    }


    public void Configure(IApplicationBuilder app, IServiceScopeFactory serviceScopeFactory)
    {
      app.UseDeveloperExceptionPage();

      app.UseHttpsRedirection();

      app.UseRouting();
      app.UseCors(builder =>
        builder
          .AllowAnyMethod()
          .AllowAnyHeader()
          .WithOrigins(allAuthURLs.ToArray())
          .AllowCredentials());

      app.UseIdentityServer();
      app.UseAuthentication();
      app.UseAuthorization();
      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });

      app.UseFileServer();

      var scope = serviceScopeFactory.CreateScope();
      var dbCtx = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

      dbCtx.Database.EnsureDeleted();
      dbCtx.Database.EnsureCreated();

      Task.Run((Func<Task>)(async () =>
      {
        var user = new IdentityUser
        {
          Email = "USER@GMAIL.COM",
          UserName = "USER@GMAIL.COM"
        };

        var exists = dbCtx.Users.Any(x => x.Email == user.Email);
        if (exists) { 
          dbCtx.Users.Remove(user); 
          await dbCtx.SaveChangesAsync();
        }

          var uMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
          await uMgr.CreateAsync(user, "SECRETPASS");
      }));

    }
  }
}
