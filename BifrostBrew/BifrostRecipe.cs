using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BifrostBrew
{
    // Server-synced recipe settings for the mead base and the fermenter yield. Changes apply live
    // (including values synced from a server on join): the registered Recipe and the fermenters'
    // item conversions are updated in place.
    internal static class BifrostRecipe
    {
        public const string MeadBaseName = "MeadBaseBifrost";

        private const string DefaultIngredients = "Honey:10, GreydwarfEye:5, SurtlingCore:1";
        private const string DefaultStation = "piece_MeadCauldron"; // Mead Ketill
        private const int DefaultBrewsPerBase = 6;

        public static ConfigEntry<string> Ingredients;
        public static ConfigEntry<string> Station;
        public static ConfigEntry<int> BrewsPerBase;

        // The Recipe Jotunn registered for the mead base; the same instance lives in ObjectDB.
        private static Recipe s_recipe;

        public static void Bind(ConfigFile config, ConfigurationManagerAttributes adminOnly)
        {
            Ingredients = config.Bind("Recipe", "Mead base ingredients", DefaultIngredients,
                new ConfigDescription("Ingredients for one Mead base: Bifrost, as ItemName:Amount pairs separated by commas. " +
                                      "Use internal item names (e.g. Honey, GreydwarfEye, SurtlingCore, Thistle). Unknown names are skipped.",
                    null, adminOnly));

            Station = config.Bind("Recipe", "Mead base station", DefaultStation,
                new ConfigDescription("Crafting station for the mead base, by prefab name: piece_MeadCauldron (Mead Ketill), " +
                                      "piece_cauldron (Cauldron), piece_workbench (Workbench). Leave empty to craft it anywhere.",
                    null, adminOnly));

            BrewsPerBase = config.Bind("Recipe", "Brews per base", DefaultBrewsPerBase,
                new ConfigDescription("Bifrost Brews the fermenter produces from one mead base.",
                    new AcceptableValueRange<int>(1, 50), adminOnly));

            Ingredients.SettingChanged += (_, __) => ApplyRecipe();
            Station.SettingChanged += (_, __) => ApplyRecipe();
            BrewsPerBase.SettingChanged += (_, __) => ApplyYield();
        }

        // Requirements for the initial ItemConfig, validated so Jotunn never gets an unknown item.
        public static RequirementConfig[] GetRequirementConfigs()
        {
            return ResolveIngredients().Select(i => new RequirementConfig(i.item.name, i.amount)).ToArray();
        }

        // Station for the initial ItemConfig; null means no station.
        public static string GetStationName()
        {
            if (TryFindStation(Station.Value, out CraftingStation station))
                return station != null ? station.name : null;

            Jotunn.Logger.LogWarning($"Unknown crafting station '{Station.Value}', using {DefaultStation}");
            return DefaultStation;
        }

        public static void Attach(CustomItem meadBase)
        {
            s_recipe = meadBase.Recipe?.Recipe;
        }

        private static void ApplyRecipe()
        {
            if (s_recipe == null)
                return;

            s_recipe.m_resources = ResolveIngredients()
                .Select(i => new Piece.Requirement { m_resItem = i.item, m_amount = i.amount, m_amountPerLevel = 0 })
                .ToArray();

            if (TryFindStation(Station.Value, out CraftingStation station))
                s_recipe.m_craftingStation = station;
            else
                Jotunn.Logger.LogWarning($"Unknown crafting station '{Station.Value}', keeping the current one");
        }

        // Updates the conversion on the fermenter prefab and on fermenters already loaded in the
        // world (each instance holds its own copy of the conversion list).
        private static void ApplyYield()
        {
            foreach (Fermenter fermenter in Resources.FindObjectsOfTypeAll<Fermenter>())
            {
                foreach (Fermenter.ItemConversion conversion in fermenter.m_conversion)
                {
                    if (conversion.m_from != null && conversion.m_from.name == MeadBaseName)
                        conversion.m_producedItems = BrewsPerBase.Value;
                }
            }
        }

        // Parses "Name:Amount, Name:Amount". Falls back to the default recipe if nothing is valid,
        // so the mead base always has ingredients.
        private static List<(ItemDrop item, int amount)> ResolveIngredients()
        {
            var result = Parse(Ingredients.Value, logProblems: true);
            if (result.Count > 0)
                return result;

            Jotunn.Logger.LogWarning($"No valid mead base ingredients in '{Ingredients.Value}', using the default recipe");
            return Parse(DefaultIngredients, logProblems: false);
        }

        private static List<(ItemDrop item, int amount)> Parse(string text, bool logProblems)
        {
            var result = new List<(ItemDrop item, int amount)>();
            foreach (string entry in (text ?? "").Split(','))
            {
                string trimmed = entry.Trim();
                if (trimmed.Length == 0)
                    continue;

                string[] parts = trimmed.Split(':');
                string name = parts[0].Trim();
                int amount = 1;
                if (parts.Length > 2 || (parts.Length == 2 && (!int.TryParse(parts[1].Trim(), out amount) || amount < 1)))
                {
                    if (logProblems)
                        Jotunn.Logger.LogWarning($"Skipping mead base ingredient '{trimmed}': expected ItemName:Amount");
                    continue;
                }

                ItemDrop item = FindItem(name);
                if (item == null)
                {
                    if (logProblems)
                        Jotunn.Logger.LogWarning($"Skipping mead base ingredient '{name}': no item with that name");
                    continue;
                }

                result.Add((item, amount));
            }
            return result;
        }

        private static ItemDrop FindItem(string name)
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(name) : null;
            if (prefab == null)
                prefab = PrefabManager.Instance.GetPrefab(name);
            return prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        }

        // An empty name is valid and means "no station" (station stays null).
        private static bool TryFindStation(string name, out CraftingStation station)
        {
            station = null;
            if (string.IsNullOrWhiteSpace(name))
                return true;

            GameObject prefab = PrefabManager.Instance.GetPrefab(CraftingStations.GetInternalName(name.Trim()));
            station = prefab != null ? prefab.GetComponent<CraftingStation>() : null;
            return station != null;
        }
    }
}
