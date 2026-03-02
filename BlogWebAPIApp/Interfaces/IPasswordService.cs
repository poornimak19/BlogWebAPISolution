namespace BlogWebAPIApp.Interfaces
{

    public interface IPasswordService
    {
        /// <summary>
        /// Hashes the password. If salt/hashKey is provided, reuse it (verify); otherwise, generate a new key.
        /// Returns the derived password bytes; out hashKey is the salt/key to store with user.
        /// </summary>
        public byte[] HashPassword(string password, byte[]? existingHashKey, out byte[]? hashkey);
    }

}
