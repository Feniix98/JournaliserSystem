using SQLite;

namespace JournalierSystem
{
    public class JournalierManager :  ModKit.ORM.ModEntity<JournalierManager>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }

        public int PlayerId { get; set; }

        public long LastUse { get; set; }   
    }
}
