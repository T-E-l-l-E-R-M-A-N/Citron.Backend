using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Citron.Database
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ScreenName { get; set; }
        public string Comment { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string AccessKey { get; set; }
    }

    public class Message
    {
        public int Id { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }
        public Room Room { get; set; }
        public int RoomId { get; set; }
        public string Text { get; set; }
        public string Time { get; set; }
    }
}