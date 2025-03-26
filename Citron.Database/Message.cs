using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citron.Database
{
    public class Message
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public Room? Room { get; set; }
        public int RoomId { get; set; }
        public User? User { get; set; }
        public int UserId { get; set; }
    }
}