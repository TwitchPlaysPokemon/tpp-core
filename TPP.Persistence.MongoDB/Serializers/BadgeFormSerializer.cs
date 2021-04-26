using System.Collections.Generic;
using TPP.Persistence.Models;

namespace TPP.Persistence.MongoDB.Serializers
{
    class BadgeFormSerializer : EnumToStringUsingTranslationMappingSerializer<Badge.BadgeForm>
    {
        public static readonly BadgeFormSerializer Instance = new BadgeFormSerializer();

        private BadgeFormSerializer() : base(new Dictionary<Badge.BadgeForm, string>
        {
            [Badge.BadgeForm.Normal] = "normal",
            [Badge.BadgeForm.Shiny] = "shiny",
            [Badge.BadgeForm.Shadow] = "shadow",
            [Badge.BadgeForm.Mega] = "mega",
            [Badge.BadgeForm.Alolan] = "alolan",
            [Badge.BadgeForm.Galarian] = "galarian",
            [Badge.BadgeForm.ShinyShadow] = "shiny_shadow",
            [Badge.BadgeForm.ShinyMega] = "shiny_mega",
            [Badge.BadgeForm.ShinyAlolan] = "shiny_alolan",
            [Badge.BadgeForm.ShinyGalarian] = "shiny_galarian",
        })
        {
        }
    }
}
