using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace BifrostBrew
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class BifrostBrewPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.voxxsidus.bifrostbrew";
        public const string PluginName = "BifrostBrew";
        public const string PluginVersion = "0.1.0";

        private static readonly Color BifrostColor = new Color(0.35f, 0.75f, 1f);

        // Shared by the channel and cooldown effects. Vanilla CanConsumeItem refuses to drink
        // while any status effect in the item's category is active.
        private const string BifrostCategory = "bifrostbrew";

        internal static ConfigEntry<float> ChannelSeconds;
        internal static ConfigEntry<float> CooldownMinutes;

        private void Awake()
        {
            CreateConfig();
            AddLocalizations();
            new Harmony(PluginGUID).PatchAll();
            // Vanilla prefabs don't exist yet in Awake; wait for Jotunn to say they're loaded.
            PrefabManager.OnVanillaPrefabsAvailable += AddItems;
            // Runs on every world load: ObjectDB (and with it the Serving Tray) is rebuilt each time.
            ItemManager.OnItemsRegistered += AddBrewToServingTray;
        }

        private void AddItems()
        {
            try
            {
                CustomItem meadBase = AddMeadBase();
                CustomItem brew = AddBrew();

                // Vanilla mead bases show the finished mead's effect in their tooltip by carrying
                // its status effect (they're Material items, so it can never be consumed). The clone
                // still carried Lingering Stamina Mead's; point it at the brew's instead.
                meadBase.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect =
                    brew.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect;

                AddFermenterConversion();
            }
            finally
            {
                // Only run once, not on every return to the main menu.
                PrefabManager.OnVanillaPrefabsAvailable -= AddItems;
            }
        }

        // Shown in config managers (e.g. BepInEx ConfigurationManager). IsAdminOnly makes Jotunn
        // sync the server's values to clients and lets only admins change them in multiplayer.
        private void CreateConfig()
        {
            var adminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };

            ChannelSeconds = Config.Bind("Bifrost Brew", "Channel seconds", 5f,
                new ConfigDescription("Seconds between drinking Bifrost Brew and being teleported. 0 teleports instantly.",
                    new AcceptableValueRange<float>(0f, 30f), adminOnly));

            CooldownMinutes = Config.Bind("Bifrost Brew", "Cooldown minutes", 0f,
                new ConfigDescription("Minutes after a teleport before another Bifrost Brew can be drunk. 0 disables the cooldown. " +
                                      "Status effects aren't saved, so logging out clears an active cooldown.",
                    new AcceptableValueRange<float>(0f, 120f), adminOnly));

            BifrostRecipe.Bind(Config, adminOnly);
        }

        // Liquid materials on the cloned prefabs (checked against the game's asset bundles):
        // - MeadStaminaLingering: "potion" renderers, slot 0 is the bottle body/liquid; slot 1
        //   (MistlandsPotions_mat) is the cork, strap, label and seal.
        // - MeadBaseStaminaLingering: a single "meadbase" material covers the white bowl and the contents.
        private const string BrewLiquidMaterial = "MajorStaminaPotion_mat";
        private const string MeadBaseLiquidMaterial = "meadbase";

        // Which vanilla icon pixels are the amber liquid and glow (h, s in 0-1). The red seal, brown
        // strap and brown bowl fall outside hue 30-75 deg. The tan cork overlaps that range but is
        // both lower in hue (30-38 deg) and paler (saturation <= 0.45) than the liquid.
        private static bool IsIconLiquid(float h, float s) =>
            h >= 30f / 360f && h <= 75f / 360f && s >= 0.15f && (h >= 39f / 360f || s >= 0.46f);

        // The brew's liquid runs cyan -> blue -> violet from bottom to top, like the channel swirl.
        // Texture V tracks height on the bottle mesh (checked against the mesh). In MistlandsPotions_d
        // the bottle's base is at v ~0.08, the leather strap covers v ~0.09-0.15 and the liquid
        // surface (where the grey glass starts) is at v ~0.25. Mapping 0.05-0.25 puts cyan below the
        // strap and runs the visible liquid above it from blue to violet. In the 64px vanilla icon
        // the liquid spans v ~0.08-0.52.
        private static Color BrewLiquidColor(float v) => BifrostGradient(Mathf.InverseLerp(0.05f, 0.25f, v));
        private static Color BrewIconColor(float v) => BifrostGradient(Mathf.InverseLerp(0.08f, 0.52f, v));

        private static Color BifrostGradient(float t) => t < 0.5f
            ? Color.Lerp(BifrostVfx.Cyan, BifrostVfx.Blue, t * 2f)
            : Color.Lerp(BifrostVfx.Blue, BifrostVfx.Violet, t * 2f - 1f);

        // The bottle's own point light sits right on it. Lit color is roughly albedo x light, so a
        // cyan light turned the violet top blue; a pale lavender keeps the whole gradient visible.
        private static readonly Color BrewLightColor = new Color(0.75f, 0.7f, 1f);

        // Mead Ketill-crafted base that goes into the fermenter.
        private CustomItem AddMeadBase()
        {
            var config = new ItemConfig
            {
                Name = "$item_meadbasebifrost",
                Description = "$item_meadbasebifrost_desc",
                // Station and ingredients come from the [Recipe] config section.
                CraftingStation = BifrostRecipe.GetStationName(),
                Amount = 1,
                Requirements = BifrostRecipe.GetRequirementConfigs()
            };

            var item = new CustomItem(BifrostRecipe.MeadBaseName, "MeadBaseStaminaLingering", config);
            Recolor(item.ItemPrefab, MeadBaseLiquidMaterial, BifrostColor);
            RecolorIcon(item, BifrostColor);
            ItemManager.Instance.AddItem(item);
            BifrostRecipe.Attach(item);
            return item;
        }

        // The finished, drinkable brew. No recipe: it only comes from the fermenter.
        private CustomItem AddBrew()
        {
            var item = new CustomItem("BifrostBrew", "MeadStaminaLingering");
            var shared = item.ItemDrop.m_itemData.m_shared;

            shared.m_name = "$item_bifrostbrew";
            shared.m_description = "$item_bifrostbrew_desc";

            Recolor(item.ItemPrefab, BrewLiquidMaterial, BrewLightColor, BrewLiquidColor);
            RecolorIcon(item, BifrostColor, BrewIconColor);

            // Replace the cloned lingering stamina effect with the teleport.
            shared.m_consumeStatusEffect = CreateTeleportEffects(shared.m_icons[0], shared.m_consumeStatusEffect);

            // The clone keeps the lingering mead's Piece component, which is what lets the Serving
            // Tray place it (ItemDrop.m_pieceEnableObj switches on the bottle model when placed).
            // Retarget it so the tray shows Bifrost Brew and placing one costs a Bifrost Brew.
            if (item.ItemPrefab.TryGetComponent(out Piece piece))
            {
                piece.m_name = shared.m_name;
                piece.m_description = shared.m_description;
                piece.m_icon = shared.m_icons[0];
                piece.m_resources = new[] { new Piece.Requirement { m_resItem = item.ItemDrop, m_amount = 1, m_recover = true } };
            }
            else
            {
                Jotunn.Logger.LogWarning("MeadStaminaLingering has no Piece component; Bifrost Brew won't be placeable");
            }

            ItemManager.Instance.AddItem(item);
            s_brewPrefab = item.ItemPrefab;
            return item;
        }

        private static GameObject s_brewPrefab;

        // Adds Bifrost Brew to the Serving Tray's piece table, right after Lingering Stamina Mead.
        private static void AddBrewToServingTray()
        {
            if (s_brewPrefab == null || ObjectDB.instance == null)
                return;

            GameObject tray = ObjectDB.instance.GetItemPrefab(ServingTray);
            PieceTable table = tray != null ? tray.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces : null;
            if (table == null)
            {
                Jotunn.Logger.LogWarning($"{ServingTray} piece table not found; Bifrost Brew won't be placeable");
                return;
            }

            if (table.m_pieces.Contains(s_brewPrefab))
                return;

            int index = table.m_pieces.FindIndex(p => p != null && p.name == "MeadStaminaLingering");
            table.m_pieces.Insert(index >= 0 ? index + 1 : table.m_pieces.Count, s_brewPrefab);
        }

        // Internal name of the Serving Tray item.
        private const string ServingTray = "Feaster";

        private static StatusEffect CreateTeleportEffects(Sprite icon, StatusEffect vanillaEffect)
        {
            var cooldown = ScriptableObject.CreateInstance<StatusEffect>();
            cooldown.name = "SE_BifrostCooldown";
            cooldown.m_name = "$se_bifrostcooldown";
            cooldown.m_tooltip = "$se_bifrostcooldown_tooltip";
            cooldown.m_icon = icon;
            cooldown.m_cooldownIcon = true;
            cooldown.m_category = BifrostCategory;
            ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(cooldown, false));
            SE_BifrostChannel.CooldownEffect = cooldown;

            var channel = ScriptableObject.CreateInstance<SE_BifrostChannel>();
            channel.name = "SE_BifrostChannel";
            channel.m_name = "$se_bifrostchannel";
            channel.m_tooltip = "$se_bifrostchannel_tooltip";
            channel.m_icon = icon;
            channel.m_category = BifrostCategory;
            channel.m_startMessageType = MessageHud.MessageType.Center;
            channel.m_startMessage = "$se_bifrostchannel_start";
            // Keep the lingering mead's start sound (sfx_Potion_stamina_Start_lingering) but replace
            // vfx_Potion_stamina_medium, the stamina potion's yellow visual, with the Bifrost portal.
            var startEffects = new List<EffectList.EffectData>();
            if (vanillaEffect != null)
            {
                startEffects.AddRange(vanillaEffect.m_startEffects.m_effectPrefabs
                    .Where(e => e.m_prefab != null && !e.m_prefab.name.StartsWith("vfx_")));
            }

            // Attached so it follows the drinker; removed by SEMan when the channel ends.
            GameObject channelVfx = BifrostVfx.CreateChannelEffect();
            if (channelVfx != null)
                startEffects.Add(new EffectList.EffectData { m_prefab = channelVfx, m_attach = true });
            channel.m_startEffects = new EffectList { m_effectPrefabs = startEffects.ToArray() };

            // Played where the player leaves from (SE_BifrostChannel only keeps it on a real teleport).
            GameObject departVfx = BifrostVfx.CreateDepartEffect();
            if (departVfx != null)
                channel.m_stopEffects = new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = departVfx } } };
            ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(channel, false));
            return channel;
        }

        private void AddFermenterConversion()
        {
            var conversion = new CustomItemConversion(new FermenterConversionConfig
            {
                FromItem = BifrostRecipe.MeadBaseName,
                ToItem = "BifrostBrew",
                ProducedItems = BifrostRecipe.BrewsPerBase.Value
            });
            ItemManager.Instance.AddItemConversion(conversion);
        }

        // color tints the lights and, unless colorAtV is given, the liquid. colorAtV maps a texture
        // V coordinate (0 = bottom) to a liquid color, for a vertical gradient.
        private static void Recolor(GameObject prefab, string liquidMaterial, Color color, System.Func<float, Color> colorAtV = null)
        {
            // IMPORTANT: sharedMaterials on a clone are the SAME assets the vanilla
            // mead uses. Copy them, or every lingering stamina mead turns blue too.
            // Renderers that share a material also share its copy.
            var copies = new Dictionary<Material, Material>();

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].name != liquidMaterial) continue;

                    if (!copies.TryGetValue(mats[i], out var copy))
                    {
                        copy = new Material(mats[i]) { name = mats[i].name + "_bifrost" };

                        // Both shaders (Autodesk Interactive, Standard) multiply _Color into the
                        // _MainTex albedo. The liquid's color lives in the texture (amber/brown),
                        // so a _Color tint would only darken it toward olive. Hue-shift a copy of
                        // the texture instead and leave _Color white.
                        if (!IsHeadless && copy.GetTexture("_MainTex") is Texture2D tex)
                        {
                            var shifted = CopyRegion(tex, new Rect(0, 0, tex.width, tex.height), true);
                            shifted.wrapMode = tex.wrapMode;
                            shifted.filterMode = tex.filterMode;
                            shifted.anisoLevel = tex.anisoLevel;
                            ShiftHue(shifted, colorAtV ?? (_ => color), (h, sat) => sat >= 0.15f);
                            copy.SetTexture("_MainTex", shifted);
                        }

                        copies[mats[i]] = copy;
                    }

                    mats[i] = copy;
                    changed = true;
                }
                if (changed)
                    renderer.sharedMaterials = mats;
            }

            // Lights are components on the clone itself, not shared assets, so edit them directly.
            foreach (var light in prefab.GetComponentsInChildren<Light>(true))
                light.color = color;
        }

        // Vanilla icons are hand-made images (upright, tightly framed, glow painted in) that a
        // RenderManager render of the model can't reproduce. Recolor a copy of the cloned
        // item's vanilla icon instead, so angle, framing and glow match exactly.
        private static void RecolorIcon(CustomItem item, Color color, System.Func<float, Color> colorAtV = null)
        {
            var shared = item.ItemDrop.m_itemData.m_shared;
            if (IsHeadless || shared.m_icons == null || shared.m_icons.Length == 0)
                return;

            Sprite vanilla = shared.m_icons[0];
            if (vanilla == null)
                return;

            Texture2D tex;
            try
            {
                // Icons are rect-packed into IconAtlas; textureRect is the icon's area in the atlas.
                tex = CopyRegion(vanilla.texture, vanilla.textureRect, false);
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogWarning($"{item.ItemPrefab.name}: could not copy icon {vanilla.name}, keeping vanilla icon. {e.Message}");
                return;
            }

            ShiftHue(tex, colorAtV ?? (_ => color), IsIconLiquid);
            var pivot = new Vector2(vanilla.pivot.x / vanilla.rect.width, vanilla.pivot.y / vanilla.rect.height);
            var icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, vanilla.pixelsPerUnit);
            icon.name = item.ItemPrefab.name;
            shared.m_icons = new[] { icon };
        }

        // Dedicated servers have no GPU to blit with, and nothing is rendered there anyway.
        internal static bool IsHeadless => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        // Copies a region of a texture into a new readable Texture2D. Game textures aren't
        // CPU-readable (and are often compressed), so go through a RenderTexture.
        internal static Texture2D CopyRegion(Texture tex, Rect region, bool mipChain)
        {
            int width = (int)region.width;
            int height = (int)region.height;
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var scale = new Vector2(region.width / tex.width, region.height / tex.height);
            var offset = new Vector2(region.x / tex.width, region.y / tex.height);
            Graphics.Blit(tex, rt, scale, offset);

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, mipChain) { name = tex.name + "_bifrost" };
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        // Moves every pixel that shouldShift(hue, saturation) accepts to the hue of colorAtV(v),
        // where v is the pixel row's texture V (0 = bottom), keeping its saturation, brightness and
        // alpha. Callers skip near-grey pixels (the empty glass, the white bowl) via saturation.
        // Uploads the result and makes it no longer CPU-readable.
        private static void ShiftHue(Texture2D tex, System.Func<float, Color> colorAtV, System.Func<float, float, bool> shouldShift)
        {
            // GetPixels runs left to right, bottom to top, so row y is pixel index / width.
            var rowHue = new float[tex.height];
            for (int y = 0; y < rowHue.Length; y++)
                Color.RGBToHSV(colorAtV((y + 0.5f) / tex.height), out rowHue[y], out _, out _);

            var pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color.RGBToHSV(pixels[i], out float h, out float s, out float v);
                if (!shouldShift(h, s)) continue;

                var shifted = Color.HSVToRGB(rowHue[i / tex.width], s, v);
                shifted.a = pixels[i].a;
                pixels[i] = shifted;
            }
            tex.SetPixels(pixels);
            tex.Apply(true, true);
        }

        private void AddLocalizations()
        {
            var loc = LocalizationManager.Instance.GetLocalization();
            loc.AddTranslation("English", new Dictionary<string, string>
            {
                { "item_bifrostbrew", "Bifrost Brew" },
                { "item_bifrostbrew_desc", "A shimmering mead that carries you home across the rainbow bridge." },
                { "item_meadbasebifrost", "Mead base: Bifrost" },
                { "item_meadbasebifrost_desc", "Needs to ferment before it can be drunk." },
                { "se_bifrostchannel", "Bifrost" },
                { "se_bifrostchannel_tooltip", "Opens the Bifrost and carries you home to your bed." },
                { "se_bifrostchannel_start", "The Bifrost shimmers around you..." },
                { "se_bifrostcooldown", "Bifrost-weary" },
                { "se_bifrostcooldown_tooltip", "The rainbow bridge needs time before it will carry you again." },
                { "msg_bifrost_nohome", "The Bifrost cannot find your home" }
            });
        }
    }
}
