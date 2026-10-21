using System;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game.UI;
using UltraPerformanceMod;

namespace UltraPerformanceMod.Systems
{
    public partial class InGameUISystem : UISystemBase
    {
        private static readonly ILog modLog = Mod.log;
        public static InGameUISystem? Instance { get; private set; }

        private const string Group = "UltraPerformanceMod";

        private bool m_IsPanelOpen = false;
        private string m_StatusMessage = "";

        // Value bindings
        private ValueBinding<bool> m_IsPanelOpenBinding = default!;
        private ValueBinding<string> m_StatusMessageBinding = default!;
        private ValueBinding<int> m_PresetBinding = default!;

        private ValueBinding<bool> m_InvisibleNPCsBinding = default!;
        private ValueBinding<bool> m_InvisibleVehiclesBinding = default!;
        private ValueBinding<bool> m_InvisibleBicyclesBinding = default!;
        private ValueBinding<bool> m_InvisibleMotorcyclesBinding = default!;
        private ValueBinding<bool> m_InvisibleTreesBinding = default!;
        private ValueBinding<bool> m_InvisibleWildTreesBinding = default!;
        private ValueBinding<bool> m_InvisibleBuildingTreesBinding = default!;
        private ValueBinding<bool> m_InvisibleRoadTreesBinding = default!;
        private ValueBinding<bool> m_DisableTreeWindBinding = default!;
        private ValueBinding<int> m_WaterSimulationBinding = default!;
        private ValueBinding<bool> m_AutoClearDeadlocksBinding = default!;
        private ValueBinding<bool> m_HideNotificationIconsBinding = default!;

        private ValueBinding<bool> m_AutoGarbageBinding = default!;
        private ValueBinding<bool> m_AutoDeathcareBinding = default!;
        private ValueBinding<bool> m_AutoRoadMaintenanceBinding = default!;
        private ValueBinding<bool> m_AutoParkMaintenanceBinding = default!;
        private ValueBinding<bool> m_AutoPoliceBinding = default!;
        private ValueBinding<bool> m_AutoAmbulanceBinding = default!;
        private ValueBinding<bool> m_AutoPostServiceBinding = default!;
        private ValueBinding<bool> m_InstantConstructionBinding = default!;
        private ValueBinding<bool> m_DisableOutsideTaxisBinding = default!;
        private ValueBinding<bool> m_DisableDummyTrafficBinding = default!;
        private ValueBinding<bool> m_OptimizeParkingBinding = default!;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            var s = Mod.Settings;

            // Register Value Bindings
            AddBinding(m_IsPanelOpenBinding = new ValueBinding<bool>(Group, "IsPanelOpen", m_IsPanelOpen));
            AddBinding(m_StatusMessageBinding = new ValueBinding<string>(Group, "StatusMessage", m_StatusMessage));
            AddBinding(m_PresetBinding = new ValueBinding<int>(Group, "Preset", (int)(s?.Preset ?? PerformancePreset.Medium)));

            AddBinding(m_InvisibleNPCsBinding = new ValueBinding<bool>(Group, "InvisibleNPCs", s?.InvisibleNPCs ?? false));
            AddBinding(m_InvisibleVehiclesBinding = new ValueBinding<bool>(Group, "InvisibleVehicles", s?.InvisibleVehicles ?? false));
            AddBinding(m_InvisibleBicyclesBinding = new ValueBinding<bool>(Group, "InvisibleBicycles", s?.InvisibleBicycles ?? false));
            AddBinding(m_InvisibleMotorcyclesBinding = new ValueBinding<bool>(Group, "InvisibleMotorcycles", s?.InvisibleMotorcycles ?? false));
            AddBinding(m_InvisibleTreesBinding = new ValueBinding<bool>(Group, "InvisibleTrees", s?.InvisibleTrees ?? false));
            AddBinding(m_InvisibleWildTreesBinding = new ValueBinding<bool>(Group, "InvisibleWildTrees", s?.InvisibleWildTrees ?? false));
            AddBinding(m_InvisibleBuildingTreesBinding = new ValueBinding<bool>(Group, "InvisibleBuildingTrees", s?.InvisibleBuildingTrees ?? false));
            AddBinding(m_InvisibleRoadTreesBinding = new ValueBinding<bool>(Group, "InvisibleRoadTrees", s?.InvisibleRoadTrees ?? false));
            AddBinding(m_DisableTreeWindBinding = new ValueBinding<bool>(Group, "DisableTreeWind", s?.DisableTreeWind ?? true));
            AddBinding(m_WaterSimulationBinding = new ValueBinding<int>(Group, "WaterSimulation", (int)(s?.WaterSimulation ?? WaterSimulationMode.Media)));
            AddBinding(m_AutoClearDeadlocksBinding = new ValueBinding<bool>(Group, "AutoClearDeadlocks", s?.AutoClearDeadlocks ?? false));
            AddBinding(m_HideNotificationIconsBinding = new ValueBinding<bool>(Group, "HideNotificationIcons", s?.HideNotificationIcons ?? false));

