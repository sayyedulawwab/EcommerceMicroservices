namespace Identity.Domain.Users;
public interface IUserRepository
{
    void Add(User user);
    void Update(User user);
    void Remove(User user);
    Task<User?> GetByEmail(string email);

}