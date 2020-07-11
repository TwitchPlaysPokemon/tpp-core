using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public struct Nature
    {
        [DataMember(Name = "id")] public int Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "increased")] public Stat? Inc { get; set; }
        [DataMember(Name = "decreased")] public Stat? Dec { get; set; }
        [DataMember(Name = "description")] public string Description { get; set; }

        public static Nature Hardy = new Nature { Id = 0, Name = "Hardy" };
        public static Nature Lonely = new Nature { Id = 1, Name = "Lonely", Inc = Stat.Atk, Dec = Stat.Def };
        public static Nature Brave = new Nature { Id = 2, Name = "Brave", Inc = Stat.Atk, Dec = Stat.Spe };
        public static Nature Adamant = new Nature { Id = 3, Name = "Adamant", Inc = Stat.Atk, Dec = Stat.SpA };
        public static Nature Naughty = new Nature { Id = 4, Name = "Naughty", Inc = Stat.Atk, Dec = Stat.SpD };
        public static Nature Bold = new Nature { Id = 5, Name = "Bold", Inc = Stat.Def, Dec = Stat.Atk };
        public static Nature Docile = new Nature { Id = 6, Name = "Docile" };
        public static Nature Relaxed = new Nature { Id = 7, Name = "Relaxed", Inc = Stat.Def, Dec = Stat.Spe };
        public static Nature Impish = new Nature { Id = 8, Name = "Impish", Inc = Stat.Def, Dec = Stat.SpA };
        public static Nature Lax = new Nature { Id = 9, Name = "Lax", Inc = Stat.Def, Dec = Stat.SpD };
        public static Nature Timid = new Nature { Id = 10, Name = "Timid", Inc = Stat.Spe, Dec = Stat.Atk };
        public static Nature Hasty = new Nature { Id = 11, Name = "Hasty", Inc = Stat.Spe, Dec = Stat.Def };
        public static Nature Serious = new Nature { Id = 12, Name = "Serious" };
        public static Nature Jolly = new Nature { Id = 13, Name = "Jolly", Inc = Stat.Spe, Dec = Stat.SpA };
        public static Nature Naive = new Nature { Id = 14, Name = "Naive", Inc = Stat.Spe, Dec = Stat.SpD };
        public static Nature Modest = new Nature { Id = 15, Name = "Modest", Inc = Stat.SpA, Dec = Stat.Atk };
        public static Nature Mild = new Nature { Id = 16, Name = "Mild", Inc = Stat.SpA, Dec = Stat.Def };
        public static Nature Quiet = new Nature { Id = 17, Name = "Quiet", Inc = Stat.SpA, Dec = Stat.Spe };
        public static Nature Bashful = new Nature { Id = 18, Name = "Bashful" };
        public static Nature Rash = new Nature { Id = 19, Name = "Rash", Inc = Stat.SpA, Dec = Stat.SpD };
        public static Nature Calm = new Nature { Id = 20, Name = "Calm", Inc = Stat.SpD, Dec = Stat.Atk };
        public static Nature Gentle = new Nature { Id = 21, Name = "Gentle", Inc = Stat.SpD, Dec = Stat.Def };
        public static Nature Sassy = new Nature { Id = 22, Name = "Sassy", Inc = Stat.SpD, Dec = Stat.Spe };
        public static Nature Careful = new Nature { Id = 23, Name = "Careful", Inc = Stat.SpD, Dec = Stat.SpA };
        public static Nature Quirky = new Nature { Id = 24, Name = "Quirky" };
    }
}
