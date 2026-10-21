using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
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
        private EntityQuery m_PostVanRequestQuery = default!;
        private EntityQuery m_PostVanQuery = default!;
        private EntityQuery m_MailSenderQuery = default!;

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

            m_PostVanRequestQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PostVanRequest>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_PostVanQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PostVan>(), ComponentType.ReadOnly<Vehicle>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_MailSenderQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<MailSender>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] AutoPostServiceSystem initialized.");
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
            // Run every 128 simulation frames (~2 seconds)
            if (m_FrameCounter % 128 != 0) return;

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

            // 1. Process MailProducers (buildings producing/receiving mail)
            if (!m_MailProducerQuery.IsEmptyIgnoreFilter)
            {
                using var entities = m_MailProducerQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var producer = EntityManager.GetComponentData<MailProducer>(entity);

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
                        EntityManager.SetComponentData(entity, producer);
                        buildingsProcessed++;
                    }
                }
            }

            // 2. Process Public MailBoxes
            if (!m_MailBoxQuery.IsEmptyIgnoreFilter)
            {
                using var mailboxEntities = m_MailBoxQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < mailboxEntities.Length; i++)
                {
                    var entity = mailboxEntities[i];
                    var box = EntityManager.GetComponentData<MailBox>(entity);

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
                        EntityManager.SetComponentData(entity, box);
                        mailboxesProcessed++;
                    }
                }
            }

            // 3. Clear MailSender citizens
            if (!m_MailSenderQuery.IsEmptyIgnoreFilter)
            {
                using var senderEntities = m_MailSenderQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < senderEntities.Length; i++)
                {
                    var entity = senderEntities[i];
                    var sender = EntityManager.GetComponentData<MailSender>(entity);
                    if (sender.m_Amount > 0)
                    {
                        sender.m_Amount = 0;
                        EntityManager.SetComponentData(entity, sender);
                    }
                }
            }

            // 4. Delete orphan PostVanRequests
            if (!m_PostVanRequestQuery.IsEmptyIgnoreFilter)
            {
                using var orphanedRequests = m_PostVanRequestQuery.ToEntityArray(Allocator.TempJob);
                if (orphanedRequests.Length > 0)
                {
                    EntityManager.AddComponent<Deleted>(orphanedRequests);
                    requestsDeleted += orphanedRequests.Length;
                }
            }

            // 5. Purge roaming PostVans if automatic service is active
            if (!m_PostVanQuery.IsEmptyIgnoreFilter)
            {
                using var activeVans = m_PostVanQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < activeVans.Length; i++)
                {
                    var vanEntity = activeVans[i];
                    if (EntityManager.Exists(vanEntity) && !EntityManager.HasComponent<Deleted>(vanEntity))
                    {
                        UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, vanEntity);
                        postVansPurged++;
                    }
                }
            }

            if (buildingsProcessed > 0 || mailboxesProcessed > 0 || postVansPurged > 0)
            {
                log.Info($"[UltraPerformanceMod] AutoPostService: Cleared {buildingsProcessed} building mail, {mailboxesProcessed} mailboxes, deleted {requestsDeleted} requests, purged {postVansPurged} roaming post vans.");
            }
        }
    }
}
