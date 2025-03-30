using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Citron.Database
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MessagesCount { get; set; }
        
    }

    public class RoomData
    {
        public int Id { get; set; }
        public Room Room { get; set; }
        public int RoomId { get; set; }
        public List<User> Users { get; set; }
        public List<Message> Messages { get; set; }

    }
}