using System.Collections.Generic;
using TPP.Persistence.Models;

namespace TPP.Persistence.MongoDB.Serializers
{
    class RoleSerializer : EnumToStringUsingTranslationMappingSerializer<Role>
    {
        public static readonly RoleSerializer Instance = new RoleSerializer();

        private RoleSerializer() : base(new Dictionary<Role, string>
        {
            [Role.Operator] = "operator",
            [Role.Moderator] = "moderator",
            [Role.Trusted] = "trusted",
            [Role.MusicTeam] = "musicteam",
        })
        {
        }
    }
}
