namespace BlogWebAPIApp.Interfaces
{

    public interface ITokenService
    {
        string CreateToken(Guid userId, string username, string role);


        // NEW: Short-lived JWT for password reset (no DB)
        string GeneratePasswordResetToken(Guid userId, TimeSpan ttl);
        Guid ValidatePasswordResetToken(string token); // throws if invalid/expired/purpose mismatch

    }

}