            AddBinding(m_AutoGarbageBinding = new ValueBinding<bool>(Group, "AutoGarbage", s?.AutoGarbage ?? true));
            AddBinding(m_AutoDeathcareBinding = new ValueBinding<bool>(Group, "AutoDeathcare", s?.AutoDeathcare ?? true));
            AddBinding(m_AutoRoadMaintenanceBinding = new ValueBinding<bool>(Group, "AutoRoadMaintenance", s?.AutoRoadMaintenance ?? true));
            AddBinding(m_AutoParkMaintenanceBinding = new ValueBinding<bool>(Group, "AutoParkMaintenance", s?.AutoParkMaintenance ?? true));
            AddBinding(m_AutoPoliceBinding = new ValueBinding<bool>(Group, "AutoPolice", s?.AutoPolice ?? true));
            AddBinding(m_AutoAmbulanceBinding = new ValueBinding<bool>(Group, "AutoAmbulance", s?.AutoAmbulance ?? true));
            AddBinding(m_AutoPostServiceBinding = new ValueBinding<bool>(Group, "AutoPostService", s?.AutoPostService ?? true));
            AddBinding(m_InstantConstructionBinding = new ValueBinding<bool>(Group, "InstantConstruction", s?.InstantConstruction ?? false));
            AddBinding(m_DisableOutsideTaxisBinding = new ValueBinding<bool>(Group, "DisableOutsideTaxis", s?.DisableOutsideTaxis ?? true));
            AddBinding(m_DisableDummyTrafficBinding = new ValueBinding<bool>(Group, "DisableDummyTraffic", s?.DisableDummyTraffic ?? true));
            AddBinding(m_OptimizeParkingBinding = new ValueBinding<bool>(Group, "OptimizeParking", s?.OptimizeParking ?? true));

            // Register Trigger Bindings
            AddBinding(new TriggerBinding(Group, "TogglePanel", TogglePanel));
            AddBinding(new TriggerBinding<string>(Group, "ExecuteQuickAction", ExecuteQuickAction));
            AddBinding(new TriggerBinding<int>(Group, "SelectPreset", SelectPreset));
            AddBinding(new TriggerBinding<string, bool>(Group, "ToggleSetting", ToggleSetting));
            AddBinding(new TriggerBinding<int>(Group, "SetWaterMode", SetWaterMode));

            if (s != null)
            {
                s.OnSettingChanged += OnSettingChangedHandler;
            }

            modLog.Info("[UltraPerformanceMod] InGameUISystem initialized successfully.");
        }

