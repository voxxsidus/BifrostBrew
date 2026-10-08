using HarmonyLib;
using UnityEngine;

namespace BifrostBrew
{
    // Status effect applied when Bifrost Brew is drunk. Counts down the channel time, then
    // teleports the player to their bed (or the world spawn if no bed is set).
    //
    // The template is registered with ObjectDB; SEMan clones it for each player that drinks,
    // so per-use state lives on the clone.
    public class SE_BifrostChannel : StatusEffect
    {
        // Applied after a successful teleport when the cooldown config is above zero.
        // Static rather than a field: Instantiate only copies fields Unity can serialize.
        internal static StatusEffect CooldownEffect;

        private bool m_fired;

        public override void Setup(Character character)
        {
            // Read the (server-synced) config per use so changes apply without a restart.
            m_ttl = BifrostBrewPlugin.ChannelSeconds.Value;
            base.Setup(character);
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            if (m_fired || m_time < m_ttl)
                return;

            m_fired = true;
            if (m_character is Player player && player == Player.m_localPlayer)
                Teleport(player);
        }

        // Base IsDone treats m_ttl == 0 as "never ends", which would break a 0-second channel.
        public override bool IsDone() => m_fired;

        private static void Teleport(Player player)
        {
            if (player.IsDead() || player.IsTeleporting())
                return;

            // Re-check: ore picked up during the channel still blocks the trip (the mead is spent).
            if (!player.IsTeleportable(false))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return;
            }

            if (!TryGetHome(out Vector3 destination))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_bifrost_nohome");
                return;
            }

            // Leave chairs, beds, ship controls and saddles before moving, like a respawn would.
            player.StopDoodadControl();
            player.AttachStop();

            // distantTeleport: waits for the destination zone to load and finds the floor.
            if (player.TeleportTo(destination, player.transform.rotation, true))
                StartCooldown(player);
        }

        // Bed spawn point if set, otherwise the starting stones (same order as a death respawn).
        // A destroyed bed still sends the player to where it stood; only a respawn clears it.
        private static bool TryGetHome(out Vector3 destination)
        {
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (profile.HaveCustomSpawnPoint())
            {
                destination = profile.GetCustomSpawnPoint();
                return true;
            }

            if (ZoneSystem.instance.GetLocationIcon(Game.instance.m_StartLocation, out destination))
            {
                destination += Vector3.up * 2f;
                return true;
            }

            destination = profile.GetHomePoint();
            return destination != Vector3.zero;
        }

        private static void StartCooldown(Player player)
        {
            float minutes = BifrostBrewPlugin.CooldownMinutes.Value;
            if (minutes <= 0f || CooldownEffect == null)
                return;

            // SEMan clones the template; set the duration on the clone.
            StatusEffect cooldown = player.GetSEMan().AddStatusEffect(CooldownEffect, true);
            if (cooldown != null)
                cooldown.m_ttl = minutes * 60f;
        }
    }

    // Refuse to drink (and so don't use up the brew) while carrying items portals won't take.
    // The cooldown and "already channeling" cases need no patch: vanilla CanConsumeItem
    // already refuses while the player has a status effect in the brew's category.
    [HarmonyPatch(typeof(Player), nameof(Player.CanConsumeItem))]
    internal static class BlockNonTeleportablePatch
    {
        private static void Postfix(Player __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!__result || !(item.m_shared.m_consumeStatusEffect is SE_BifrostChannel))
                return;

            if (!__instance.IsTeleportable(false))
            {
                __instance.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                __result = false;
            }
        }
    }
}
