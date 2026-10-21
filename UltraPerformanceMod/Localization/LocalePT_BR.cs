using System.Collections.Generic;
using Colossal;
using UltraPerformanceMod.Systems;

namespace UltraPerformanceMod.Localization
{
    public class LocalePT_BR : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocalePT_BR(Setting setting)
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
                // Título do Mod e Seções
                { m_Setting.GetSettingsLocaleID(), "Ultra Desempenho & Otimização" },

                // Abas
                { m_Setting.GetOptionTabLocaleID(Setting.TabPresets), "Predefinições" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabVisibility), "Visibilidade" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabServices), "Serviços" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabTraffic), "Trânsito" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabGraphics), "Gráficos & Ambiente" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabActions), "Ações Rápidas" },
                { m_Setting.GetOptionTabLocaleID(Setting.TabAbout), "Sobre" },

                // Grupos
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupPresets), "Perfis de Otimização" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupVisibility), "Renderização de Malhas e Entidades" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupServices), "Automação de Serviços Urbanos" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupTraffic), "Otimizações de Tráfego e Pathfinding" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupGraphics), "Água, Árvores e Efeitos Visuais" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupActions), "Limpeza Manual Instantânea" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GroupAbout), "Informações do Mod" },

                // Presets
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Preset)), "Predefinição de Desempenho" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Preset)), "Escolha um perfil de desempenho pré-configurado: Básico (correções essenciais de simulação), Médio (equilibrado e recomendado para cidades grandes), ou Máximo (FPS extremo para megalópoles de 500k-1M de cidadãos)." },
                { m_Setting.GetEnumValueLocaleID(PerformancePreset.Custom), "Personalizado" },
                { m_Setting.GetEnumValueLocaleID(PerformancePreset.Basic), "Básico (Simulação Otimizada)" },
                { m_Setting.GetEnumValueLocaleID(PerformancePreset.Medium), "Médio (Equilibrado Recomendado)" },
                { m_Setting.GetEnumValueLocaleID(PerformancePreset.Maximum), "Máximo (FPS Extremo / Megalópole)" },

                // Visibilidade
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleNPCs)), "NPCs / Cidadãos Invisíveis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleNPCs)), "Oculta a renderização dos pedestres e desativa matrizes esqueléticas de animação. A simulação da cidade continua ativa normalmente." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleVehicles)), "Todos os Veículos Invisíveis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleVehicles)), "Oculta os modelos 3D dos carros, caminhões e reboques, proporcionando um salto brutal de FPS em áreas de alto tráfego." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleBicycles)), "Bicicletas Invisíveis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleBicycles)), "Oculta apenas ciclistas e bicicletas nas ciclovias e vias públicas sem afetar os automóveis." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleMotorcycles)), "Motocicletas Invisíveis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleMotorcycles)), "Oculta motos e motonetas individualmente dos demais carros de 4 rodas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AnimalHandling)), "Gerenciamento de Animais (Cães/Selvagens)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AnimalHandling)), "Escolha o tratamento dos cães de estimação e animais selvagens: Padrão (visíveis), Invisíveis (simulação ativa, sem renderização gráfica), ou Remover Completamente (desativa do jogo, economizando CPU e memória)." },
                { m_Setting.GetEnumValueLocaleID(AnimalHandlingMode.KeepVanilla), "Padrão (Visíveis)" },
                { m_Setting.GetEnumValueLocaleID(AnimalHandlingMode.Invisible), "Invisíveis (Sem Custo de Render)" },
                { m_Setting.GetEnumValueLocaleID(AnimalHandlingMode.RemoveCompletely), "Remover Completamente (Máximo Desempenho)" },
                
                // Árvores Granulares
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleWildTrees)), "Árvores na Natureza (Fora de Construções)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleWildTrees)), "Oculta árvores livres, matas e florestas espalhadas pelo relevo do mapa fora de lotes urbanos, liberando milhares de draw calls." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleBuildingTrees)), "Árvores em Construções e Parques" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleBuildingTrees)), "Oculta árvores pertencentes a lotes de edifícios, praças públicas e áreas de lazer." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleRoadTrees)), "Árvores em Vias e Calçadas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleRoadTrees)), "Oculta árvores decorativas de canteiros centrais, calçadas de avenidas e rodovias." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvisibleTrees)), "Todas as Árvores Invisíveis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvisibleTrees)), "Oculta todas as árvores do mapa de uma só vez para máximo ganho de FPS." },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HideNotificationIcons)), "Ocultar Ícones de Erro / Alerta" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HideNotificationIcons)), "Oculta todos os ícones 3D flutuantes (falta de trabalhadores, clientes, aluguel alto, etc.) limpando o horizonte da cidade e poupando renderização." },

                // Serviços
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoGarbage)), "Coleta Automática de Lixo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoGarbage)), "Zera o acúmulo de lixo nos edifícios e recolhe caminhões de lixo das ruas, impedindo engarrafamentos de frotas de limpeza." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoDeathcare)), "Funerária Automática (Rabecão)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoDeathcare)), "Processa e recolhe cidadãos falecidos automaticamente sem depender de carros funerários no trânsito." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoRoadMaintenance)), "Manutenção Automática de Rodovias" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoRoadMaintenance)), "Mantém todas as pistas em 100% de qualidade e remove caminhões lentos de manutenção de vias das pistas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoParkMaintenance)), "Manutenção Automática de Parques" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoParkMaintenance)), "Mantém parques e praças sempre conservados sem necessidade de frotas de manutenção circulando." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoPolice)), "Polícia Automática (Segurança Instantânea)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoPolice)), "Zera a criminalidade e previne viaturas de ficarem presas em engarrafamentos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoAmbulance)), "Saúde / Ambulância Automática" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoAmbulance)), "Cura cidadãos doentes e feridos instantaneamente, dispensando sirenes e frotas de emergência bloqueadas no trânsito." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoPostService)), "Correios Automático (Coleta e Entrega)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoPostService)), "Processa toda correspondência de residências e caixas de correio sem furgões postais nas ruas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InstantConstruction)), "Crescimento Instantâneo de Prédios" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InstantConstruction)), "Conclui construções e upgrades de nível instantaneamente, sem guindastes ou andaimes demorados." },

                // Trânsito
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableOutsideTaxis)), "Bloquear Táxis Externos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableOutsideTaxis)), "Impede que milhares de táxis vazios venham de conexões de fora do mapa para pegar passageiros, aliviando o tráfego intermunicipal." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableDummyTraffic)), "Remover Tráfego Falso / Passante" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableDummyTraffic)), "Elimina veículos artificiais que só atravessam o mapa sem residir ou consumir na cidade." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OptimizeParking)), "Otimizar Estacionamento" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OptimizeParking)), "Remove carros estacionados ilegalmente em calçadas ou locais inválidos que possam causar bloqueios." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoClearDeadlocks)), "Desobstrução Automática de Trânsito Preso" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoClearDeadlocks)), "Detecta veículos totalmente travados em engarrafamentos ou acidentes e os despacha, acabando com travamentos infinitos e surtos de recálculo de pathfinding." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeadlockTimeoutSeconds)), "Tempo Limite de Espera de Veículo Preso (segundos)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DeadlockTimeoutSeconds)), "Define quantos segundos um veículo pode ficar parado no mesmo segmento antes de ser considerado congelado e desobstruído." },

                // Gráficos & Ambiente
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableHeavyEffects)), "Reduzir Efeitos Visuais Pesados" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableHeavyEffects)), "Desativa fumaça pesada, poluição volumétrica e partículas complexas que derrubam o FPS da placa de vídeo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableTreeWind)), "Desativar Balanço de Vento nas Árvores" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableTreeWind)), "Zera a deformação de vértices por vento na folhagem, reduzindo trabalho de shaders de geometria." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DisableTreeGrowth)), "Desativar Crescimento / Envelhecimento de Árvores" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DisableTreeGrowth)), "Pausa o sistema de envelhecimento e decaimento das árvores decorativas, poupando processamento diário no ECS sem afetar as indústrias de silvicultura." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.WaterSimulation)), "Velocidade da Simulação de Água" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.WaterSimulation)), "Ajusta com segurança a velocidade do fluxo de água sem risco de oscilações ou tufões: Normal (velocidade padrão do jogo), Média (50% de velocidade com transição suave), ou Parada (simulação pausada com água visível e zero uso de CPU)." },
                { m_Setting.GetEnumValueLocaleID(WaterSimulationMode.Normal), "Normal (Padrão Vanilla)" },
                { m_Setting.GetEnumValueLocaleID(WaterSimulationMode.Media), "Média (50% Suave / Estável)" },
                { m_Setting.GetEnumValueLocaleID(WaterSimulationMode.Parada), "Parada (Pausada / Leve)" },

                // Botões de Ações Rápidas
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanGarbageButton)), "Esvaziar Todo o Lixo da Cidade" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanGarbageButton)), "Zera instantaneamente o lixo de todos os imóveis e remove caminhões da pista." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CollectDeceasedButton)), "Recolher Todos os Corpos Falecidos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CollectDeceasedButton)), "Remove imediatamente todas as pendências funerárias e corpos da cidade." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MaintainRoadsAndParksButton)), "Reparar Todas as Rodovias e Parques" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MaintainRoadsAndParksButton)), "Restaura o asfalto de 100% das vias e limpa todas as áreas verdes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HealAndProtectCityButton)), "Curar Cidadãos e Zerar Crime Agora" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HealAndProtectCityButton)), "Cura todos os doentes/feridos e elimina crimes em andamento." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeliverAllMailButton)), "Entregar Toda Correspondência Agora" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DeliverAllMailButton)), "Zera correspondências pendentes e limpa caixas de correio da cidade." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearTrafficDeadlocksButton)), "Desobstruir Trânsito e Remover Bloqueios" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearTrafficDeadlocksButton)), "Despacha carros que estão travados no trânsito há mais de 15 segundos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearAccidentsButton)), "Limpar Todos os Acidentes das Pistas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearAccidentsButton)), "Remove locais de acidentes e libera faixas interditadas imediatamente." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeTrailersButton)), "Remover Reboques / Trailers Órfãos (Io-Iô)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeTrailersButton)), "Exclui carretinhas, trailers e contêineres soltos que ficaram travados ou pulando nas pistas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeAllAnimalsButton)), "Expurgar Todos os Animais" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeAllAnimalsButton)), "Remove do mapa todos os animais domésticos e selvagens ativos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InstantCompleteAllBuildingsButton)), "Concluir Construções em Andamento" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InstantCompleteAllBuildingsButton)), "Finaliza de imediato qualquer prédio em construção ou elevação de nível." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeOutsideTaxisButton)), "Remover Táxis Externos Atuais" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeOutsideTaxisButton)), "Despacha todos os táxis de fora da cidade que estão nas ruas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeDummyTrafficButton)), "Remover Tráfego Passante Atual" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeDummyTrafficButton)), "Remove todos os carros artificiais de travessia do mapa." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PurgeParkedCarsButton)), "Limpar Carros Estacionados Inválidos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PurgeParkedCarsButton)), "Remove veículos abandonados ou mal estacionados." },

                // Sobre
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersionInfo)), "Versão" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersionInfo)), "Versão do Ultra Performance Mod compilada para Cities: Skylines II." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModStatusInfo)), "Estado Atual" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModStatusInfo)), "Informa o preset ativo e status de funcionamento dos módulos de simulação." },
            };
        }
    }
}