        protected override void OnDestroy()
        {
            if (Mod.Settings != null)
            {
                Mod.Settings.OnSettingChanged -= OnSettingChangedHandler;
            }

            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void OnSettingChangedHandler()
        {
            UpdateAllBindings();
        }

        public void TogglePanel()
        {
            m_IsPanelOpen = !m_IsPanelOpen;
            m_IsPanelOpenBinding.Update(m_IsPanelOpen);
            if (m_IsPanelOpen)
            {
                UpdateAllBindings();
            }
        }

        public void SetStatusMessage(string message)
        {
            m_StatusMessage = message;
            m_StatusMessageBinding.Update(message);
        }

        public void SelectPreset(int presetIndex)
        {
            var s = Mod.Settings;
            if (s == null) return;

            var preset = (PerformancePreset)presetIndex;
            s.Preset = preset;
            s.ApplyPreset(preset);
            s.ApplyAndSave();

            UpdateAllBindings();
            SetStatusMessage($"[OK] Predefini\u00E7\u00E3o '{preset}' aplicada com sucesso!");
        }

        public void SetWaterMode(int modeIndex)
        {
            var s = Mod.Settings;
            if (s == null) return;

            s.WaterSimulation = (WaterSimulationMode)modeIndex;
            s.ApplyAndSave();

            UpdateAllBindings();
            string modeName = s.WaterSimulation switch
            {
                WaterSimulationMode.Parada => "Parada (Congelada)",
                WaterSimulationMode.Media => "M\u00E9dia (Otimizada 50%)",
                _ => "Normal (Padr\u00E3o Vanilla)"
            };
            SetStatusMessage($"[OK] Velocidade da \u00E1gua ajustada: {modeName}");
        }

        public void ToggleSetting(string key, bool value)
        {
            var s = Mod.Settings;
            if (s == null) return;

            switch (key)
            {
                case "InvisibleNPCs": s.InvisibleNPCs = value; break;
                case "InvisibleVehicles": s.InvisibleVehicles = value; break;
                case "InvisibleBicycles": s.InvisibleBicycles = value; break;
                case "InvisibleMotorcycles": s.InvisibleMotorcycles = value; break;
                case "InvisibleTrees": s.InvisibleTrees = value; break;
                case "InvisibleWildTrees": s.InvisibleWildTrees = value; break;
                case "InvisibleBuildingTrees": s.InvisibleBuildingTrees = value; break;
                case "InvisibleRoadTrees": s.InvisibleRoadTrees = value; break;
                case "DisableTreeWind": s.DisableTreeWind = value; break;
                case "AutoClearDeadlocks": s.AutoClearDeadlocks = value; break;
                case "HideNotificationIcons": s.HideNotificationIcons = value; break;
                case "AutoGarbage": s.AutoGarbage = value; break;
                case "AutoDeathcare": s.AutoDeathcare = value; break;
                case "AutoRoadMaintenance": s.AutoRoadMaintenance = value; break;
                case "AutoParkMaintenance": s.AutoParkMaintenance = value; break;
                case "AutoPolice": s.AutoPolice = value; break;
                case "AutoAmbulance": s.AutoAmbulance = value; break;
                case "AutoPostService": s.AutoPostService = value; break;
                case "InstantConstruction": s.InstantConstruction = value; break;
                case "DisableOutsideTaxis": s.DisableOutsideTaxis = value; break;
                case "DisableDummyTraffic": s.DisableDummyTraffic = value; break;
                case "OptimizeParking": s.OptimizeParking = value; break;
            }

            s.ApplyAndSave();
            UpdateAllBindings();
            SetStatusMessage($"[OK] Ajuste atualizado: {key} = {(value ? "Ativado" : "Desativado")}");
        }

        public void ExecuteQuickAction(string actionKey)
        {
            try
            {
                switch (actionKey)
                {
                    case "ClearDeadlocks":
                        int deadlocks = TrafficDeadlockBreakerSystem.Instance?.ClearAllTrafficDeadlocksNow() ?? 0;
                        SetStatusMessage($"[OK] Tr\u00E2nsito verificado: {deadlocks} ve\u00EDculos/acidentes desobstru\u00EDdos!");
                        break;

                    case "ClearAccidents":
                        int accidents = TrafficDeadlockBreakerSystem.Instance?.ClearAllAccidentsNow() ?? 0;
                        SetStatusMessage($"[OK] {accidents} acidentes de tr\u00E2nsito removidos da pista!");
                        break;

                    case "CleanGarbage":
                        int garbage = AutoGarbageSystem.Instance?.CleanAllGarbageNow() ?? 0;
                        SetStatusMessage($"[OK] Lixo coletado instantaneamente de {garbage} im\u00F3veis!");
                        break;

                    case "CollectDeceased":
                        int deceased = AutoDeathcareSystem.Instance?.CollectAllDeceasedNow() ?? 0;
                        SetStatusMessage($"[OK] Funer\u00E1ria autom\u00E1tica atendeu {deceased} corpos na cidade!");
                        break;

                    case "MaintainRoads":
                        AutoMaintenanceSystem.Instance?.PerformRoadMaintenanceNow();
                        SetStatusMessage("[OK] Asfalto de todas as vias e rodovias reparado 100%!");
                        break;

                    case "MaintainParks":
                        AutoMaintenanceSystem.Instance?.PerformParkMaintenanceNow();
                        SetStatusMessage("[OK] Todos os parques e pra\u00E7as revitalizados!");
                        break;

                    case "HealCity":
                        AutoEmergencyServicesSystem.Instance?.HealAndProtectCityNow();
                        SetStatusMessage("[OK] Sa\u00FAde e seguran\u00E7a: cidad\u00E3os socorridos e ocorr\u00EAncias resolvidas!");
                        break;

                    case "DeliverMail":
                        AutoPostServiceSystem.Instance?.DeliverAllMailNow();
                        SetStatusMessage("[OK] Correios: correspond\u00EAncias entregues em todos os im\u00F3veis!");
                        break;

                    case "PurgeTrailers":
                        int trailers = TrailerCleanupSystem.Instance?.PurgeAllTrailerAndVanPropsNow() ?? 0;
                        SetStatusMessage($"[OK] {trailers} trailers, cont\u00EAineres e vans de entrega removidos!");
                        break;

                    case "PurgeAnimals":
                        AnimalManagerSystem.Instance?.PurgeAllAnimalsNow();
                        SetStatusMessage("[OK] Animais e c\u00E3es soltos limpos do mapa com seguran\u00E7a!");
                        break;

                    case "CompleteConstruction":
                        InstantConstructionSystem.Instance?.CompleteAllConstructionNow();
                        SetStatusMessage("[OK] Todas as obras em andamento conclu\u00EDdas instantaneamente!");
                        break;

                    case "PurgeOutsideTaxis":
                        int taxis = OutsideTaxiLimiterSystem.Instance?.PurgeOutsideTaxisNow() ?? 0;
                        SetStatusMessage($"[OK] {taxis} t\u00E1xis de fora da cidade removidos das conex\u00F5es!");
                        break;

                    case "PurgeDummyTraffic":
                        int dummy = DummyTrafficRemoverSystem.Instance?.PurgeDummyTrafficNow() ?? 0;
                        SetStatusMessage($"[OK] {dummy} ve\u00EDculos de passagem fict\u00EDcios removidos!");
                        break;

                    case "PurgeParkedCars":
                        int parked = ParkingOptimizerSystem.Instance?.PurgeInvalidParkedCarsNow() ?? 0;
                        SetStatusMessage($"[OK] {parked} carros abandonados em estacionamentos limpos!");
                        break;

                    default:
                        SetStatusMessage($"A\u00E7\u00E3o desconhecida: {actionKey}");
                        break;
                }
            }
            catch (Exception ex)
            {
                modLog.Error($"[UltraPerformanceMod] Error executing quick action '{actionKey}': {ex.Message}");
                SetStatusMessage($"[Aviso] Erro ao executar a\u00E7\u00E3o: {ex.Message}");
            }
        }

        public void UpdateAllBindings()
        {
            var s = Mod.Settings;
            if (s == null) return;

            m_PresetBinding.Update((int)s.Preset);
            m_InvisibleNPCsBinding.Update(s.InvisibleNPCs);
            m_InvisibleVehiclesBinding.Update(s.InvisibleVehicles);
            m_InvisibleBicyclesBinding.Update(s.InvisibleBicycles);
            m_InvisibleMotorcyclesBinding.Update(s.InvisibleMotorcycles);
            m_InvisibleTreesBinding.Update(s.InvisibleTrees);
            m_InvisibleWildTreesBinding.Update(s.InvisibleWildTrees);
            m_InvisibleBuildingTreesBinding.Update(s.InvisibleBuildingTrees);
            m_InvisibleRoadTreesBinding.Update(s.InvisibleRoadTrees);
            m_DisableTreeWindBinding.Update(s.DisableTreeWind);
            m_WaterSimulationBinding.Update((int)s.WaterSimulation);
            m_AutoClearDeadlocksBinding.Update(s.AutoClearDeadlocks);
            m_HideNotificationIconsBinding.Update(s.HideNotificationIcons);

            m_AutoGarbageBinding.Update(s.AutoGarbage);
            m_AutoDeathcareBinding.Update(s.AutoDeathcare);
            m_AutoRoadMaintenanceBinding.Update(s.AutoRoadMaintenance);
            m_AutoParkMaintenanceBinding.Update(s.AutoParkMaintenance);
            m_AutoPoliceBinding.Update(s.AutoPolice);
            m_AutoAmbulanceBinding.Update(s.AutoAmbulance);
            m_AutoPostServiceBinding.Update(s.AutoPostService);
            m_InstantConstructionBinding.Update(s.InstantConstruction);
            m_DisableOutsideTaxisBinding.Update(s.DisableOutsideTaxis);
            m_DisableDummyTrafficBinding.Update(s.DisableDummyTraffic);
            m_OptimizeParkingBinding.Update(s.OptimizeParking);
        }
    }
}
