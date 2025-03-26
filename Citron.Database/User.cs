using System.ComponentModel.DataAnnotations;

namespace Citron.Database
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string AccessKey { get; set; }
    }
}