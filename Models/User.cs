using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataContextContainer
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string UserName { get; set; }

        public virtual UserConfig Config { get; set; }
    }

    public class UserConfig
    {
        [Key]
        public int UserId { get; set; }

        public string ConnectionString { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
    }
}