using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;

namespace NamePlateDebuffs.StatusNode
{
    public unsafe class StatusNodeManager : IDisposable
    {
        private NamePlateDebuffsPlugin _plugin;

        private AddonNamePlate* namePlateAddon;

        private StatusNodeGroup[] NodeGroups;

        private ExcelSheet<Status> StatusSheet;

        private static byte NamePlateCount = 50;
        private static uint StartingNodeId = 50000;

        public bool Built { get; private set; }

        internal StatusNodeManager(NamePlateDebuffsPlugin p)
        {
            _plugin = p; 

            NodeGroups = new StatusNodeGroup[NamePlateCount];

            StatusSheet = NamePlateDebuffsPlugin.DataManager.GetExcelSheet<Status>();
        }

        public void Dispose()
        {
            DestroyNodes();
        }

        public void SetNamePlateAddonPointer(AddonNamePlate* addon)
        {
            namePlateAddon = addon;
        }

        public void ForEachGroup(Action<StatusNodeGroup> func)
        {
            foreach(StatusNodeGroup group in NodeGroups)
                if (group != null)
                    func(group);
        }

        public void ForEachNode(Action<StatusNode> func)
        {
            foreach (StatusNodeGroup group in NodeGroups)
                group?.ForEachNode(func);
        }

        public void SetGroupVisibility(int index, bool enable, bool setChildren = false)
        {
            StatusNodeGroup group = NodeGroups[index];

            group?.SetVisibility(enable, setChildren);
        }

        public void SetStatus(int groupIndex, int statusIndex, int id, int timer)
        {
            Status row = StatusSheet.GetRow((uint) id);

            if (row == null)
                return;

            StatusNodeGroup group = NodeGroups[groupIndex];

            group?.SetStatus(statusIndex, (int)row.Icon, timer);
        }

        public void HideUnusedStatus(int groupIndex, int statusCount)
        {
            StatusNodeGroup group = NodeGroups[groupIndex];

            group?.HideUnusedStatus(statusCount);
        }

        public void SetDepthPriority(int groupIndex, bool enable)
        {
            StatusNodeGroup group = NodeGroups[groupIndex];

            if (group == null)
                return;

            group.RootNode->SetUseDepthBasedPriority(enable);

            group.ForEachNode(node =>
            {
                node.RootNode->SetUseDepthBasedPriority(enable);
                node.DurationNode->AtkResNode.SetUseDepthBasedPriority(enable);
                node.IconNode->AtkResNode.SetUseDepthBasedPriority(enable);
            });
        }

        public void LoadConfig()
        {
            ForEachNode(node => node.LoadConfig());
            ForEachGroup(group => group.LoadConfig());
        }

        public bool BuildNodes(bool rebuild = false)
        {
            if (namePlateAddon == null) return false;
            if (Built && !rebuild) return true;
            if (rebuild) DestroyNodes();
 
            for(byte i = 0; i < NamePlateCount; i++)
            {
                StatusNodeGroup nodeGroup = new(_plugin);
                var npObj = &namePlateAddon->NamePlateObjectArray[i];
                if (!nodeGroup.BuildNodes(StartingNodeId))
                {
                    DestroyNodes();
                    return false;
                }
                var npComponent = npObj->RootNode->Component;

                var lastChild = npComponent->UldManager.RootNode;
                while (lastChild->PrevSiblingNode != null) lastChild = lastChild->PrevSiblingNode;

                lastChild->PrevSiblingNode = nodeGroup.RootNode;
                nodeGroup.RootNode->NextSiblingNode = lastChild;
                nodeGroup.RootNode->ParentNode = (AtkResNode*) npObj->RootNode;

                npComponent->UldManager.UpdateDrawNodeList();

                NodeGroups[i] = nodeGroup;
            }

            Built = true;

            return true;
        }

        public void DestroyNodes()
        {
            if (namePlateAddon == null) return;

            for(byte i = 0; i < NamePlateCount; i++)
            {
                var npObj = &namePlateAddon->NamePlateObjectArray[i];
                var npComponent = npObj->RootNode->Component;

                if (NodeGroups[i] != null)
                {
                    var lastDefaultNode = NodeGroups[i].RootNode->NextSiblingNode;
                    lastDefaultNode->PrevSiblingNode = null;
                    NodeGroups[i].DestroyNodes();
                }
                NodeGroups[i] = null;

                npComponent->UldManager.UpdateDrawNodeList();
            }

            Built = false;
        }
    }
}
