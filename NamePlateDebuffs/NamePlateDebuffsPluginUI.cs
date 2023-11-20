﻿using ImGuiNET;
using System;
using System.Numerics;


namespace NamePlateDebuffs
{
    public class NamePlateDebuffsPluginUI : IDisposable
    {
        private readonly NamePlateDebuffsPlugin _plugin;

        private bool ConfigOpen = false;
        
        public bool IsConfigOpen => ConfigOpen;

        public NamePlateDebuffsPluginUI(NamePlateDebuffsPlugin p)
        {
            _plugin = p;

            _plugin.Interface.UiBuilder.OpenConfigUi += UiBuilder_OnOpenConfigUi;
            _plugin.Interface.UiBuilder.Draw += UiBuilder_OnBuild;
        }

        public void Dispose()
        {
            _plugin.Interface.UiBuilder.OpenConfigUi -= UiBuilder_OnOpenConfigUi;
            _plugin.Interface.UiBuilder.Draw -= UiBuilder_OnBuild;
        }

        public void ToggleConfig()
        {
            ConfigOpen = !ConfigOpen;
        }

        public void UiBuilder_OnOpenConfigUi() => ConfigOpen = true;

        public void UiBuilder_OnBuild()
        {
            if (!ConfigOpen)
                return;

            ImGui.SetNextWindowSize(new Vector2(420, 647), ImGuiCond.Always);

            if (!ImGui.Begin(_plugin.Name, ref ConfigOpen, ImGuiWindowFlags.NoResize))
            {
                ImGui.End();
                return;
            }

            bool needSave = false;

            if (ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
            {
                needSave |= ImGui.Checkbox("Enabled", ref _plugin.Config.Enabled);
                needSave |= ImGui.InputInt("Update Interval (ms)", ref _plugin.Config.UpdateInterval, 10);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Interval between status updates in milliseconds");
                if (ImGui.Button("Reset Config to Defaults"))
                {
                    _plugin.Config.SetDefaults();
                    needSave = true;
                }
                ImGui.Text("While config is open, test nodes are displayed to help with configuration.");
            }

            if (ImGui.CollapsingHeader("Node Group", ImGuiTreeNodeFlags.DefaultOpen))
            {
                needSave |= ImGui.Checkbox("Fill From Right", ref _plugin.Config.FillFromRight);
                needSave |= ImGui.SliderInt("X Offset", ref _plugin.Config.GroupX, -200, 200);
                needSave |= ImGui.SliderInt("Y Offset", ref _plugin.Config.GroupY, -200, 200);
                needSave |= ImGui.SliderInt("Node Spacing", ref _plugin.Config.NodeSpacing, -5, 30);
                needSave |= ImGui.SliderFloat("Group Scale", ref _plugin.Config.Scale, 0.01F, 3.0F);
            }

            if (ImGui.CollapsingHeader("Node", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Text("Try and maintain a 3:4 ratio of Icon Width:Icon Height for best results.");
                needSave |= ImGui.SliderInt("Icon X Offset", ref _plugin.Config.IconX, -200, 200);
                needSave |= ImGui.SliderInt("Icon Y Offset", ref _plugin.Config.IconY, -200, 200);
                needSave |= ImGui.SliderInt("Icon Width", ref _plugin.Config.IconWidth, 5, 100);
                needSave |= ImGui.SliderInt("Icon Height", ref _plugin.Config.IconHeight, 5, 100);
                needSave |= ImGui.SliderInt("Duration X Offset", ref _plugin.Config.DurationX, -200, 200);
                needSave |= ImGui.SliderInt("Duration Y Offset", ref _plugin.Config.DurationY, -200, 200);
                needSave |= ImGui.SliderInt("Duration Font Size", ref _plugin.Config.FontSize, 1, 60);
                needSave |= ImGui.SliderInt("Duration Padding", ref _plugin.Config.DurationPadding, -100, 100);

                needSave |= ImGui.ColorEdit4("Duration Text Color", ref _plugin.Config.DurationTextColor);
                needSave |= ImGui.ColorEdit4("Duration Edge Color", ref _plugin.Config.DurationEdgeColor);
            }

            if (needSave)
            {
                _plugin.StatusNodeManager.LoadConfig();
                _plugin.Config.Save();
            }


            ImGui.End();
        }
    }
}
