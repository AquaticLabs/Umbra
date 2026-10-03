using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine.Networking;

namespace UmbraMenu
{
    /// <summary>Session-only, per-recipient invulnerability with owned state restored on disconnect or unload.</summary>
    internal static class LobbyProtection
    {
        private sealed class Grant
        {
            internal HealthComponent Health;
            internal bool Original;
            internal void Restore() { if (Health) Health.godMode = Original; Health = null; }
        }
        private static readonly Dictionary<NetworkUser, Grant> grants = new Dictionary<NetworkUser, Grant>();
        internal static bool IsEnabled(NetworkUser user) { return user && grants.ContainsKey(user); }

        internal static void Set(NetworkUser user, bool enabled)
        {
            if (!NetworkServer.active) throw new InvalidOperationException("Player protection requires the host.");
            if (!user || !NetworkUser.readOnlyInstancesList.Contains(user)) throw new InvalidOperationException("Select a connected player.");
            if (user == UmbraRuntime.LocalNetworkUser) throw new InvalidOperationException("Use Player > God mode for yourself.");
            if (enabled && !grants.ContainsKey(user)) grants.Add(user, new Grant());
            if (!enabled && grants.TryGetValue(user, out var grant)) { grant.Restore(); grants.Remove(user); }
        }

        internal static void Update()
        {
            if (!NetworkServer.active) { Clear(); return; }
            foreach (var pair in grants.ToArray())
            {
                if (!pair.Key || !NetworkUser.readOnlyInstancesList.Contains(pair.Key))
                { pair.Value.Restore(); grants.Remove(pair.Key); continue; }
                var body = pair.Key.master ? pair.Key.master.GetBody() : null;
                var health = body ? body.healthComponent : null;
                var grant = pair.Value;
                if (grant.Health != health)
                {
                    grant.Restore(); grant.Health = health;
                    if (health) grant.Original = health.godMode;
                }
                if (health) health.godMode = true;
            }
        }

        internal static void Clear()
        {
            foreach (var grant in grants.Values) grant.Restore();
            grants.Clear();
        }
    }
}
