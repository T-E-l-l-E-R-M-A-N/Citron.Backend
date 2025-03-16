using System;
using System.Collections.Generic;
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
        private readonly SingleInstanceHelper _singleInstanceHelper;
        private readonly MyDbContext _myDbContext;

        public ApplicationHub(SingleInstanceHelper singleInstanceHelper)
        {
            _singleInstanceHelper = singleInstanceHelper;
        }
        
        public async Task ConnectAsync(string connectionId, string access_key)
        {
            string connection = $"{connectionId} : {access_key}";
            _singleInstanceHelper.Connections.Add(connection);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            int i = _singleInstanceHelper.Connections.IndexOf(_singleInstanceHelper.Connections.FirstOrDefault(x => x.Contains(Context.ConnectionId)));
            _singleInstanceHelper.Connections.RemoveAt(i);
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

        public User[] GetUsers()
        {
            var users = _myDbContext.Users.ToArray();
            return users;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _myDbContext.Users.FindAsync(id);
        }

        public Room[] GetRooms(int id)
        {
            var rooms = _myDbContext.Rooms.Where(x => x.Members.Contains(id.ToString()));
            return rooms.ToArray();
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

            var clients = new List<string>();
            var users = new List<User>();
            

            foreach(var member in room.Members.Split(", "))
            {
                var m = await _myDbContext.Users.FirstAsync(x => x.Id == int.Parse(member));
                users.Add(m);
            }

            foreach(var l in users)
            {
                var connectionId = _singleInstanceHelper.Connections.FirstOrDefault(x => x.Split(" : ").FirstOrDefault(d => d == l.AccessKey) != null);
                clients.Add(connectionId);
            }

            await Clients.Clients(clients).SendAsync("OnMessageReceived", msg);

            return msg;
        }
        
    }
}