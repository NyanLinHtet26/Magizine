using Magizine.Shared.Enums;
using Magizine.Shared.JsonResources;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Reader;
using Magizine.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MagizinePublic.Api.Features.Auth;

[ApiController]
[Route("api/public/auth")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshCookiePath = "/api/public/auth";
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] ReaderRegisterReqModel reqModel, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(reqModel, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            return Ok(Result<ReaderLoginResModel>.Error(result.RespCode, result.RespDesp));
        }

        WriteAuthCookies(
            result.Data.Response.AccessToken,
            result.Data.Response.ExpiresAt,
            result.Data.RawRefreshToken,
            result.Data.Response.RefreshTokenExpiresAt);

        return Ok(new Result<ReaderLoginResModel>
        {
            Data = result.Data.Response,
            RespCode = JsonResource.Success,
            RespDesp = result.RespDesp,
            RespType = EnumRespType.Success
        });
    }

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] ReaderLoginReqModel reqModel, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(reqModel, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            return Ok(Result<ReaderLoginResModel>.Error(result.RespCode, result.RespDesp));
        }

        WriteAuthCookies(
            result.Data.Response.AccessToken,
            result.Data.Response.ExpiresAt,
            result.Data.RawRefreshToken,
            result.Data.Response.RefreshTokenExpiresAt);

        return Ok(new Result<ReaderLoginResModel>
        {
            Data = result.Data.Response,
            RespCode = JsonResource.Success,
            RespDesp = result.RespDesp,
            RespType = EnumRespType.Success
        });
    }

    [AllowAnonymous]
    [HttpPost("Refresh")]
    public async Task<IActionResult> Refresh([FromBody] ReaderRefreshTokenReqModel? reqModel, CancellationToken ct)
    {
        var rawRefreshToken = Request.Cookies[MagizineClaims.ReaderRefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            rawRefreshToken = reqModel?.RefreshToken;
        }

        var result = await _authService.RefreshTokenAsync(rawRefreshToken, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            ClearAuthCookies();
            return Ok(Result<ReaderLoginResModel>.Error(result.RespCode, result.RespDesp));
        }

        WriteAuthCookies(
            result.Data.Response.AccessToken,
            result.Data.Response.ExpiresAt,
            result.Data.RawRefreshToken,
            result.Data.Response.RefreshTokenExpiresAt);

        return Ok(new Result<ReaderLoginResModel>
        {
            Data = result.Data.Response,
            RespCode = JsonResource.Success,
            RespDesp = result.RespDesp,
            RespType = EnumRespType.Success
        });
    }

    [AllowAnonymous]
    [HttpPost("Logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var rawRefreshToken = Request.Cookies[MagizineClaims.ReaderRefreshTokenCookie];
        var result = await _authService.LogoutAsync(rawRefreshToken, ct);

        ClearAuthCookies();
        return Ok(result);
    }

    [Authorize]
    [HttpGet("Me")]
    [HttpPost("Me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await _authService.GetCurrentReaderProfileAsync(ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("UpdateProfile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateReaderProfileReqModel reqModel, CancellationToken ct)
    {
        var result = await _authService.UpdateProfileAsync(reqModel, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeReaderPasswordReqModel reqModel, CancellationToken ct)
    {
        var result = await _authService.ChangePasswordAsync(reqModel, ct);
        if (result.IsSuccess)
        {
            ClearAuthCookies();
        }

        return Ok(result);
    }

    private void WriteAuthCookies(
        string accessToken,
        DateTimeOffset accessExpiresAt,
        string? rawRefreshToken,
        DateTimeOffset refreshExpiresAt)
    {
        var isSecure = Request.IsHttps;

        Response.Cookies.Append(
            MagizineClaims.ReaderAccessTokenCookie,
            accessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = accessExpiresAt,
                IsEssential = true
            });

        // Only write a new refresh cookie on Register, Login, or Standard Rotation.
        // On a 30s Grace Period Hit, rawRefreshToken is null so the browser's already-rotated cookie stays intact.
        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            Response.Cookies.Append(
                MagizineClaims.ReaderRefreshTokenCookie,
                rawRefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = isSecure,
                    SameSite = SameSiteMode.Strict,
                    Path = RefreshCookiePath,
                    Expires = refreshExpiresAt,
                    IsEssential = true
                });
        }
    }

    private void ClearAuthCookies()
    {
        var isSecure = Request.IsHttps;

        Response.Cookies.Delete(
            MagizineClaims.ReaderAccessTokenCookie,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });

        Response.Cookies.Delete(
            MagizineClaims.ReaderRefreshTokenCookie,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Strict,
                Path = RefreshCookiePath
            });
    }
}
