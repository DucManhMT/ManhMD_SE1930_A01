using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ReportsModel : PageModel
{
    public void OnGet()
    {
    }
}
