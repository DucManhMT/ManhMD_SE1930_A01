using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Services;
using ManhMD_SE1930_A01_BE.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace ManhMD_SE1930_A01_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
    }

    [HttpGet]
    public ActionResult<ODataResponse<AccountDto>> Get(ODataQueryOptions<AccountDto> queryOptions)
    {
        var result = ODataQueryHelper.ApplyOData(_accountService.GetQueryable(), queryOptions);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
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
}
