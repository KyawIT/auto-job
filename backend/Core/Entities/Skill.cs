using Core.Enums;

namespace Core.Entities
{
    public class Skill : EntityObject
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public SkillLevel? Level { get; set; }
    }
}
