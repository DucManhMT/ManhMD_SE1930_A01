using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Services;
using ManhMD_SE1930_A01_BE.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace ManhMD_SE1930_A01_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public ActionResult<ODataResponse<AccountDto>> Get(ODataQueryOptions<AccountDto> queryOptions)
    {
        var result = ODataQueryHelper.ApplyOData(_accountService.GetQueryable(), queryOptions);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AccountDto>> GetById(short id, CancellationToken cancellationToken)
    {
        var account = await _accountService.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy tài khoản",
                Detail = $"Tài khoản với mã {id} không tồn tại."
            });
        }

        return Ok(account);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AccountDto>> Create([FromBody] CreateAccountRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = await _accountService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.AccountId }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AccountDto>> Update(short id, [FromBody] UpdateAccountRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _accountService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(short id, CancellationToken cancellationToken)
    {
        await _accountService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Roles = "Staff,Lecturer")]
    public async Task<ActionResult<AccountDto>> GetProfile(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (!accountId.HasValue)
        {
            return Forbid();
        }

        var profile = await _accountService.GetProfileAsync(accountId.Value, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Staff,Lecturer")]
    public async Task<ActionResult<AccountDto>> UpdateProfile([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (!accountId.HasValue)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _accountService.UpdateProfileAsync(accountId.Value, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("me/change-password")]
    [Authorize(Roles = "Staff,Lecturer")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (!accountId.HasValue)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await _accountService.ChangePasswordAsync(accountId.Value, request, cancellationToken);
        return NoContent();
    }

    private short? GetCurrentAccountId()
    {
        var claimVal = User.FindFirst("accountId")?.Value
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return short.TryParse(claimVal, out var id) ? id : null;
    }
}

