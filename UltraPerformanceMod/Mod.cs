using System;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using UltraPerformanceMod.Localization;
using UltraPerformanceMod.Systems;

namespace UltraPerformanceMod
{
    public class Mod : IMod
    {
        public const string ModName = "Ultra Performance & Simulation Booster";
        public const string ModId = "UltraPerformanceMod";
        public const string Version = "1.3.4";

        public static readonly ILog log = LogManager.GetLogger(ModId).SetShowsErrorsInUI(false);
        public static Setting? Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info($"[{ModId}] OnLoad started - Version {Version}");

            try
            {
                if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                {
                    log.Info($"[{ModId}] Loaded mod asset from: {asset.path}");
                }

                // 1. Initialize Mod Settings
                Settings = new Setting(this);
                AssetDatabase.global.LoadSettings(ModId, Settings, new Setting(this), false);

                // 2. Register Localization Dictionaries (English & Portuguese)
                RegisterLocalizations();

                // 3. Register Options UI
                Settings.RegisterInOptionsUI();

                // 4. Register ECS Systems in appropriate update phases

                // Phase: PreCulling (Visibility & Rendering Control)
                updateSystem.UpdateAt<NPCVisibilitySystem>(SystemUpdatePhase.PreCulling);
                updateSystem.UpdateAt<VehicleVisibilitySystem>(SystemUpdatePhase.PreCulling);
                updateSystem.UpdateAt<AnimalManagerSystem>(SystemUpdatePhase.PreCulling);
                updateSystem.UpdateAt<TreeManagerSystem>(SystemUpdatePhase.PreCulling);
                updateSystem.UpdateAt<NotificationIconOptimizerSystem>(SystemUpdatePhase.PreCulling);
                updateSystem.UpdateAt<HoverOverlayOptimizerSystem>(SystemUpdatePhase.PreCulling);

                // Phase: GameSimulation (Simulation, Automations & Cleanup)
                updateSystem.UpdateAt<TrailerCleanupSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<AutoGarbageSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<AutoDeathcareSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<AutoMaintenanceSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<AutoEmergencyServicesSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<AutoPostServiceSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<InstantConstructionSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<OutsideTaxiLimiterSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<DummyTrafficRemoverSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<ParkingOptimizerSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<TrafficDeadlockBreakerSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<HomelessOptimizerSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<StuckPedestrianOptimizerSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<WaterSimulationOptimizerSystem>(SystemUpdatePhase.GameSimulation);

                // Phase: MainLoop (Graphics & Volumes)
                updateSystem.UpdateAt<VisualEffectsOptimizerSystem>(SystemUpdatePhase.MainLoop);
                updateSystem.UpdateAt<ShadowOptimizerSystem>(SystemUpdatePhase.MainLoop);

                // Phase: UIUpdate (In-Game HUD Toolbar & Quick Action Panel)
                updateSystem.UpdateAt<InGameUISystem>(SystemUpdatePhase.UIUpdate);

                log.Info($"[{ModId}] Successfully registered all performance systems and settings.");
            }
            catch (Exception ex)
            {
                log.Error($"[{ModId}] Error during OnLoad: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void RegisterLocalizations()
        {
            try
            {
                var lm = GameManager.instance?.localizationManager;
                if (lm == null || Settings == null) return;

                lm.AddSource("en-US", new LocaleEN(Settings));
                lm.AddSource("pt-BR", new LocalePT_BR(Settings));
                lm.AddSource("pt", new LocalePT_BR(Settings));

                log.Info($"[{ModId}] Registered en-US and pt-BR localization sources.");
            }
            catch (Exception ex)
            {
                log.Warn($"[{ModId}] Failed to register localizations: {ex.Message}");
            }
        }

        public void OnDispose()
        {
            log.Info($"[{ModId}] OnDispose triggered.");
            try
            {
                if (Settings != null)
                {
                    Settings.UnregisterInOptionsUI();
                    Settings = null;
                }
            }
            catch (Exception ex)
            {
                log.Warn($"[{ModId}] Exception during OnDispose: {ex.Message}");
            }
        }
    }
}
