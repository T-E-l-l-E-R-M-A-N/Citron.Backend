using System;
using System.Linq;
using System.Threading.Tasks;
using Citron.Database;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Citron.Backend
{
    internal class ApplicationHub : Hub
    {
        private readonly Random _rand = new Random();
        private readonly MyDbContext _myDbContext;

        public ApplicationHub(MyDbContext myDbContext)
        {
            _myDbContext = myDbContext;
                _myDbContext.Database.EnsureCreated();
        }

        public async Task<string> RegisterAsync(string name, string login, string password)
        {
            if (string.IsNullOrEmpty(login)) return "none: access key";
            
            var user = new User()
            {
                Id = _rand.Next(),
                Name = name,
                Login = login,
                Password = password,
                AccessKey = Guid.NewGuid().ToString()
            };

            await _myDbContext.Users.AddAsync(user);
            await _myDbContext.SaveChangesAsync();

            return user.AccessKey;
        }

        public async Task<string> LoginAsync(string login, string password)
        {
            var user = await _myDbContext.Users.FirstOrDefaultAsync(x => x.Login == login);
            if (user == null)
                return "Invalid credentials";
            
            if(user.Password != password)
                return "Invalid credentials";

            //await Clients.Others.SendAsync("OnUserConnected", $"{user.Name} : connected");
            return user.AccessKey;
        }

        public async Task<User[]> GetUsersAsync()
        {
            var users = _myDbContext.Users.ToArray();
            return users;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _myDbContext.Users.FindAsync(id);
        }

        public async Task<Message> SendNewMessageAsync(int userId, int targetId, string text)
        {
            var room = await _myDbContext.Rooms.FirstOrDefaultAsync(x => x.Members.Contains(targetId.ToString()));
            if (room == null)
            {

                var user = await _myDbContext.Users.FindAsync(userId);
                var target = await _myDbContext.Users.FindAsync(targetId);
                
                room = new Room()
                {
                    Id = _rand.Next(),
                    Members = $"{userId}, {targetId}",
                    Name = user.Name + " + " + target.Name
                };

                await _myDbContext.Rooms.AddAsync(room);
                await _myDbContext.SaveChangesAsync();
            }

            var msg = new Message()
            {
                Id = _rand.Next(),
                UserId = userId,
                Room = room.Id,
                Text = text
            };
            
            await _myDbContext.Messages.AddAsync(msg);
            await _myDbContext.SaveChangesAsync();

            await Clients.Others.SendAsync("OnMessageReceived", msg);

            return msg;
        }
        
    }
}