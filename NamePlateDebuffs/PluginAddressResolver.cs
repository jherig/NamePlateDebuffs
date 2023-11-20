using Dalamud.Game;
using System;
using Dalamud.Logging;
using NamePlateDebuffs;

namespace NamePlateDebuffs
{
    public class PluginAddressResolver : BaseAddressResolver
    {
        public IntPtr AddonNamePlateFinalizeAddress { get; private set;  }

        private const string AddonNamePlateFinalizeSignature = "40 53 48 83 EC 20 48 8B D9 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B CB";

        public IntPtr AddonNamePlateDrawAddress { get; private set; }

        private const string AddonNamePlateDrawSignature = "0F B7 81 ?? ?? ?? ?? 4C 8B C1 66 C1 E0 06";

        protected override void Setup64Bit(ISigScanner scanner)
        {
            AddonNamePlateFinalizeAddress = scanner.ScanText(AddonNamePlateFinalizeSignature);
            AddonNamePlateDrawAddress = scanner.ScanText(AddonNamePlateDrawSignature);

            NamePlateDebuffsPlugin.Log.Verbose("===== NamePlate Debuffs =====");
            NamePlateDebuffsPlugin.Log.Verbose($"{nameof(AddonNamePlateFinalizeAddress)} {AddonNamePlateFinalizeAddress.ToInt64():X}");
            NamePlateDebuffsPlugin.Log.Verbose($"{nameof(AddonNamePlateDrawAddress)} {AddonNamePlateDrawAddress.ToInt64():X}");
        }
    }
}
