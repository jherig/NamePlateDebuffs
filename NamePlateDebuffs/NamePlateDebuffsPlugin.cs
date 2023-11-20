using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using NamePlateDebuffs.StatusNode;
using Dalamud.Game;


namespace NamePlateDebuffs
{
    public class NamePlateDebuffsPlugin : IDalamudPlugin
    {
        public string Name => "NamePlateDebuffs";

        [PluginService]
        [RequiredVersion("1.0")]
        public DalamudPluginInterface Interface { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public IClientState ClientState { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static ICommandManager CommandManager { get; private set; } = null!;

        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
        [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
        public PluginAddressResolver Address { get; private set; } = null!;
        public StatusNodeManager StatusNodeManager { get; private set; } = null!;
        public static AddonNamePlateHooks Hooks { get; private set; } = null!;
        public NamePlateDebuffsPluginUI UI { get; private set; } = null!;
        public NamePlateDebuffsPluginConfig Config { get; private set; } = null!;

        internal bool InPvp;

        public NamePlateDebuffsPlugin()
        {
            // load or create config
            Config = Interface.GetPluginConfig() as NamePlateDebuffsPluginConfig ?? new NamePlateDebuffsPluginConfig();
            Config.Initialize(Interface);


            Address = new PluginAddressResolver();
            Address.Setup(SigScanner);

            StatusNodeManager = new StatusNodeManager(this);

            Hooks = new AddonNamePlateHooks(this);
            Hooks.Initialize();

            UI = new NamePlateDebuffsPluginUI(this);

            //ClientState.TerritoryChanged += OnTerritoryChange;

            CommandManager.AddHandler("/npdebuffs", new CommandInfo(this.ToggleConfig)
            {
                HelpMessage = "Toggles config window."
            });
        }
        public void Dispose()
        {
            //ClientState?.TerritoryChanged -= OnTerritoryChange;
            CommandManager.RemoveHandler("/npdebuffs");

            UI.Dispose();
            Hooks.Dispose();
            StatusNodeManager.Dispose();
        }

        //private void OnTerritoryChange(object sender, ushort e)
        //{
        //    try
        //    {
        //        TerritoryType? territory = DataManager.GetExcelSheet<TerritoryType>()?.GetRow(e);
        //        if (territory != null) InPvp = territory.IsPvpZone;
        //    }
        //    catch (KeyNotFoundException)
        //    {
        //        Log.Warning("Could not get territory for current zone");
        //    }
        //}

        private void ToggleConfig(string command, string args)
        {
            UI.ToggleConfig();
        }
    }
}
