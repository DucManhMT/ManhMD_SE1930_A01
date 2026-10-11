using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Entities;

namespace FUNews.BusinessLogic.Helpers;

public static class AccountMappingHelper
{
    public static string GetRoleName(int? role) => role switch
    {
        1 => "Staff",
        2 => "Lecturer",
        _ => "Unknown"
    };

    public static AccountDto ToDto(SystemAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new AccountDto
        {
            AccountId = account.AccountID,
            AccountName = account.AccountName,
            AccountEmail = account.AccountEmail,
            AccountRole = account.AccountRole,
            RoleName = GetRoleName(account.AccountRole)
        };
    }
}
