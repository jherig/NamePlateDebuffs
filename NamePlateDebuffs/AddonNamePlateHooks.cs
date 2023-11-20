using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Diagnostics;

namespace NamePlateDebuffs
{
    public unsafe class AddonNamePlateHooks : IDisposable
    {
        private readonly NamePlateDebuffsPlugin _plugin;

        private delegate void AddonNamePlateFinalizePrototype(AddonNamePlate* thisPtr);
        private Hook<AddonNamePlateFinalizePrototype> _hookAddonNamePlateFinalize;

        private delegate void AddonNamePlateDrawPrototype(AddonNamePlate* thisPtr);
        private Hook<AddonNamePlateDrawPrototype> _hookAddonNamePlateDraw;

        private readonly Stopwatch _timer;
        private long _elapsed;

        private UI3DModule.ObjectInfo* _lastTarget;

        public AddonNamePlateHooks(NamePlateDebuffsPlugin p)
        {
            _plugin = p;

            _timer = new Stopwatch();
            _elapsed = 0;
        }

        public void Initialize()
        {
            _hookAddonNamePlateFinalize = NamePlateDebuffsPlugin.Hook.HookFromAddress<AddonNamePlateFinalizePrototype>(_plugin.Address.AddonNamePlateFinalizeAddress, AddonNamePlateFinalizeDetour);
            _hookAddonNamePlateDraw = NamePlateDebuffsPlugin.Hook.HookFromAddress<AddonNamePlateDrawPrototype>(_plugin.Address.AddonNamePlateDrawAddress, AddonNamePlateDrawDetour);

            _hookAddonNamePlateFinalize.Enable();
            _hookAddonNamePlateDraw.Enable();
        }

        public void Dispose()
        {
            _hookAddonNamePlateFinalize.Dispose();
            _hookAddonNamePlateDraw.Dispose();
        }

        public void AddonNamePlateDrawDetour(AddonNamePlate* thisPtr)
        {
            if (!_plugin.Config.Enabled || _plugin.InPvp)
            {
                if (_timer.IsRunning)
                {
                    _timer.Stop();
                    _timer.Reset();
                    _elapsed = 0;
                }

                if (_plugin.StatusNodeManager.Built)
                {
                    _plugin.StatusNodeManager.DestroyNodes();
                    _plugin.StatusNodeManager.SetNamePlateAddonPointer(null);
                }

                _hookAddonNamePlateDraw.Original(thisPtr);
                return;
            }

            _elapsed += _timer.ElapsedMilliseconds;
            _timer.Restart();

            if (_elapsed >= _plugin.Config.UpdateInterval)
            {
                if (!_plugin.StatusNodeManager.Built)
                {
                    _plugin.StatusNodeManager.SetNamePlateAddonPointer(thisPtr);
                    if (!_plugin.StatusNodeManager.BuildNodes())
                        return;
                }

                var framework = Framework.Instance();
                var ui3DModule = framework->GetUiModule()->GetUI3DModule();

                for (int i = 0; i < ui3DModule->NamePlateObjectInfoCount; i++)
                {
                    var objectInfo = ((UI3DModule.ObjectInfo**)ui3DModule->NamePlateObjectInfoPointerArray)[i];

                    // As of at least 6.28 this field has the same value as NamePlateObjectKind: 
                    //var npIndex = objectInfo->NamePlateIndex;

                    // Temporary work around until Dalamud 7.2.0 is pushed to xl, see https://github.com/goatcorp/Dalamud/commit/a173c5dac54fa4b55f5c5066bdcd93cbbcd74ed9
                    //var npIndex = objectInfo->Unk_4F;
                    var npIndex = *(&objectInfo->NamePlateObjectKind + 2); // Same as Unk_4F, but after 7.2.0 the Unk_4F field will be going away
                    
                    if (objectInfo->NamePlateObjectKind != 3)
                    {
                        _plugin.StatusNodeManager.SetGroupVisibility(npIndex, false, true);
                        continue;
                    }

                    _plugin.StatusNodeManager.SetGroupVisibility(npIndex, true, false);

                    if (_plugin.UI.IsConfigOpen)
                    {
                        _plugin.StatusNodeManager.ForEachNode(node => node.SetStatus(StatusNode.StatusNode.DefaultIconId, 20));
                    }
                    else
                    {
                        uint? localPlayerId = _plugin.ClientState.LocalPlayer?.ObjectId;
                        if (localPlayerId is null)
                        {
                            _plugin.StatusNodeManager.HideUnusedStatus(npIndex, 0);
                            continue;
                        }
                        StatusManager targetStatus = ((BattleChara*)objectInfo->GameObject)->GetStatusManager[0];

                        var statusArray = (Status*)targetStatus.Status;

                        int count = 0;

                        for (int j = 0; j < 30; j++)
                        {
                            Status status = statusArray[j];
                            if (status.StatusID == 0) continue;
                            if (status.SourceID != localPlayerId) continue;

                            _plugin.StatusNodeManager.SetStatus(npIndex, count, status.StatusID, (int)status.RemainingTime);
                            count++;

                            if (count == 4)
                                break;
                        }

                        _plugin.StatusNodeManager.HideUnusedStatus(npIndex, count);
                    }

                    if (objectInfo == ui3DModule->TargetObjectInfo && objectInfo != _lastTarget)
                    {
                        _plugin.StatusNodeManager.SetDepthPriority(npIndex, false);
                        if (_lastTarget != null)
                            _plugin.StatusNodeManager.SetDepthPriority(_lastTarget->NamePlateIndex, true);
                        _lastTarget = objectInfo;
                    }
                }

                _elapsed = 0;
            }

            _hookAddonNamePlateDraw.Original(thisPtr);
        }

        public void AddonNamePlateFinalizeDetour(AddonNamePlate* thisPtr)
        {
            _plugin.StatusNodeManager.DestroyNodes();
            _plugin.StatusNodeManager.SetNamePlateAddonPointer(null);
            _hookAddonNamePlateFinalize.Original(thisPtr);
        }
    }
}
