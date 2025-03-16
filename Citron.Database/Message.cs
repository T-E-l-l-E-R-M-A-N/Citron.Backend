using System;
using System.ComponentModel.DataAnnotations;

namespace Citron.Database
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Room { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }
}