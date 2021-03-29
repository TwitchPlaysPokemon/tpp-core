using System.Runtime.Serialization;

namespace TPP.Common.PkmnModels
{
    [DataContract]
    public struct Nature
    {
        [DataMember(Name = "id")] public int Id { get; init; }
        [DataMember(Name = "name")] public string Name { get; init; }
        [DataMember(Name = "increased")] public Stat? Inc { get; init; }
        [DataMember(Name = "decreased")] public Stat? Dec { get; init; }
        [DataMember(Name = "description")] public string Description { get; init; }

        public static readonly Nature Hardy = new() { Id = 0, Name = "Hardy" };
        public static readonly Nature Lonely = new() { Id = 1, Name = "Lonely", Inc = Stat.Atk, Dec = Stat.Def };
        public static readonly Nature Brave = new() { Id = 2, Name = "Brave", Inc = Stat.Atk, Dec = Stat.Spe };
        public static readonly Nature Adamant = new() { Id = 3, Name = "Adamant", Inc = Stat.Atk, Dec = Stat.SpA };
        public static readonly Nature Naughty = new() { Id = 4, Name = "Naughty", Inc = Stat.Atk, Dec = Stat.SpD };
        public static readonly Nature Bold = new() { Id = 5, Name = "Bold", Inc = Stat.Def, Dec = Stat.Atk };
        public static readonly Nature Docile = new() { Id = 6, Name = "Docile" };
        public static readonly Nature Relaxed = new() { Id = 7, Name = "Relaxed", Inc = Stat.Def, Dec = Stat.Spe };
        public static readonly Nature Impish = new() { Id = 8, Name = "Impish", Inc = Stat.Def, Dec = Stat.SpA };
        public static readonly Nature Lax = new() { Id = 9, Name = "Lax", Inc = Stat.Def, Dec = Stat.SpD };
        public static readonly Nature Timid = new() { Id = 10, Name = "Timid", Inc = Stat.Spe, Dec = Stat.Atk };
        public static readonly Nature Hasty = new() { Id = 11, Name = "Hasty", Inc = Stat.Spe, Dec = Stat.Def };
        public static readonly Nature Serious = new() { Id = 12, Name = "Serious" };
        public static readonly Nature Jolly = new() { Id = 13, Name = "Jolly", Inc = Stat.Spe, Dec = Stat.SpA };
        public static readonly Nature Naive = new() { Id = 14, Name = "Naive", Inc = Stat.Spe, Dec = Stat.SpD };
        public static readonly Nature Modest = new() { Id = 15, Name = "Modest", Inc = Stat.SpA, Dec = Stat.Atk };
        public static readonly Nature Mild = new() { Id = 16, Name = "Mild", Inc = Stat.SpA, Dec = Stat.Def };
        public static readonly Nature Quiet = new() { Id = 17, Name = "Quiet", Inc = Stat.SpA, Dec = Stat.Spe };
        public static readonly Nature Bashful = new() { Id = 18, Name = "Bashful" };
        public static readonly Nature Rash = new() { Id = 19, Name = "Rash", Inc = Stat.SpA, Dec = Stat.SpD };
        public static readonly Nature Calm = new() { Id = 20, Name = "Calm", Inc = Stat.SpD, Dec = Stat.Atk };
        public static readonly Nature Gentle = new() { Id = 21, Name = "Gentle", Inc = Stat.SpD, Dec = Stat.Def };
        public static readonly Nature Sassy = new() { Id = 22, Name = "Sassy", Inc = Stat.SpD, Dec = Stat.Spe };
        public static readonly Nature Careful = new() { Id = 23, Name = "Careful", Inc = Stat.SpD, Dec = Stat.SpA };
        public static readonly Nature Quirky = new() { Id = 24, Name = "Quirky" };
    }
}
