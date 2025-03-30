using System;
using System.Collections.Generic;
using System.IO;
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
                ScreenName = name,
                FirstName = name.ToLower(),
                LastName = name.ToUpper(),
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

            if (user.Password != password)
                return "Invalid credentials";

            //await Clients.Others.SendAsync("OnUserConnected", $"{user.Name} : connected");
            return user.AccessKey;
        }

        public async Task<User[]> GetUsersAsync()
        {
            var connection = _singleInstanceHelper.Connections.FirstOrDefault(x => x.Contains(Context.ConnectionId));
            var splits = connection.Split(" : ");
            var s = _myDbContext.Users.FirstOrDefault(x => x.Id == int.Parse(splits[1]));
            var users = _myDbContext.Users.Where(x => x.Id != s.Id).ToArray();
            return await Task.FromResult(users);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            var user = await _myDbContext.Users.SingleAsync(x => x.Id == id);
            return user;
        }

        public async Task<List<RoomData>> GetRoomsAsync(int id)
        {
            var user = await _myDbContext.Users.SingleAsync(x => x.Id == id);
            var rooms = _myDbContext.RoomDatas.Include(x => x.Room).Include(u => u.Users).Include(u => u.Messages).Where(x => x.Users.Contains(user));


            return rooms.ToList();
        }

        public async Task<List<Message>> GetMessages(int roomId)
        {
            var rooms = await _myDbContext.RoomDatas.Include(u => u.Room).Include(u => u.Users).Include(u => u.Messages).ToListAsync();
            try
            {
                var room = rooms.FirstOrDefault(x => x.RoomId == roomId);
                if (room != null)
                {
                    return room.Messages;
                }
            }
            catch { }
            return new List<Message>();
        }

        public async Task<RoomData> GetRoomAsync(int roomId)
        {
            var rooms = await _myDbContext.RoomDatas.Include(u => u.Room).Include(u => u.Users).Include(u => u.Messages).ToListAsync();
            var roomData = rooms.FirstOrDefault(x => x.RoomId == roomId);
            return roomData;
        }

        public async Task<User[]> SearchUsers(string name)
        {
            var users = _myDbContext.Users
                .Where(x => 
                x.ScreenName.ToLower().Contains(name.ToLower())
                );
            return await Task.FromResult(users.ToArray());
        }

        public async Task SendPrivateMessageAsync(int userId, int targetId, string text)
        {
            var user = await GetUserByIdAsync(userId);
            var target = await GetUserByIdAsync(targetId);
            var rooms = await GetRoomsAsync(userId);
            var roomData = rooms.Where(x=>x.Users.Count == 2).FirstOrDefault(x=>x.Users.Contains(user) & x.Users.Contains(target));
            if (roomData == null)
            {
                var room = new Room()
                {
                    Name = user.ScreenName + " / " + target.ScreenName
                };

                await _myDbContext.Rooms.AddAsync(room);
                await _myDbContext.SaveChangesAsync();

                roomData = new RoomData()
                {
                    Users = new List<User>()
                    {
                        user,
                        target
                    },
                    Messages =new List<Message>(),
                    RoomId = room.Id
                };

                await _myDbContext.RoomDatas.AddAsync(roomData);
                await _myDbContext.SaveChangesAsync();
            }


            var msg = new Message()
            {
                UserId = userId,
                RoomId = roomData.RoomId,
                Text = text,
                Time = DateTime.Now.ToShortTimeString()
            };

            roomData.Room.MessagesCount += 1;

            roomData.Messages.Add(msg);

            //roomData.LastMessageId = roomData.Messages.LastOrDefault().Id;
            await _myDbContext.Messages.AddAsync(msg);
            await _myDbContext.SaveChangesAsync();

            await Clients.All.SendAsync("OnMessageReceived", msg);
        }
        
    }
}