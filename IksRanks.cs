using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using IksAdminApi;
using RanksApi;

namespace IksRanks
{
    public class IksRanks : BasePlugin, IPluginConfig<IksRanksConfig>
    {
        public override string ModuleName => "[IKS] Ranks";
        public override string ModuleDescription => "";
        public override string ModuleAuthor => "E!N";
        public override string ModuleVersion => "v1.0.0";

        private IRanksApi? _api;

        public IksRanksConfig Config { get; set; } = new();

        public void OnConfigParsed(IksRanksConfig config)
        {
            Config = config;
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _api = IRanksApi.Capability.Get();
            if (_api == null)
            {
                return;
            }

            AdminModule.Api.MenuOpenPre += OnMenuOpenPre;
            AdminModule.Api.RegisterPermission("other.ranks", Config.AdminPermissionFlags);
        }

        public override void Unload(bool hotReload)
        {
            AdminModule.Api.MenuOpenPre -= OnMenuOpenPre;
        }

        private HookResult OnMenuOpenPre(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu)
        {
            if (menu.Id != "iksadmin:menu:main")
            {
                return HookResult.Continue;
            }

            menu.AddMenuOption("ranks", Localizer["MenuOption.Ranks"], (_, _) => { OpenRanksMainMenu(player, menu); },
                viewFlags: AdminUtils.GetCurrentPermissionFlags("other.ranks"));

            return HookResult.Continue;
        }

        private void OpenRanksMainMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null!)
        {
            IDynamicMenu menu = AdminModule.Api.CreateMenu(
                "ranks.main",
                Localizer["MenuOption.Ranks"],
                backMenu: backMenu
            );

            menu.AddMenuOption("ranks.reload", Localizer["MenuOption.ReloadAllConfigs"], (_, _) => { Server.ExecuteCommand("css_lr_reload"); AdminUtils.Print(caller, Localizer["ConfigUpdated"]); });
            menu.AddMenuOption("ranks.exp", Localizer["MenuOption.GiveTakeMenuExp"], (p, opt) => { OpenSelectPlayerMenu(p, menu); });

            menu.Open(caller);
        }

        private void OpenSelectPlayerMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null!)
        {
            IDynamicMenu menu = AdminModule.Api.CreateMenu(
                "ranks.sp",
                Localizer["MenuOption.SP"],
                backMenu: backMenu
            );
            List<CCSPlayerController> players = [.. PlayersUtils.GetOnlinePlayers()];

            foreach (CCSPlayerController? target in players)
            {
                menu.AddMenuOption(target.GetSteamId(), target.PlayerName, (_, _) =>
                {
                    OpenGiveTakeMenu(caller, target, menu);
                });
            }

            menu.Open(caller);
        }

        private void OpenGiveTakeMenu(CCSPlayerController caller, CCSPlayerController target, IDynamicMenu? backMenu = null!)
        {
            IDynamicMenu menu = AdminModule.Api.CreateMenu(
                "ranks.gt",
                Localizer["MenuOption.GiveTakeMenuExp"],
                backMenu: backMenu
            );

            void AddExpOption(int amount)
            {
                string key = $"ranks.{amount}";
                string label = amount.ToString();
                menu.AddMenuOption(key, label, (_, _) =>
                {
                    if (amount > 0)
                    {
                        _api?.GivePlayerExperience(target, amount);
                        AdminUtils.Print(caller, Localizer["GiveExpAdmin", amount, target.PlayerName]);
                        AdminUtils.Print(target, Localizer["GiveExpPlayer", amount, caller.PlayerName, _api?.GetPlayerExperience(target) ?? 0]);
                    }
                    else
                    {
                        int absAmount = Math.Abs(amount);
                        _api?.TakePlayerExperience(target, absAmount);
                        AdminUtils.Print(caller, Localizer["TakeExpAdmin", absAmount, target.PlayerName]);
                        AdminUtils.Print(target, Localizer["TakeExpPlayer", absAmount, caller.PlayerName, _api?.GetPlayerExperience(target) ?? 0]);
                    }
                });
            }

            int[] expChanges = [10, 100, 1000, -1000, -100, -10];
            foreach (int amount in expChanges)
            {
                AddExpOption(amount);
            }

            menu.Open(caller);
        }
    }
}