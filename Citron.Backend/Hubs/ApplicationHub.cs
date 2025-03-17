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
        

        public ApplicationHub(SingleInstanceHelper singleInstanceHelper, MyDbContext myDbContext)
        {
            _singleInstanceHelper = singleInstanceHelper;
            _myDbContext = myDbContext;
        }

        public async Task<int> ConnectAsync(string connectionId, string access_key)
        {
            var user = await _myDbContext.Users.FirstOrDefaultAsync(x => x.AccessKey == access_key);
            string connection = $"{connectionId} : {user.Id}";
            _singleInstanceHelper.Connections.Add(connection);
            return user.Id;
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

        public async Task<Room[]> GetRooms(int id)
        {
            var rooms = _myDbContext.Rooms.Where(x => x.Members.Contains(id.ToString()));
            return rooms.ToArray();
        }

        public async Task<Message[]> GetMessagesAsync(int roomId)
        {
            var messages = _myDbContext.Messages.Where(x => x.Room == roomId).OrderBy(x=>x.Date);
            return messages.ToArray();
        }

        public async Task SendNewMessageAsync(int userId, int targetId, string text)
        {
            var room = await _myDbContext.Rooms.FirstOrDefaultAsync(x => x.Members.Contains(targetId.ToString()) & x.Members.Contains(userId.ToString()));
            if (room == null)
            {
                room = await _myDbContext.Rooms.FirstOrDefaultAsync(x => x.Id == targetId);
                if(room == null)
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
            }

            var msg = new Message()
            {
                Id = _rand.Next(),
                UserId = userId,
                Room = room.Id,
                Text = text,
                Date = DateTime.Now
            };
            
            await _myDbContext.Messages.AddAsync(msg);
            await _myDbContext.SaveChangesAsync();

            var clients = new List<string>();
            var users = new List<User>();
            

            if(_singleInstanceHelper.Connections.Count != 0)
            {
                foreach (var member in room.Members.Split(", "))
                {
                    var m = await _myDbContext.Users.FirstAsync(x => x.Id == int.Parse(member));
                    users.Add(m);
                }

                foreach (var l in users)
                {
                    try
                    {
                        string connectionId = "";
                        foreach(var connection in _singleInstanceHelper.Connections)
                        {
                            if (connection.Split(" : ").FirstOrDefault(d => d == l.Id.ToString()) is string g)
                            {
                                if (_singleInstanceHelper.Connections.FirstOrDefault(x => x.Contains(g)) is string connection2)
                                {
                                    connectionId = connection2.Split(" : ").FirstOrDefault();
                                }
                            }
                        }
                        
                        clients.Add(connectionId);
                    }
                    catch (Exception e) { Console.Write(e); }
                }

                await Clients.Clients(clients).SendAsync("OnMessageReceived", msg);
            }
        }
        
    }
}