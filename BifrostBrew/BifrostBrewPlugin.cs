using System.Collections.Generic;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace BifrostBrew
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class BifrostBrewPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.preston.bifrostbrew";
        public const string PluginName = "BifrostBrew";
        public const string PluginVersion = "0.1.0";

        private static readonly Color BifrostColor = new Color(0.35f, 0.75f, 1f);

        private void Awake()
        {
            AddLocalizations();
            // Vanilla prefabs don't exist yet in Awake; wait for Jotunn to say they're loaded.
            PrefabManager.OnVanillaPrefabsAvailable += AddItems;
        }

        private void AddItems()
        {
            try
            {
                AddMeadBase();
                AddBrew();
                AddFermenterConversion();
            }
            finally
            {
                // Only run once, not on every return to the main menu.
                PrefabManager.OnVanillaPrefabsAvailable -= AddItems;
            }
        }

        // Internal prefab name of the Mead Ketill (added in the Bog Witch update).
        private const string MeadKetill = "piece_MeadCauldron";

        // Mead Ketill-crafted base that goes into the fermenter.
        private void AddMeadBase()
        {
            var config = new ItemConfig
            {
                Name = "$item_meadbasebifrost",
                Description = "$item_meadbasebifrost_desc",
                CraftingStation = MeadKetill,
                Amount = 1,
                Requirements = new[]
                {
                    new RequirementConfig("Honey", 10),
                    new RequirementConfig("GreydwarfEye", 5),
                    new RequirementConfig("SurtlingCore", 1)
                }
            };

            var item = new CustomItem("MeadBaseBifrost", "MeadBaseHealthMinor", config);
            Recolor(item.ItemPrefab, BifrostColor);
            SetIcon(item);
            ItemManager.Instance.AddItem(item);
        }

        // The finished, drinkable brew. No recipe: it only comes from the fermenter.
        private void AddBrew()
        {
            var item = new CustomItem("BifrostBrew", "MeadHealthMinor");
            var shared = item.ItemDrop.m_itemData.m_shared;

            shared.m_name = "$item_bifrostbrew";
            shared.m_description = "$item_bifrostbrew_desc";

            // The clone still carries the vanilla healing effect. Clear it for now;
            // the custom teleport StatusEffect goes here later.
            shared.m_consumeStatusEffect = null;

            Recolor(item.ItemPrefab, BifrostColor);
            SetIcon(item);
            ItemManager.Instance.AddItem(item);
        }

        private void AddFermenterConversion()
        {
            var conversion = new CustomItemConversion(new FermenterConversionConfig
            {
                FromItem = "MeadBaseBifrost",
                ToItem = "BifrostBrew",
                ProducedItems = 6
            });
            ItemManager.Instance.AddItemConversion(conversion);
        }

        private static void Recolor(GameObject prefab, Color color)
        {
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                // IMPORTANT: sharedMaterials on a clone are the SAME assets the vanilla
                // mead uses. Copy them, or every minor healing mead turns blue too.
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;

                    // TODO: once you've inspected the prefab, only tint the liquid material
                    // (check mats[i].name / renderer.name) so the cork and glass keep their colors.
                    var mat = new Material(mats[i]);

                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", color);

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", color * 0.6f);
                    }

                    mats[i] = mat;
                }
                renderer.sharedMaterials = mats;
            }
        }

        private static void SetIcon(CustomItem item)
        {
            // Renders the recolored model into a sprite so the inventory icon matches.
            // Alternative: ship a hand-made PNG and load it with AssetUtils.LoadSpriteFromFile.
            Sprite icon = RenderManager.Instance.Render(item.ItemPrefab, RenderManager.IsometricRotation);
            if (icon != null)
                item.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
        }

        private void AddLocalizations()
        {
            var loc = LocalizationManager.Instance.GetLocalization();
            loc.AddTranslation("English", new Dictionary<string, string>
            {
                { "item_bifrostbrew", "Bifrost Brew" },
                { "item_bifrostbrew_desc", "A shimmering mead that carries you home across the rainbow bridge." },
                { "item_meadbasebifrost", "Mead base: Bifrost" },
                { "item_meadbasebifrost_desc", "Needs to ferment before it can be drunk." }
            });
        }
    }
}
