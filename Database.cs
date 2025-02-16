using ModKit.ORM;
using SQLite;

namespace JournalierSystem
{
    public class Cooldown_DB : ModEntity<Cooldown_DB>
    {
        [AutoIncrement]
        [PrimaryKey] public int Id { get; set; }

        public int CharacterId { get; set; }

        public long LastUsed { get; set; }
    }
}
