namespace BlogWebAPIApp.Interfaces
{

    public interface ITokenService
    {
        string CreateToken(Guid userId, string username, string role);
    }

}
