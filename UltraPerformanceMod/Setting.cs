using System;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using UltraPerformanceMod.Systems;

namespace UltraPerformanceMod
{
    public enum PerformancePreset
    {
        Custom = 0,
        Basic = 1,
        Medium = 2,
        Maximum = 3
    }

    [FileLocation("ModsSettings/UltraPerformanceMod/UltraPerformanceMod")]
    [SettingsUITabOrder(TabPresets, TabVisibility, TabServices, TabTraffic, TabGraphics, TabActions, TabAbout)]
    [SettingsUIGroupOrder(GroupPresets, GroupVisibility, GroupServices, GroupTraffic, GroupGraphics, GroupActions, GroupAbout)]
    [SettingsUIShowGroupName(GroupPresets, GroupVisibility, GroupServices, GroupTraffic, GroupGraphics, GroupActions, GroupAbout)]
    public class Setting : ModSetting
    {
        public const string TabPresets = "Presets";
        public const string TabVisibility = "Visibility";
        public const string TabServices = "Services";
        public const string TabTraffic = "Traffic";
        public const string TabGraphics = "Graphics";
        public const string TabActions = "Actions";
        public const string TabAbout = "About";

        public const string GroupPresets = "PresetGroup";
        public const string GroupVisibility = "VisibilityGroup";
        public const string GroupServices = "ServicesGroup";
        public const string GroupTraffic = "TrafficGroup";
        public const string GroupGraphics = "GraphicsGroup";
        public const string GroupActions = "ActionsGroup";
        public const string GroupAbout = "AboutGroup";

        private PerformancePreset m_Preset = PerformancePreset.Medium;

        // Visibility
        private bool m_InvisibleNPCs = false;
        private bool m_InvisibleVehicles = false;
        private bool m_InvisibleBicycles = false;
        private bool m_InvisibleMotorcycles = false;
        private AnimalHandlingMode m_AnimalHandling = AnimalHandlingMode.Invisible;
        private bool m_HideNotificationIcons = false;
        private bool m_DisableHoverOverlay = false;

        // Services
        private bool m_AutoGarbage = true;
        private bool m_AutoDeathcare = true;
        private bool m_AutoRoadMaintenance = true;
        private bool m_AutoParkMaintenance = true;
        private bool m_AutoPolice = true;
        private bool m_AutoAmbulance = true;
        private bool m_AutoPostService = true;
        private bool m_InstantConstruction = false;
        private bool m_AutoEvacuateHomeless = true;

        // Traffic & Pathfinding
        private bool m_DisableOutsideTaxis = true;
        private bool m_DisableDummyTraffic = true;
        private bool m_OptimizeParking = true;
        private bool m_AutoClearDeadlocks = false;
        private int m_DeadlockTimeoutSeconds = 90;

        // Graphics & Simulation
        private bool m_DisableHeavyEffects = true;
        private bool m_DisableTreeWind = true;
        private bool m_DisableTreeGrowth = true;
        private bool m_OptimizeShadows = true;

        public event Action? OnSettingChanged;

        public Setting(IMod mod) : base(mod)
        {
            ApplyPreset(m_Preset);
        }

        public override void ApplyAndSave()
        {
            base.ApplyAndSave();
            OnSettingChanged?.Invoke();
        }

        // --- PRESETS TAB ---
        [SettingsUISection(TabPresets, GroupPresets)]
        public PerformancePreset Preset
        {
            get => m_Preset;
            set
            {
                if (m_Preset != value)
                {
                    m_Preset = value;
                    if (value != PerformancePreset.Custom)
                    {
                        ApplyPreset(value);
                    }
                    ApplyAndSave();
                }
            }
        }

