using rustles.Infrastructure.Model;

namespace rustles_platform.Services;

public static class UserDisplayName
{
    public static string Resolve(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.PermanentUsername))
            return user.PermanentUsername.Trim();

        if (!string.IsNullOrWhiteSpace(user.UserName))
            return user.UserName.Trim();

        if (!string.IsNullOrWhiteSpace(user.Email))
            return user.Email.Trim();

        return user.Id;
    }
}
