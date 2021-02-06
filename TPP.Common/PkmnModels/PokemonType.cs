using System.Runtime.Serialization;

namespace TPP.Common.PkmnModels
{
    /// Purposely not just named "Type" to avoid confusion with System.Type
    [DataContract]
    public enum PokemonType
    {
        [EnumMember(Value = "Normal")] Normal,
        [EnumMember(Value = "Fire")] Fire,
        [EnumMember(Value = "Water")] Water,
        [EnumMember(Value = "Electric")] Electric,
        [EnumMember(Value = "Grass")] Grass,
        [EnumMember(Value = "Ice")] Ice,
        [EnumMember(Value = "Fighting")] Fighting,
        [EnumMember(Value = "Poison")] Poison,
        [EnumMember(Value = "Ground")] Ground,
        [EnumMember(Value = "Flying")] Flying,
        [EnumMember(Value = "Psychic")] Psychic,
        [EnumMember(Value = "Bug")] Bug,
        [EnumMember(Value = "Rock")] Rock,
        [EnumMember(Value = "Ghost")] Ghost,
        [EnumMember(Value = "Dragon")] Dragon,
        [EnumMember(Value = "Dark")] Dark,
        [EnumMember(Value = "Steel")] Steel,
        [EnumMember(Value = "Fairy")] Fairy,
        [EnumMember(Value = "???")] QuestionMarks,
    }

    public static class PokemonTypeExtensions
    {
        public static string? GetTypeName(this PokemonType type) => type.GetEnumMemberValue();
    }
}
