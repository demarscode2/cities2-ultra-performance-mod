using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using UltraPerformanceMod.Core;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class AutoPostServiceSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static AutoPostServiceSystem? Instance { get; private set; }

        private EntityQuery m_MailProducerQuery = default!;
        private EntityQuery m_MailBoxQuery = default!;
        private EntityQuery m_MailSenderQuery = default!;
        private EntityQuery m_PostVanRequestQuery = default!;
        private EntityQuery m_PostVanQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_MailProducerQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<MailProducer>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_MailBoxQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<MailBox>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_MailSenderQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<MailSender>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_PostVanRequestQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PostVanRequest>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_PostVanQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PostVan>(),
                    ComponentType.ReadOnly<Vehicle>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] High-Performance AutoPostServiceSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || !Mod.Settings.AutoPostService) return;

            m_FrameCounter++;
            float intervalScale = UltraCoreEngine.Instance.AutoTuner.TaskIntervalScale;
            int scaledInterval = (int)(128 * intervalScale);
            if (m_FrameCounter % System.Math.Max(32, scaledInterval) != 0) return;

            DeliverAllMail();
        }

        public void DeliverAllMailNow()
        {
            DeliverAllMail();
        }

        private void DeliverAllMail()
        {
            int buildingsProcessed = 0;
            int mailboxesProcessed = 0;
            int requestsDeleted = 0;
            int postVansPurged = 0;

            using (UltraCoreEngine.Instance.Diagnostics.Measure("AutoPostService"))
            {
                // 1. Process MailProducers (buildings producing/receiving mail)
                if (!m_MailProducerQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_MailProducerQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var producerType = GetComponentTypeHandle<MailProducer>(false);

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var producers = chunk.GetNativeArray(ref producerType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            var producer = producers[i];
                            bool modified = false;

                            if (producer.m_SendingMail > 0 || producer.m_ReceivingMail > 0)
                            {
                                producer.m_SendingMail = 0;
                                producer.m_ReceivingMail = 0;
                                modified = true;
                            }

                            if (producer.m_MailRequest != Entity.Null)
                            {
                                if (EntityManager.Exists(producer.m_MailRequest) && !EntityManager.HasComponent<Deleted>(producer.m_MailRequest))
                                {
                                    EntityManager.AddComponent<Deleted>(producer.m_MailRequest);
                                    requestsDeleted++;
                                }
                                producer.m_MailRequest = Entity.Null;
                                modified = true;
                            }

                            if (modified)
                            {
                                producers[i] = producer;
                                buildingsProcessed++;
                            }
                        }
                    }
                }

                // 2. Process Public MailBoxes
                if (!m_MailBoxQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_MailBoxQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var boxType = GetComponentTypeHandle<MailBox>(false);

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var boxes = chunk.GetNativeArray(ref boxType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            var box = boxes[i];
                            bool modified = false;

                            if (box.m_MailAmount > 0)
                            {
                                box.m_MailAmount = 0;
                                modified = true;
                            }

                            if (box.m_CollectRequest != Entity.Null)
                            {
                                if (EntityManager.Exists(box.m_CollectRequest) && !EntityManager.HasComponent<Deleted>(box.m_CollectRequest))
                                {
                                    EntityManager.AddComponent<Deleted>(box.m_CollectRequest);
                                    requestsDeleted++;
                                }
                                box.m_CollectRequest = Entity.Null;
                                modified = true;
                            }

                            if (modified)
                            {
                                boxes[i] = box;
                                mailboxesProcessed++;
                            }
                        }
                    }
                }

                // 3. Clear MailSender citizens
                if (!m_MailSenderQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_MailSenderQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var senderType = GetComponentTypeHandle<MailSender>(false);

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var senders = chunk.GetNativeArray(ref senderType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            var sender = senders[i];
                            if (sender.m_Amount > 0)
                            {
                                sender.m_Amount = 0;
                                senders[i] = sender;
                            }
                        }
                    }
                }

                // 4. Delete orphan PostVanRequests (Zero-alloc direct batch)
                if (!m_PostVanRequestQuery.IsEmptyIgnoreFilter)
                {
                    requestsDeleted += m_PostVanRequestQuery.CalculateEntityCount();
                    EntityManager.AddComponent<Deleted>(m_PostVanRequestQuery);
                }

                // 5. Purge roaming PostVans if automatic service is active
                if (!m_PostVanQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_PostVanQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var entityType = GetEntityTypeHandle();

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var vans = chunk.GetNativeArray(entityType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, vans[i]);
                            postVansPurged++;
                        }
                    }
                }
            }

            if (buildingsProcessed > 0 || mailboxesProcessed > 0 || postVansPurged > 0)
            {
                log.Info($"[UltraPerformanceMod] Post Service: Delivered mail to {buildingsProcessed} properties, emptied {mailboxesProcessed} mailboxes, purged {postVansPurged} vans (Chunk-based).");
            }
        }
    }
}