        // --- VISIBILITY TAB ---
        [SettingsUISection(TabVisibility, GroupVisibility)]
        public bool InvisibleNPCs
        {
            get => m_InvisibleNPCs;
            set
            {
                if (m_InvisibleNPCs != value)
                {
                    m_InvisibleNPCs = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabVisibility, GroupVisibility)]
        public bool InvisibleVehicles
        {
            get => m_InvisibleVehicles;
            set
            {
                if (m_InvisibleVehicles != value)
                {
                    m_InvisibleVehicles = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabVisibility, GroupVisibility)]
        public bool InvisibleBicycles
        {
            get => m_InvisibleBicycles;
            set
            {
                if (m_InvisibleBicycles != value)
                {
                    m_InvisibleBicycles = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabVisibility, GroupVisibility)]
        public bool InvisibleMotorcycles
        {
            get => m_InvisibleMotorcycles;
            set
            {
                if (m_InvisibleMotorcycles != value)
                {
                    m_InvisibleMotorcycles = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabVisibility, GroupVisibility)]
        public AnimalHandlingMode AnimalHandling
        {
            get => m_AnimalHandling;
            set
            {
                if (m_AnimalHandling != value)
                {
                    m_AnimalHandling = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabVisibility, GroupVisibility)]
        public bool HideNotificationIcons
        {
            get => m_HideNotificationIcons;
            set
            {
                if (m_HideNotificationIcons != value)
                {
                    m_HideNotificationIcons = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabVisibility, GroupVisibility)]
        public bool DisableHoverOverlay
        {
            get => m_DisableHoverOverlay;
            set
            {
                if (m_DisableHoverOverlay != value)
                {
                    m_DisableHoverOverlay = value;
                    m_Preset = PerformancePreset.Custom;
                    HoverOverlayOptimizerSystem.Instance?.SetOutlinesPassEnabled(!value);
                    ApplyAndSave();
                }
            }
        }

        // --- SERVICES TAB ---
        [SettingsUISection(TabServices, GroupServices)]
        public bool AutoGarbage
        {
            get => m_AutoGarbage;
            set
            {
                if (m_AutoGarbage != value)
                {
                    m_AutoGarbage = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabServices, GroupServices)]
        public bool AutoDeathcare
        {
            get => m_AutoDeathcare;
            set
            {
                if (m_AutoDeathcare != value)
                {
                    m_AutoDeathcare = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabServices, GroupServices)]
        public bool AutoRoadMaintenance
        {
            get => m_AutoRoadMaintenance;
            set
            {
                if (m_AutoRoadMaintenance != value)
                {
                    m_AutoRoadMaintenance = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabServices, GroupServices)]
        public bool AutoParkMaintenance
        {
            get => m_AutoParkMaintenance;
            set
            {
                if (m_AutoParkMaintenance != value)
                {
                    m_AutoParkMaintenance = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabServices, GroupServices)]
        public bool AutoPolice
        {
            get => m_AutoPolice;
            set
            {
                if (m_AutoPolice != value)
                {
                    m_AutoPolice = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabServices, GroupServices)]
        public bool AutoAmbulance
        {
            get => m_AutoAmbulance;
            set
            {
                if (m_AutoAmbulance != value)
                {
                    m_AutoAmbulance = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabServices, GroupServices)]
        public bool AutoPostService
        {
            get => m_AutoPostService;
            set
            {
                if (m_AutoPostService != value)
                {
                    m_AutoPostService = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabServices, GroupServices)]
        public bool InstantConstruction
        {
            get => m_InstantConstruction;
            set
            {
                if (m_InstantConstruction != value)
                {
                    m_InstantConstruction = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabServices, GroupServices)]
        public bool AutoEvacuateHomeless
        {
            get => m_AutoEvacuateHomeless;
            set
            {
                if (m_AutoEvacuateHomeless != value)
                {
                    m_AutoEvacuateHomeless = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        // --- TRAFFIC TAB ---
        [SettingsUISection(TabTraffic, GroupTraffic)]
        public bool DisableOutsideTaxis
        {
            get => m_DisableOutsideTaxis;
            set
            {
                if (m_DisableOutsideTaxis != value)
                {
                    m_DisableOutsideTaxis = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabTraffic, GroupTraffic)]
        public bool DisableDummyTraffic
        {
            get => m_DisableDummyTraffic;
            set
            {
                if (m_DisableDummyTraffic != value)
                {
                    m_DisableDummyTraffic = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabTraffic, GroupTraffic)]
        public bool OptimizeParking
        {
            get => m_OptimizeParking;
            set
            {
                if (m_OptimizeParking != value)
                {
                    m_OptimizeParking = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabTraffic, GroupTraffic)]
        public bool AutoClearDeadlocks
        {
            get => m_AutoClearDeadlocks;
            set
            {
                if (m_AutoClearDeadlocks != value)
                {
                    m_AutoClearDeadlocks = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISlider(min = 30f, max = 300f, step = 15f)]
        [SettingsUISection(TabTraffic, GroupTraffic)]
        public int DeadlockTimeoutSeconds
        {
            get => m_DeadlockTimeoutSeconds;
            set
            {
                if (m_DeadlockTimeoutSeconds != value)
                {
                    m_DeadlockTimeoutSeconds = value;
                    ApplyAndSave();
                }
            }
        }

        // --- GRAPHICS TAB ---
        [SettingsUISection(TabGraphics, GroupGraphics)]
        public bool DisableHeavyEffects
        {
            get => m_DisableHeavyEffects;
            set
            {
                if (m_DisableHeavyEffects != value)
                {
                    m_DisableHeavyEffects = value;
                    m_Preset = PerformancePreset.Custom;
                    VisualEffectsOptimizerSystem.ApplyEffectsOptimization(value);
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabGraphics, GroupGraphics)]
        public bool DisableTreeWind
        {
            get => m_DisableTreeWind;
            set
            {
                if (m_DisableTreeWind != value)
                {
                    m_DisableTreeWind = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabGraphics, GroupGraphics)]
        public bool DisableTreeGrowth
        {
            get => m_DisableTreeGrowth;
            set
            {
                if (m_DisableTreeGrowth != value)
                {
                    m_DisableTreeGrowth = value;
                    m_Preset = PerformancePreset.Custom;
                    ApplyAndSave();
                }
            }
        }

        [SettingsUISection(TabGraphics, GroupGraphics)]
        public bool OptimizeShadows
        {
            get => m_OptimizeShadows;
            set
            {
                if (m_OptimizeShadows != value)
                {
                    m_OptimizeShadows = value;
                    m_Preset = PerformancePreset.Custom;
                    ShadowOptimizerSystem.ApplyShadowOptimization(value);
                    ApplyAndSave();
                }
            }
        }

        // --- ACTIONS TAB (MANUAL BUTTONS) ---
        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool CleanGarbageButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: CleanGarbageNow triggered.");
                AutoGarbageSystem.Instance?.CleanAllGarbageNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool CollectDeceasedButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: CollectAllDeceasedNow triggered.");
                AutoDeathcareSystem.Instance?.CollectAllDeceasedNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool MaintainRoadsAndParksButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: MaintainRoadsAndParksNow triggered.");
                AutoMaintenanceSystem.Instance?.MaintainAllNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool HealAndProtectCityButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: HealAndProtectCityNow triggered.");
                AutoEmergencyServicesSystem.Instance?.HealAndProtectCityNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool DeliverAllMailButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: DeliverAllMailNow triggered.");
                AutoPostServiceSystem.Instance?.DeliverAllMailNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool ClearTrafficDeadlocksButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: ClearTrafficDeadlocksNow triggered.");
                TrafficDeadlockBreakerSystem.Instance?.ClearAllTrafficDeadlocksNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool ClearAccidentsButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: ClearAccidentsNow triggered.");
                TrafficDeadlockBreakerSystem.Instance?.ClearAllAccidentsNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool PurgeTrailersButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: PurgeOrphanTrailersNow triggered.");
                TrailerCleanupSystem.Instance?.PurgeOrphanTrailersNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool PurgeAllAnimalsButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: PurgeAllAnimalsNow triggered.");
                AnimalManagerSystem.Instance?.PurgeAllAnimalsNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool InstantCompleteAllBuildingsButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: CompleteAllConstructionNow triggered.");
                InstantConstructionSystem.Instance?.CompleteAllConstructionNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool PurgeOutsideTaxisButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: PurgeOutsideTaxisNow triggered.");
                OutsideTaxiLimiterSystem.Instance?.PurgeOutsideTaxisNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool PurgeDummyTrafficButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: PurgeDummyTrafficNow triggered.");
                DummyTrafficRemoverSystem.Instance?.PurgeDummyTrafficNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool PurgeParkedCarsButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: PurgeInvalidParkedCarsNow triggered.");
                ParkingOptimizerSystem.Instance?.PurgeInvalidParkedCarsNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool PurgeHomelessButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: PurgeHomelessNow triggered.");
                HomelessOptimizerSystem.Instance?.PurgeStrandedHomelessNow();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(TabActions, GroupActions)]
        public bool ClearStuckPedestriansButton
        {
            set
            {
                Mod.log.Info("[UltraPerformanceMod] Action: ClearStuckPedestriansNow triggered.");
                StuckPedestrianOptimizerSystem.Instance?.ClearStuckPedestriansNow();
            }
        }

        // --- ABOUT TAB ---
        [SettingsUISection(TabAbout, GroupAbout)]
        public string ModVersionInfo => "Ultra Performance & Simulation Booster v1.3.4 (Paradox Mods Ready)";

        [SettingsUISection(TabAbout, GroupAbout)]
        public string ModStatusInfo => $"Current Preset: {m_Preset} | Simulation Optimizations Active";

        public void ApplyPreset(PerformancePreset preset)
        {
            switch (preset)
            {
                case PerformancePreset.Basic:
                    m_InvisibleNPCs = false;
                    m_InvisibleVehicles = false;
                    m_InvisibleBicycles = false;
                    m_InvisibleMotorcycles = false;
                    m_AnimalHandling = AnimalHandlingMode.KeepVanilla;
                    m_HideNotificationIcons = false;
                    m_DisableHoverOverlay = false;
                    m_AutoGarbage = true;
                    m_AutoDeathcare = true;
                    m_AutoRoadMaintenance = true;
                    m_AutoParkMaintenance = true;
                    m_AutoPolice = false;
                    m_AutoAmbulance = false;
                    m_AutoPostService = true;
                    m_InstantConstruction = false;
                    m_AutoEvacuateHomeless = false;
                    m_DisableOutsideTaxis = true;
                    m_DisableDummyTraffic = true;
                    m_OptimizeParking = true;
                    m_AutoClearDeadlocks = false;
                    m_DeadlockTimeoutSeconds = 120;
                    m_DisableHeavyEffects = false;
                    m_DisableTreeWind = false;
                    m_DisableTreeGrowth = false;
                    m_OptimizeShadows = false;
                    break;

                case PerformancePreset.Medium:
                    m_InvisibleNPCs = false;
                    m_InvisibleVehicles = false;
                    m_InvisibleBicycles = false;
                    m_InvisibleMotorcycles = false;
                    m_AnimalHandling = AnimalHandlingMode.Invisible;
                    m_HideNotificationIcons = true;
                    m_DisableHoverOverlay = true;
                    m_AutoGarbage = true;
                    m_AutoDeathcare = true;
                    m_AutoRoadMaintenance = true;
                    m_AutoParkMaintenance = true;
                    m_AutoPolice = true;
                    m_AutoAmbulance = true;
                    m_AutoPostService = true;
                    m_InstantConstruction = false;
                    m_AutoEvacuateHomeless = true;
                    m_DisableOutsideTaxis = true;
                    m_DisableDummyTraffic = true;
                    m_OptimizeParking = true;
                    m_AutoClearDeadlocks = false;
                    m_DeadlockTimeoutSeconds = 90;
                    m_DisableHeavyEffects = true;
                    m_DisableTreeWind = true;
                    m_DisableTreeGrowth = true;
                    m_OptimizeShadows = true;
                    break;

                case PerformancePreset.Maximum:
                    m_InvisibleNPCs = true;
                    m_InvisibleVehicles = true;
                    m_InvisibleBicycles = true;
                    m_InvisibleMotorcycles = true;
                    m_AnimalHandling = AnimalHandlingMode.RemoveCompletely;
                    m_HideNotificationIcons = true;
                    m_DisableHoverOverlay = true;
                    m_AutoGarbage = true;
                    m_AutoDeathcare = true;
                    m_AutoRoadMaintenance = true;
                    m_AutoParkMaintenance = true;
                    m_AutoPolice = true;
                    m_AutoAmbulance = true;
                    m_AutoPostService = true;
                    m_InstantConstruction = true;
                    m_AutoEvacuateHomeless = true;
                    m_DisableOutsideTaxis = true;
                    m_DisableDummyTraffic = true;
                    m_OptimizeParking = true;
                    m_AutoClearDeadlocks = true;
                    m_DeadlockTimeoutSeconds = 90;
                    m_DisableHeavyEffects = true;
                    m_DisableTreeWind = true;
                    m_DisableTreeGrowth = true;
                    m_OptimizeShadows = true;
                    break;

                case PerformancePreset.Custom:
                    break;
            }

            VisualEffectsOptimizerSystem.ApplyEffectsOptimization(m_DisableHeavyEffects);
            ShadowOptimizerSystem.ApplyShadowOptimization(m_OptimizeShadows);
            HoverOverlayOptimizerSystem.Instance?.SetOutlinesPassEnabled(!m_DisableHoverOverlay);
        }

        public override void SetDefaults()
        {
            m_Preset = PerformancePreset.Medium;
            ApplyPreset(m_Preset);
        }
    }
}
