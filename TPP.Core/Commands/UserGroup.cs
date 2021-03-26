using System;

namespace TPP.Core.Commands
{
    [Flags]
    public enum UserGroup : byte
    {
        None = 0,
        Operator = 1,
        Moderator = 2,
        Trusted = 4,
        MusicTeam = 8,

        ModTeam = Operator | Moderator 
    }
}
