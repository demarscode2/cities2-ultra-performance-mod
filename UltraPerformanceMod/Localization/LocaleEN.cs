using System.Collections.Generic;
using Colossal;
using UltraPerformanceMod.Systems;

namespace UltraPerformanceMod.Localization
{
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public void Unload()
        {
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod Title and Sections
                { m_Setting.GetSettingsLocaleID(), "Ultra Performance & Simulation Booster" },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.TabPresets), "Presets" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabVisibility), "Visibility" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabServices), "Services" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabTraffic), "Traffic" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabGraphics), "Graphics & Environment" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabActions), "Quick Actions" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabAbout), "About" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupPresets), "Optimization Presets" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupVisibility), "Entity & Mesh Rendering" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupServices), "City Services Automation" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupTraffic), "Traffic & Pathfinding Optimizations" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupGraphics), "Water, Trees & Visual Effects" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupActions), "Instant Manual Cleanup" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupAbout), "Mod Information" },

                // Presets
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Preset)), "Performance Preset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Preset)), "Choose a performance preset: Basic (essential simulation fixes), Medium (balanced & recommended for large cities), or Maximum (extreme FPS for 500k-1M megalopolises)." },
                { m_Setting.GetEnumValueLocaleID(PerformancePreset.Custom), "Custom" },
                { m_Setting.GetEnumValueLocaleID(PerformancePreset.Basic), "Basic (Simulation Optimized)" },
                { m_Setting.GetEnumValueLocaleID(PerformancePreset.Medium), "Medium (Balanced Recommended)" },
                { m_Setting.GetEnumValueLocaleID(PerformancePreset.Maximum), "Maximum (Extreme FPS / Megalopolis)" },

                // Visibility
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleNPCs)), "Invisible NPCs / Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleNPCs)), "Hides citizen rendering and eliminates skeletal animation skinning calculations on CPU/GPU. Simulation continues normally." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleVehicles)), "Invisible All Vehicles" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleVehicles)), "Hides 3D models for cars, trucks, and trailers, providing massive FPS improvements in dense traffic." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleBicycles)), "Invisible Bicycles" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleBicycles)), "Hides cyclists and bicycles individually from 4-wheeled motor vehicles." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleMotorcycles)), "Invisible Motorcycles" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleMotorcycles)), "Hides motorcycles and mopeds individually from regular cars." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AnimalHandling)), "Animal Management (Dogs/Wildlife)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AnimalHandling)), "Control household pets and wildlife: Vanilla (visible), Invisible (active in simulation, zero draw calls), or Remove Completely (despawns animals to save CPU/memory)." },
                { m_Setting.GetEnumValueLocaleID(AnimalHandlingMode.KeepVanilla), "Vanilla (Visible)" },
                { m_Setting.GetEnumValueLocaleID(AnimalHandlingMode.Invisible), "Invisible (Zero Render Cost)" },
                { m_Setting.GetEnumValueLocaleID(AnimalHandlingMode.RemoveCompletely), "Remove Completely (Maximum Performance)" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleTrees)), "Invisible Trees" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleTrees)), "Hides all trees on the map, eliminating hundreds of thousands of GPU draw calls." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HideNotificationIcons)), "Hide Warning / Error Icons" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HideNotificationIcons)), "Hides all 3D floating warning icons (lack of workers, customers, high rent, etc.), clearing the skyline and saving draw calls." },

                // Services
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoGarbage)), "Automatic Garbage Collection" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoGarbage)), "Instantly clears building garbage accumulation and removes roaming garbage trucks from roads." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoDeathcare)), "Automatic Deathcare (Hearse)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoDeathcare)), "Processes deceased citizens automatically without requiring hearses to navigate traffic." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoRoadMaintenance)), "Automatic Road Maintenance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoRoadMaintenance)), "Restores road surface wear to 100% and purges slow maintenance trucks from highways." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoParkMaintenance)), "Automatic Park Maintenance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoParkMaintenance)), "Keeps all parks and plazas fully maintained without service vehicle traffic." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoPolice)), "Automatic Police (Instant Security)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoPolice)), "Eliminates crime and avoids police cars getting stuck in traffic jams." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoAmbulance)), "Automatic Healthcare / Ambulance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoAmbulance)), "Heals sick and injured citizens instantly without ambulances caught in gridlock." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoPostService)), "Automatic Post / Mail Service" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoPostService)), "Processes citizen and mailbox letters automatically without post vans on the roads." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InstantConstruction)), "Instant Building Construction" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InstantConstruction)), "Instantly finalizes newly zoned buildings and level-up upgrades without cranes or scaffold delays." },

                // Traffic
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableOutsideTaxis)), "Block Outside Taxis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableOutsideTaxis)), "Prevents thousands of empty foreign taxis from crossing intercity highways into the city." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableDummyTraffic)), "Remove Dummy / Through Traffic" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableDummyTraffic)), "Removes artificial pass-through vehicles that do not reside or consume in your city." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OptimizeParking)), "Optimize Parking" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OptimizeParking)), "Clears cars parked in invalid positions or sidewalks that could block intersections." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoClearDeadlocks)), "Auto Clear Traffic Deadlocks" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoClearDeadlocks)), "Detects frozen vehicles stuck in traffic gridlocks and despawns them, eliminating pathfinding recalculation spikes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeadlockTimeoutSeconds)), "Stuck Vehicle Timeout (seconds)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DeadlockTimeoutSeconds)), "Duration in seconds a vehicle must be immobilized on a lane segment before being considered deadlocked." },

                // Graphics & Environment
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableHeavyEffects)), "Reduce Heavy Visual Effects" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableHeavyEffects)), "Disables dense smoke, volumetric pollution, and complex particle systems." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableTreeWind)), "Disable Tree Wind Sway" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableTreeWind)), "Zeroes vertex sway deformation on trees, reducing vertex shader GPU overhead." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableTreeGrowth)), "Disable Tree Growth & Aging" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableTreeGrowth)), "Pauses decorative tree aging and decay simulation, saving daily ECS cycles while keeping forestry industry active." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.WaterSimulation)), "Water Simulation Flow" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.WaterSimulation)), "Vanilla (full fluid simulation every frame), Throttled (updates fluid dynamics every 6 frames to save CPU/GPU), or Disabled (fully static water mesh)." },
                { m_Setting.GetEnumValueLocaleID(WaterSimulationMode.Vanilla), "Vanilla (Full Simulation)" },
                { m_Setting.GetEnumValueLocaleID(WaterSimulationMode.Throttled), "Throttled (Smooth every 6 frames)" },
                { m_Setting.GetEnumValueLocaleID(WaterSimulationMode.Disabled), "Disabled (Static / Maximum FPS)" },

                // Quick Action Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanGarbageButton)), "Clear All City Garbage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanGarbageButton)), "Immediately resets garbage on all properties and removes trucks from streets." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CollectDeceasedButton)), "Collect All Deceased Bodies" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CollectDeceasedButton)), "Instantly processes all deceased citizens across the city." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MaintainRoadsAndParksButton)), "Repair All Roads & Parks" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MaintainRoadsAndParksButton)), "Restores road condition to 100% and cleans all green spaces." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HealAndProtectCityButton)), "Heal Citizens & Clear Crime Now" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HealAndProtectCityButton)), "Cures all sick/injured citizens and eliminates pending crimes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeliverAllMailButton)), "Deliver All Mail Now" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DeliverAllMailButton)), "Clears accumulated letters and empties mailboxes across the map." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearTrafficDeadlocksButton)), "Clear Traffic Deadlocks & Blockages" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearTrafficDeadlocksButton)), "Despawns all vehicles frozen in gridlocks for over 15 seconds." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearAccidentsButton)), "Clear All Traffic Accidents" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearAccidentsButton)), "Removes accident sites and frees blocked road lanes immediately." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeTrailersButton)), "Purge Orphan Trailers (Yo-Yo Fix)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeTrailersButton)), "Deletes detached trailers and cargo containers bouncing or frozen on roads." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeAllAnimalsButton)), "Purge All Animals" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeAllAnimalsButton)), "Removes all active domestic pets and wild animals from the map." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InstantCompleteAllBuildingsButton)), "Finish Under-Construction Buildings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InstantCompleteAllBuildingsButton)), "Instantly completes any building currently undergoing construction or upgrade." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeOutsideTaxisButton)), "Purge Active Outside Taxis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeOutsideTaxisButton)), "Despawns foreign taxis currently roaming city streets." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeDummyTrafficButton)), "Purge Active Dummy Traffic" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeDummyTrafficButton)), "Removes all artificial pass-through cars." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeParkedCarsButton)), "Purge Invalid Parked Cars" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeParkedCarsButton)), "Removes abandoned or illegally parked vehicles." },

                // About
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersionInfo)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersionInfo)), "Ultra Performance Mod build for Cities: Skylines II." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModStatusInfo)), "Current State" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModStatusInfo)), "Displays active preset and simulation modules status." },
            };
        }
    }
}
