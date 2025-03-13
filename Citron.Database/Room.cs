using System.ComponentModel.DataAnnotations;

namespace Citron.Database
{
    public class Room
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Members { get; set; }
    }
}