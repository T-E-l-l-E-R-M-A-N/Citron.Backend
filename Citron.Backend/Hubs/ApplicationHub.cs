using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (user == null)
                return -1;
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
            var user = await _myDbContext.Users.SingleAsync(x => x.Login == login);
            if (user == null)
                return "Invalid credentials";
            
            if(user.Password != password)
                return "Invalid credentials";

            //await Clients.Others.SendAsync("OnUserConnected", $"{user.Name} : connected");
            return user.AccessKey;
        }

        public async Task<User[]> GetUsersAsync()
        {
            var connection = _singleInstanceHelper.Connections.FirstOrDefault(x => x.Contains(Context.ConnectionId));
            var splits = connection.Split(" : ");
            var s = _myDbContext.Users.FirstOrDefault(x => x.Id == int.Parse(splits[1]));
            var users = _myDbContext.Users.Where(x=>x.Id != s.Id).ToArray();
            return await Task.FromResult(users);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _myDbContext.Users.SingleAsync(x => x.Id == id);
        }

        public async Task<Room[]> GetRoomsAsync(int id)
        {
            var user = await _myDbContext.Users.SingleAsync(x=>x.Id == id);
            var rooms = _myDbContext.Rooms.Where(x => x.Users.Contains(user.Id.ToString()));
            var res = rooms.Select(u => new Room()
            {
                Id = u.Id,
                Name = u.Name.Replace(user.Name, "").Replace(" + ", ""),
                Users = u.Users
            }).ToArray();
            return res;
        }

        public async Task<Message[]> GetMessages(int roomId)
        {
            var messages = _myDbContext.Messages
                .Include(u => u.Room)
                .Include(u =>u.User)
                .Where(x=>x.RoomId == roomId);
            return await Task.FromResult(messages.ToArray());
        }

        public async Task<User[]> SearchUsers(string name)
        {
            var users = _myDbContext.Users
                .Where(x => 
                x.Name.ToLower().Contains(name.ToLower())
                );
            return await Task.FromResult(users.ToArray());
        }

        public async Task SendPrivateMessageAsync(int userId, int targetId, string text)
        {
            var user = await GetUserByIdAsync(userId);
            var target = await GetUserByIdAsync(targetId);
            var rooms = await GetRoomsAsync(userId);
            var room = rooms.Where(x=>x.Users.Split(", ").Length == 2).FirstOrDefault(x=>x.Users.Contains(user.Id.ToString()) & x.Users.Contains(target.Id.ToString()));
            if (room == null)
            {
                room = new Room()
                {
                    Users = new StringBuilder().Append(user.Id).Append(", ").Append(target.Id).ToString(),
                    Name = user.Name + " + " + target.Name
                };

                await _myDbContext.Rooms.AddAsync(room);
                await _myDbContext.SaveChangesAsync();
            }

            var msg = new Message()
            {
                UserId = userId,
                RoomId = room.Id,
                Text = text,
                Date = DateTime.Now
            };
            
            await _myDbContext.Messages.AddAsync(msg);
            await _myDbContext.SaveChangesAsync();

            var clients = new List<string>();
            var users = new List<User>();
            var roomUsers = (room as Room).Users.Split(", ").Select(x=>GetUserByIdAsync(int.Parse(x)).Result);

            if (_singleInstanceHelper.Connections.Count != 0)
            {
                foreach (var member in roomUsers)
                {
                    var m = await _myDbContext.Users.FirstAsync(x => x.Id == member.Id);
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