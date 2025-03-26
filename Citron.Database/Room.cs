using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Citron.Database
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Users { get; set; }
    }
}