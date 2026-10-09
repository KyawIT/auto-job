using Core.Contracts;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public abstract class EntityObject : IEntityObject
    {
        [Key]
        public int Id { get; set; }

        // Set automatically in ApplicationDBContext.SaveChanges (UTC).
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // SQLite can't generate rowversion bytes, so a new Guid is set on every save.
        // Concurrent edits (e.g. batch worker vs. user) then fail instead of overwriting each other.
        [ConcurrencyCheck]
        public Guid RowVersion { get; set; }
    }
}
