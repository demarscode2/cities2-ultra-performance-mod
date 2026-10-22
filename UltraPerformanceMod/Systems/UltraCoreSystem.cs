using Colossal.Logging;
using Game;
using UltraPerformanceMod.Core;
using Unity.Entities;
using UnityEngine;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// UltraCore ECS Driver System.
    /// Runs in PreCulling to sample frame metrics, update AutoTuner load assessments,
    /// time-slice budgeted mod tasks, and perform cache garbage-free maintenance.
    /// </summary>
    public partial class UltraCoreSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static UltraCoreSystem? Instance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            UltraCoreEngine.Instance.Initialize();
            log.Info("[UltraPerformanceMod] UltraCoreSystem active in ECS pipeline.");
        }

        protected override void OnDestroy()
        {
            UltraCoreEngine.Instance.Shutdown();
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            float dt = UnityEngine.Time.deltaTime;
            UltraCoreEngine.Instance.Update(dt);
        }
    }
}
