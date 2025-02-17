  using Format = ModKit.Helper.TextFormattingHelper;
using Life;
using Life.Network;
using Life.UI;
using System.Threading.Tasks;
using System;
using System.Linq;
using Life.DB;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;
using System.IO;
using Newtonsoft.Json;
using Mirror;

namespace JournalierSystem
{
    public class Journalier : ModKit.ModKit
    {
        public Journalier(IGameAPI game) : base(game)
        {
            PluginInformations = new PluginInformations("JournalierSystem", "1.0.0", "! Fenix");
        }

        public Config config;

        public void InitConfig()
        {
            string directoryPath = pluginsPath + "/JournalierSystem";
            string configFilePath = directoryPath + "/config.json";

            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            if (!File.Exists(configFilePath))
            {
                var defaultConfig = new Config
                {
                    JournalierMoney = 700,
                };
                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
            }

            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFilePath));
        }

        public class Config
        {
            public int JournalierMoney { get; set; }
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            InitConfig();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisée");
            Orm.RegisterTable<Cooldown_DB>();
        }


        public override async void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
        {
            base.OnPlayerSpawnCharacter(player, conn, character);

            if(await CreateOrUpdateCooldown(player))
            {
                JournalierPanel(player);
            }
            else
            {
                player.Notify("JournalierSystem", "Vous avez déja récuperer votre journalier !");
            }
        }

        public void JournalierPanel(Player player)
        {
            Panel panel = PanelHelper.Create("JournalierSystem", UIPanel.PanelType.Text, player, () => JournalierPanel(player));
            panel.TextLines.Add($"Voulez vous récuperer votre journalier d'une valeur de {Format.Color($"{config.JournalierMoney}€", Format.Colors.Info)}");
            panel.CloseButton();
            panel.CloseButtonWithAction($"{Format.Color("Récuperer", Format.Colors.Success)}", async () =>
            {
                player.Notify("JournalierSystem", "Vous avez récuperer votre journalier avec succès !", NotificationManager.Type.Success);
                player.AddMoney(config.JournalierMoney, "Journalier");
                return await Task.FromResult(true);
            });
            panel.Display();
        }
        public async Task<bool> CreateOrUpdateCooldown(Player player)
        {
            long cooldown = 86400;
            var query = await Cooldown_DB.Query(d => d.CharacterId == player.account.id);
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (query != null && query.Any())
            {
                var lastCooldown = query.First();
                if (currentTime < lastCooldown.LastUsed)
                {
                    return false;
                }
                else
                {
                    lastCooldown.LastUsed = currentTime + cooldown;
                    await lastCooldown.Save();
                    return true;
                }
            }
            else
            {
                var newCooldown = new Cooldown_DB
                {
                    CharacterId = player.account.id,
                    LastUsed = currentTime + cooldown
                };
                await newCooldown.Save();
                return true;
            }
        }
    }
}
