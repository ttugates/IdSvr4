using IdentityServer4.Services;
using IdSvr4.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace IdSvr4.Controllers
{
  [Route("api/[controller]")]
  public class AccountController : ControllerBase
  {
    private readonly IIdentityServerInteractionService _interaction;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(
        IIdentityServerInteractionService interaction,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager
        )
    {
      _interaction = interaction;
      _signInManager = signInManager;
      _userManager = userManager;
    }


    [HttpGet("ExternalLogin"), ProducesResponseType(302)]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string returnUrl = null)
    {
      var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
      var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
      return Challenge(properties, provider);
    }

    [HttpGet("ExternalLoginCallback"), ProducesResponseType(302)]
    public async Task<IActionResult> ExternalLoginCallback(
      [FromQuery] string returnUrl = null, [FromQuery] string remoteError = null)
    {
      if (remoteError != null)
        return BadRequest(remoteError);

      var info = await _signInManager.GetExternalLoginInfoAsync();
      if (info == null)
        return BadRequest();

      var result = await _signInManager
        .ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
      if (result.Succeeded)
        return Redirect(returnUrl);

      return Redirect("/error?error=\"This external account not linked to your account.  Please log in and set up External Account first.\"");
    }

    [HttpGet("LinkLogin"), ProducesResponseType(302)]
    public IActionResult LinkLogin([FromQuery] string provider)
    {
      {
        // Request a redirect to the external login provider to link a login for the current user
        var redirectUrl = Url.Action("LinkLoginCallback", "Account");
        var properties = _signInManager
          .ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
        return Challenge(properties, provider);
      }
    }

    [HttpGet("LinkLoginCallback"), ProducesResponseType(302)]
    public async Task<ActionResult> LinkLoginCallback()
    {
      {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
          return BadRequest();

        var info = await _signInManager
          .GetExternalLoginInfoAsync(await _userManager.GetUserIdAsync(user));

        if (info == null)
          return BadRequest();

        var result = await _userManager.AddLoginAsync(user, info);
        return Redirect("/account");
      }
    }

    [HttpPost("Login"), ProducesResponseType(typeof(SignInResult), 200)]
    public async Task<IActionResult> Login([FromBody] LoginInputModel model)
    {
      if (!ModelState.IsValid)
        return BadRequest();

      var result = await _signInManager
        .PasswordSignInAsync(model.Email, model.Password, true, false);

      return result.Succeeded
          ? Ok(result)
          : StatusCode(401, result);
    }

    [HttpGet("Logout"), ProducesResponseType(302)]
    public async Task<IActionResult> Logout([FromQuery] string logoutId)
    {
      await _signInManager.SignOutAsync();

      var logoutRequest = await _interaction.GetLogoutContextAsync(logoutId);

      return String.IsNullOrEmpty(logoutRequest.PostLogoutRedirectUri)
       ? Redirect("/")
       : Redirect(logoutRequest.PostLogoutRedirectUri);

    }
  }
}