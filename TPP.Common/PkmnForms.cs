using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TPP.Common;

namespace TPP.Common
{
    public static class PkmnForms
    {
        /// <summary>
        /// 
        /// </summary>
        static readonly Dictionary<string, Dictionary<string, int>> Forms = new Dictionary<string, Dictionary<string, int>>
        {
            ["unown"] = new Dictionary<string, int>
            {
                ["a"] = 1,
                ["b"] = 2,
            },
            ["shellos"] = new Dictionary<string, int>
            {
                ["west sea"] = 1,
                ["west"] = 1,
                ["pink"] = 1,
                ["east sea"] = 2,
                ["east"] = 2,
                ["blue"] = 2,
            },
        };

        public static string getFormName(PkmnSpecies pokemon, int formid)
        {
            string pkmnName = pokemon.Name.ToLower();
            Dictionary<string, int>? forms;
            if (!Forms.TryGetValue(pkmnName, out forms))
                throw new ArgumentException($"{pokemon.Name} does not have alternate forms.");
            string formName = forms.FirstOrDefault(p => p.Value == formid).Key;
            if (formName == null)
                throw new ArgumentException($"{pokemon.Name} does not have a form with id {formid}.");
            return formName;
        }

        public static int getFormId(PkmnSpecies pokemon, string formName)
        {
            string pkmnName = pokemon.Name.ToLower();
            Dictionary<string, int>? forms;
            if (!Forms.TryGetValue(pkmnName, out forms))
                throw new ArgumentException($"{pokemon.Name} does not have alternate forms.");
            int formid = forms.GetValueOrDefault(formName.ToLower());
            if (formid == 0)
                throw new ArgumentException($"{pokemon.Name} does not have a form called {formName}.");
            return formid;
        }

        public static bool pokemonHasForms(PkmnSpecies pokemon)
        {
            return Forms.ContainsKey(pokemon.Name.ToLower());
        }
    }
}
