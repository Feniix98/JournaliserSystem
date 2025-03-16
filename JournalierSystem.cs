using System;
using System.Linq;
using mk = ModKit.Helper.TextFormattingHelper;
using System.Threading.Tasks;
using Life;
using Life.Network;
using Mirror;
using Life.DB;
using ModKit.Helper;
using Life.UI;
using Socket.Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.IO;

namespace JournalierSystem
{
    class JournalierSystem : ModKit.ModKit
    {
        public JournalierSystem(IGameAPI aPI) : base(aPI) { }

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
                    Money = 700,
                };
                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
            }

            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFilePath));
        }
        public class Config
        {
            public double Money { get; set; }
        }

        static void Notify(Player player, string message, NotificationManager.Type type, float seconds = 6f)
        {
            mk.Colors color = mk.Colors.Info;
            if (type == NotificationManager.Type.Error) color = mk.Colors.Error;
            else if (type == NotificationManager.Type.Warning) color = mk.Colors.Warning;
            else if (type == NotificationManager.Type.Success) color = mk.Colors.Success;

            player.Notify(mk.Color(nameof(JournalierSystem), color), message, type, seconds);
        }

        static void Debug(object message, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[{nameof(JournalierSystem)}] " + (message?.ToString() ?? "unknown message"));
            Console.ResetColor();
        }

        public override async void OnPluginInit()
        {
            base.OnPluginInit();
            InitConfig();
            Orm.RegisterTable<JournalierManager>();

            Debug("Plugin is ready !", ConsoleColor.DarkGreen);
        }

        public override async void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
        {
            base.OnPlayerSpawnCharacter(player, conn, character);
            if(await CreateOrUpdateCooldown(player))
            {
                Journalier(player);
            }
        }

        void Journalier(Player player)
        {
            UIPanel panel = new UIPanel(mk.Color("JournalierSystem", mk.Colors.Info), UIPanel.PanelType.Text);

            panel.SetText("Voulez-vous récuperer votre journalier d'une valeur de" + " " + mk.Color(config.Money.ToString() + "€", mk.Colors.Info));

            panel.AddButton(mk.Color("Fermer", mk.Colors.Error), ui => player.ClosePanel(panel));
            panel.AddButton(mk.Color("Récuperer", mk.Colors.Success), async delegate
            {
                Notify(player, "Vous avez récuperer votre journalier avec succès !", NotificationManager.Type.Success);
                player.AddMoney(config.Money, "JOURNALIER_SYSTEM");
                await player.Save();
                player.ClosePanel(panel);
            });
            player.ShowPanelUI(panel);
        }

        private async Task<bool> CreateOrUpdateCooldown(Player player)
        {
            const long CooldownDuration = 86400;
            var query = await JournalierManager.Query(p => p.PlayerId == player.account.id);
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (query.Any())
            {
                var journalier = query.First();
                long timeSinceLastUse = currentTime - journalier.LastUse;

                if (timeSinceLastUse > CooldownDuration)
                {
                    journalier.LastUse = currentTime;
                    await journalier.Save();
                    return true;
                }

                var remainingTime = CooldownDuration - timeSinceLastUse;
                TimeSpan timeLeft = TimeSpan.FromSeconds(remainingTime);
                Notify(player, $"Vous pourrez récupérer votre journalier dans {timeLeft:hh\\:mm\\:ss}", NotificationManager.Type.Info);
                return false;
            }
            else
            {
                var newJournalier = new JournalierManager { PlayerId = player.account.id, LastUse = currentTime };
                await newJournalier.Save();
                return true;
            }
        }
    }
}
