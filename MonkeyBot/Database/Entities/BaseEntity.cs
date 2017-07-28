using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
    }
}