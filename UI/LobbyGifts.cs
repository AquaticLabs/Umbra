using System;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    internal static partial class ModernMenu
    {
        private static NetworkUser giftRecipient;
        private static string giftMoney = "1000",
            giftCoins = "10",
            giftItems = "1";

        /// <summary>Requires explicit recipient selection; disconnected recipients never silently redirect gifts.</summary>
        private static void DrawLobbyGifts()
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
                DrawParagraph(c, "Item: " + (catalogSelection == null ? "Choose one in Items first." : catalogSelection.Name));
                DrawInput(c, "Item quantity", ref giftItems);
                DrawButtonRow(c,
                new ButtonAction("Choose item", () => OpenPage(3)),
                new ButtonAction("Give item", () =>
                {
                    RequireRecipient();
                    ItemService.Give(catalogSelection, giftRecipient.master, ItemCount(giftItems));
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
        private static void RequireRecipient()
        {
            RequireHost();
            if (!giftRecipient || !NetworkUser.readOnlyInstancesList.Contains(giftRecipient))
                throw new InvalidOperationException("Select a connected recipient first.");
        }

        /// <summary>Awards checked run money to the selected player's master.</summary>
        private static void GiveLobbyMoney(uint amount)
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
        private static void GiveLobbyCoins(uint amount)
        {
            RequireRecipient();
            if (amount > uint.MaxValue - giftRecipient.lunarCoins)
                throw new InvalidOperationException("Recipient's lunar balance would overflow.");
            giftRecipient.AwardLunarCoins(amount);
            Toast("Lunar coins awarded to " + giftRecipient.userName);
        }

        private static void AttemptOtherRespawn()
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
    }
}
