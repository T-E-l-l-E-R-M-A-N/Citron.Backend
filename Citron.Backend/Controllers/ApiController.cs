using Citron.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Citron.Backend.Controllers
{
    public class ApiController : Controller
    {
        private readonly Random _rand = new Random();
        private readonly MyDbContext _dbContext;

        public ApiController(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return new JsonResult("<h2 style='color: blue'>HELLO</h2>");
        }

        [HttpPost]
        public async Task<string> RegisterAsync([FromBody] RegisterData register)
        {
            if (string.IsNullOrEmpty(register.Login)) return "none: access key";

            var user = new User()
            {
                Id = _rand.Next(),
                //Name = register.Name,
                Login = register.Login,
                Password = register.Password,
                AccessKey = Guid.NewGuid().ToString()
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            return user.AccessKey;
        }

        [HttpPost]
        public async Task<string> LoginAsync([FromBody] RegisterData data)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Login == data.Login);
            if (user == null)
                return "Invalid credentials";

            if (user.Password != data.Password)
                return "Invalid credentials";

            //await Clients.Others.SendAsync("OnUserConnected", $"{user.Name} : connected");
            return user.AccessKey;
        }

        [HttpGet]
        public async Task<User[]> GetUsersAsync()
        {
            var users = _dbContext.Users.ToArray();
            return users;
        }

        [HttpPost]
        public async Task<User> GetUserByIdAsync([FromBody]int id) 
        {
            return await _dbContext.Users.FindAsync(id);
        }

        //public async Task<Message> SendNewMessageAsync([FromBody] SendMessageBody messageBody)
        //{
        //    var room = await _dbContext.Rooms.FirstOrDefaultAsync(x => x.Members.Contains(messageBody.TargetId.ToString()));
        //    if (room == null)
        //    {

        //        var user = await _dbContext.Users.FindAsync(messageBody.UserId);
        //        var target = await _dbContext.Users.FindAsync(messageBody.TargetId);

        //        room = new Room()
        //        {
        //            Id = _rand.Next(),
        //            Members = $"{messageBody.UserId}, {messageBody.TargetId}",
        //            Name = user.Name + " + " + target.Name
        //        };

        //        await _dbContext.Rooms.AddAsync(room);
        //        await _dbContext.SaveChangesAsync();
        //    }

        //    var msg = new Message()
        //    {
        //        Id = _rand.Next(),
        //        UserId = messageBody.UserId,
        //        Room = room.Id,
        //        Text = messageBody.Text
        //    };

        //    await _dbContext.Messages.AddAsync(msg);
        //    await _dbContext.SaveChangesAsync();

        //    return msg;
        //}
    }

    public class RegisterData
    {
        public string Name { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class SendMessageBody
    {
        public int UserId { get; set; }
        public int TargetId { get; set; }
        public string Text { get; set; }
    }
}
