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
                { m_Setting.GetOptionTabLocaleID(Setting.TabUltraCore), "UltraCore" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabVisibility), "Visibility" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabServices), "Services" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabTraffic), "Traffic" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabGraphics), "Graphics & Environment" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabActions), "Quick Actions" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabAbout), "About" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupPresets), "Optimization Presets" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupUltraCore), "Internal Performance & Simulation Engine" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupVisibility), "Entity & Mesh Rendering" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupServices), "City Services Automation" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupTraffic), "Traffic & Pathfinding Optimizations" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupGraphics), "Water, Trees & Visual Effects" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupActions), "Instant Manual Cleanup" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupAbout), "Mod Information" },

                // UltraCore
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UltraCoreAutoTuning)), "Adaptive Simulation Auto-Tuner" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UltraCoreAutoTuning)), "Monitors frame latency and simulation step time in real-time, dynamically adapting background task budgets to prevent micro-stutters in mega-cities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UltraCoreCacheEnabled)), "Smart Cache (Traffic Blocker Chains & Queries)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UltraCoreCacheEnabled)), "Memoizes vehicle blocker evaluations and static spatial queries with intelligent TTL expiration, eliminating repetitive CPU recalculations." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearUltraCoreCacheButton)), "Flush Smart Cache" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearUltraCoreCacheButton)), "Instantly clears all cached evaluations in the UltraCore engine to trigger fresh calculations." },

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
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HideNotificationIcons)), "Hide Warning / Error Icons" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HideNotificationIcons)), "Hides all 3D floating warning icons (lack of workers, customers, high rent, etc.), clearing the skyline and saving draw calls." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableHoverOverlay)), "Hide Blue Hover Highlight Outline" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableHoverOverlay)), "Removes the blue/cyan outline border when hovering the cursor over buildings, citizens, vehicles, and objects, saving GPU post-processing passes." },

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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoEvacuateHomeless)), "Automatic Homeless Evacuation" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoEvacuateHomeless)), "Fixes the infamous CS2 pathfind loop bug: periodically evacuates stranded homeless cims that overload pathfind threads in parks and squares." },

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
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableHeavyEffects)), "Disables dense smoke, volumetric pollution and complex particle emitters that lower GPU framerate." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableTreeWind)), "Disable Tree Wind Sway" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableTreeWind)), "Eliminates foliage vertex wind deformations, reducing geometry vertex processing." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableTreeGrowth)), "Disable Tree Growth & Aging" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableTreeGrowth)), "Freezes decorative tree aging calculations, saving daily ECS cycles while keeping forestry extractors functional." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OptimizeShadows)), "Optimize Shadows & HDRP Distance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OptimizeShadows)), "Limits distant shadow calculation to 450m and culls micro-props from shadow passes, boosting GPU framerate in huge cities." },

                // Quick Action Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanGarbageButton)), "Clear All City Garbage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanGarbageButton)), "Instantly zeroes garbage in all buildings and clears garbage trucks from streets." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CollectDeceasedButton)), "Collect All Deceased Bodies" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CollectDeceasedButton)), "Instantly processes all deceased waiting citizens and clears death backlog." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MaintainRoadsAndParksButton)), "Repair All Roads & Parks" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MaintainRoadsAndParksButton)), "Restores 100% pavement wear condition on all roads and rejuvenates parks." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HealAndProtectCityButton)), "Heal Citizens & Clear Crime Now" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HealAndProtectCityButton)), "Heals all sick/injured citizens and immediately clears ongoing criminal events." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeliverAllMailButton)), "Deliver All Mail Now" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DeliverAllMailButton)), "Clears mail backlogs across all mailboxes and delivery post facilities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearTrafficDeadlocksButton)), "Clear Traffic Deadlocks & Jams" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearTrafficDeadlocksButton)), "Despawns vehicles that have been stuck in gridlock for more than 15 seconds." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearAccidentsButton)), "Clear All Accidents From Roads" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearAccidentsButton)), "Removes accident scenes and immediately unblocks obstructed lanes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeTrailersButton)), "Purge Orphan Trailers & Vans (Yo-Yo)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeTrailersButton)), "Deletes orphaned trailers, containers, and delivery van props hopping on roads." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeAllAnimalsButton)), "Purge All Animals" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeAllAnimalsButton)), "Removes all household pets and roaming wild animals from the map." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InstantCompleteAllBuildingsButton)), "Complete Ongoing Construction" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InstantCompleteAllBuildingsButton)), "Instantly finishes any building currently under construction or leveling up." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeOutsideTaxisButton)), "Remove Current Outside Taxis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeOutsideTaxisButton)), "Despawns all foreign intercity taxis currently inside the city borders." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeDummyTrafficButton)), "Remove Current Dummy Traffic" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeDummyTrafficButton)), "Purges all artificial pass-through traffic traversing your roads." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeParkedCarsButton)), "Clear Invalid Parked Cars" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeParkedCarsButton)), "Removes vehicles abandoned or illegally parked on sidewalks and curbs." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeHomelessButton)), "Evacuate Stranded Homeless Cims" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeHomelessButton)), "Instantly clears homeless households stuck in pathfinding calculation loops, restoring CPU simulation speed." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearStuckPedestriansButton)), "Clear Stuck Pedestrians" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearStuckPedestriansButton)), "Despawns pedestrians trapped with lane obstruction glitches on sidewalks or transit stops." },

                // About
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersionInfo)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersionInfo)), "Ultra Performance Mod release compiled for Cities: Skylines II." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModStatusInfo)), "Current State" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModStatusInfo)), "Displays active preset and health status of background simulation modules." },
            };
        }
    }
}
