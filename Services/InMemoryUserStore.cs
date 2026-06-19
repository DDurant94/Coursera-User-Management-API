using System.Collections.Concurrent;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextId = 0;

    public IEnumerable<User> GetAll() => _users.Values;

    public bool TryGetById(int id, out User? user) => _users.TryGetValue(id, out user);

    public User Add(User user)
    {
        user.Id = Interlocked.Increment(ref _nextId);
        _users[user.Id] = user;
        return user;
    }

    public bool TryUpdate(int id, User updatedUser)
    {
        if (!_users.TryGetValue(id, out var user))
            return false;

        user.UserName = updatedUser.UserName;
        user.Email = updatedUser.Email;
        return true;
    }

    public bool TryRemove(int id) => _users.TryRemove(id, out _);
}
