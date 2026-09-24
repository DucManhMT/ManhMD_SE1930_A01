using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.Staff;

[Authorize(Roles = "Staff")]
public class CategoriesModel : PageModel
{
    public void OnGet()
    {
    }
}
