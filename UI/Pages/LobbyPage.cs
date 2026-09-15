using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RoR2;
using UnityEngine;
using static UmbraMenu.MenuController;
using static UmbraMenu.MenuWidgets;
using static UmbraMenu.MenuTheme;
using static UmbraMenu.MenuActions;

namespace UmbraMenu
{
    /// <summary>Lobby page; owns its local editing state and preserves the user's card layout.</summary>
    internal sealed class LobbyPage : IMenuPage
    {
        public string Title { get { return "Lobby"; } }
        public string Description { get { return "Connected players, host gifts, revival and session diagnostics."; } }

        private NetworkUser giftRecipient;
        private string giftMoney = "1000",
            giftCoins = "10",
            giftItems = "1";

        /// <summary>Requires explicit recipient selection; disconnected recipients never silently redirect gifts.</summary>
        private void DrawLobbyGifts()
        {
            AddCard(0, "PLAYERS", c =>
                {
                    foreach (var user in NetworkUser.readOnlyInstancesList)
                    {
                        if (!user)
                            continue;
                        var candidate = user;
                        DrawButtonRow(c, new ButtonAction(
                                (candidate == giftRecipient ? "✓ " : "") + candidate.userName,
                                () => giftRecipient = candidate));
                    }
                    DrawParagraph(c, "Select a recipient explicitly. Their character inventory becomes available after spawning.");
                });

            AddCard(1, "HOST GIFTS", c =>
            {
                DrawParagraph(c, giftRecipient ? "Recipient: " + giftRecipient.userName : "No recipient selected.");
                DrawInput(c, "Money", ref giftMoney);
                DrawButtonRow(c, new ButtonAction("Give money", () => GiveLobbyMoney(ParseUInt(giftMoney))));
                DrawInput(c, "Lunar coins", ref giftCoins);
                DrawButtonRow(c, new ButtonAction("Give lunar coins", () => GiveLobbyCoins(ParseUInt(giftCoins))));
                DrawParagraph(c, "Lunar coins change the recipient's persistent profile. Inform them before awarding.");
                DrawParagraph(c, "Item: " + (SelectedItem == null ? "Choose one in Items first." : SelectedItem.Name));
                DrawInput(c, "Item quantity", ref giftItems);
                DrawButtonRow(c,
                new ButtonAction("Choose item", () => OpenPage(3)),
                new ButtonAction("Give item", () =>
                {
                    RequireRecipient();
                    ItemService.Give(SelectedItem, giftRecipient.master, ItemCount(giftItems));
                    Toast("Gift sent to " + giftRecipient.userName);
                }));

                c.Space(7f);
                DrawButtonRow(c, new ButtonAction("Attempt revive", () => AttemptOtherRespawn()));
                DrawParagraph(c, "Attempt to revive the player.");
            },
                true
            );
        }

        /// <summary>Rechecks host and connection at the instant of each gift.</summary>
        private void RequireRecipient()
        {
            RequireHost();
            if (!giftRecipient || !NetworkUser.readOnlyInstancesList.Contains(giftRecipient))
                throw new InvalidOperationException("Select a connected recipient first.");
        }

        /// <summary>Awards checked run money to the selected player's master.</summary>
        private void GiveLobbyMoney(uint amount)
        {
            RequireRecipient();
            var master = giftRecipient.master;
            if (!master)
                throw new InvalidOperationException("Recipient has no character master yet.");
            if (amount > uint.MaxValue - master.money)
                throw new InvalidOperationException("Recipient's balance would overflow.");
            master.GiveMoney(amount);
            Toast("Money given to " + giftRecipient.userName);
        }

        /// <summary>Uses the game's server award/RPC path so the selected user's profile receives the currency.</summary>
        private void GiveLobbyCoins(uint amount)
        {
            RequireRecipient();
            if (amount > uint.MaxValue - giftRecipient.lunarCoins)
                throw new InvalidOperationException("Recipient's lunar balance would overflow.");
            giftRecipient.AwardLunarCoins(amount);
            Toast("Lunar coins awarded to " + giftRecipient.userName);
        }

        private void AttemptOtherRespawn()
        {
            RequireRecipient();
            var master = giftRecipient.master;
            if (!master)
                throw new InvalidOperationException("Recipient has no character master yet.");

            var body = master.GetBody();
            if (body != null && body.healthComponent != null && body.healthComponent.alive)
            {
                Toast("Recipient is already alive: " + giftRecipient.userName);
                throw new InvalidOperationException("Recipient is already alive.");
            }

            if (Run.instance && giftRecipient)
            {
                master.Respawn(master.deathFootPosition, Quaternion.identity, false);
                Toast("Revived " + giftRecipient.userName);
            }
        }
        /// <summary>Displays session data without pretending a run difficulty write changes lobby rules.</summary>
        public void Build(float width)
        {
            DrawLobbyGifts();
            AddCard(0, "SESSION", c =>
            {
                DrawReadout(c, "Connected players", NetworkUser.readOnlyInstancesList.Count.ToString());
                DrawReadout(c, "Local player", UmbraRuntime.LocalNetworkUser ? UmbraRuntime.LocalNetworkUser.userName : "—");
                DrawReadout(c, "Run seed", Run.instance ? Run.instance.seed.ToString() : "—");
                DrawButtonRow(c, new ButtonAction("Copy run seed", CopyRunSeed));
                DrawToggle(c, "Show active mods", State.Render.renderMods, v => State.Render.renderMods = v, null);
            });

            AddCard(0, "DIFFICULTY", c =>
            {
                DrawReadout(c, "Current", CurrentDifficulty());
                DrawParagraph(c, "Run difficulty is read-only here. Select difficulty in the game lobby before starting.");
            });

            AddCard(2, "MOD STATE", c =>
            {
                DrawReadout(c, "Application", "Modded");
                DrawParagraph(c, "Umbra is intended for private testing. Session-wide changes may require host authority and should be used with everyone informed.");
            });
        }
    }
}
