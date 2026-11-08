# ⚡ Ultra Performance & Simulation Booster (Beta v1.5.0)
### *O Mod Definitivo de Desempenho, Renderização e Simulação para Cities: Skylines II*

![Ultra Performance Booster](Properties/Thumbnail.png)

O **Ultra Performance & Simulation Booster** foi desenvolvido sob medida para resolver os gargalos mais críticos do Cities: Skylines II: **queda de FPS, sobrecarga na renderização gráfica, lentidão extrema da simulação na CPU e congelamento do tráfego por cálculo excessivo de rotas (pathfinding)**.

Projetado sobre a arquitetura nativa ECS / DOTS e Unity HDRP, o mod oferece um ecossistema completo de ferramentas para recuperar a fluidez em cidades de qualquer porte, inclusive megalópoles com mais de **700.000 a 1.000.000 de habitantes**.

> 🔗 **Repositório Oficial no GitHub:** [https://github.com/demarscode2/cities2-ultra-performance-mod](https://github.com/demarscode2/cities2-ultra-performance-mod)  
> 🎯 **Versão do Jogo Suportada:** `1.6.*`

---

## ⚠️ AVISO IMPORTANTE: Testado no Beta Aberto de Desempenho da Steam

> [!IMPORTANT]
> **Compatibilidade de Versão:**
> Esta versão (**v1.5.0**) foi desenvolvida, ajustada e validada no novo **Beta Aberto de Desempenho do Cities: Skylines II (`performance-test`)** disponibilizado pela Colossal Order na Steam.
> 
> Como essa versão Beta inclui reformulações profundas nos modelos de LOD do jogo, texturização virtual (Virtual Texturing) e na renderização geral, **é possível que ocorram pequenas falhas, inconsistências visuais ou comportamentos inesperados na versão estável atual do jogo**.
> 
> Implementamos travas dinâmicas de compatibilidade dentro do UltraCore para proteger savegames em ambas as versões. Recomendamos testar as opções gradualmente no seu jogo!

---

## ⚡ NOVIDADE v1.5.0: Alívio da Fila de Pathfinding (Pathfind Queue Unchoker)

Resolve o infame problema de cidades grandes (300k a 700k+ habitantes) onde os veículos e pedestres passam a se mover em **câmera lenta** (como se estivessem a 0.2x mesmo na velocidade 3x):

* **Prioridade Total da CPU para o Relógio:** Trava `SimulationSystem.performancePreference = SimulationSpeed`, impedindo que oscilações de FPS na placa de vídeo roubem os ticks de tempo do jogo.
* **Destravamento Automático de Gap de Quadros:** No jogo vanilla, quando uma requisição de caminho se perde, a fórmula nativa estrangula a velocidade do tempo global da cidade. O UltraCore detecta essa retenção em tempo real e destrava a fila.
* **Purga de Entidades Órfãs:** Remove automaticamente requisições pendentes de cidadãos ou veículos que já foram deletados ou despachados, impedindo que travem o relógio global.
* **Priorização do Trânsito Urbano:** Em picos severos de fila, poda temporariamente requisições de baixa prioridade de animais selvagens para reservar threads de trabalho para o tráfego da cidade.
* **Botão de 1 Clique:** Botão direto no menu flutuante e configurações para desafogar imediatamente toda a fila e devolver a fluidez total da simulação.

---

## 🎯 NOVIDADE v1.5.0: Otimizador Radix de Cobertura de Serviços (Anti-Stutter)

Elimina os congelamentos rítmicos de CPU (quedas de 70 a 100ms a cada 4.4 segundos) em cidades com centenas de parques e serviços públicos:

* **Fila de Prioridade Monótona Byte-Radix $O(1)$:** Substitui o deslocamento linear ineficiente $O(E \times B)$ do método nativo `ApplyCoverageJob` por um algoritmo Radix de alto desempenho.
* **100% de Paridade Matemática:** Calcula a cobertura de parques, postos de saúde, polícia, bombeiros e correios com exatidão matemática idêntica ao jogo original, sem discrepâncias.
* **Compilado com Unity Burst & Zero-Alloc:** Código nativo puro com consumo nulo de memória gerenciada.
* **Patch Seguro via Harmony:** Valida o tamanho das estruturas em memória dinamicamente antes de anexar o patch, com retorno automático ao código nativo em caso de incompatibilidade futura.

---

## 👁️ Ultra Visibility Engine (UVE) [Fase Experimental]

O **Ultra Visibility Engine (UVE)** é o subsistema interno focado em reduzir drasticamente o trabalho gráfico e de vértices desnecessário na GPU, preservando 100% da simulação da sua cidade:

* **Controle Multinível de LOD de Distância:**
  * 🟢 **Vanilla (100%):** Distâncias de renderização e detalhe originais do jogo.
  * 🟡 **Equilibrado (70%):** Transição suave de malhas à distância; oculta pequenos props microscópicos e otimiza a densidade sem impacto perceptível.
  * 🟠 **Agressivo (45%):** Transição antecipada de árvores e edifícios distantes para variantes de baixo consumo de polígonos; corte de props decorativos além de 150m.
  * 🔴 **Extremo (25%):** Foco absoluto em taxa de quadros (FPS); malhas de alta definição reservadas apenas para as imediações da câmera.
* **Frustum Far-Clip Dinâmico:** Ajusta suavemente o plano de corte de horizonte da câmera com base na altitude e no ângulo vertical (*pitch*). A nível de rua, o corte é reduzido de 10.000m para 2.400m–3.500m, economizando até 70% de geometria desnecessária oculta pela névoa volumétrica.
* **Orçamento Estrito de Memória GPU (HLOD):** Força o motor do jogo (`strictMemoryBudget = true`) a consolidar chamadas de desenho (*draw calls*) em sub-lotes nativos já alocados, evitando o inchaço e a fragmentação da VRAM.
* **Teto de Iluminação Noturna (Clustered Lights):** Limita o teto de luzes simultâneas ativas em períodos noturnos (4.096 no Agressivo e 2.048 no Extremo), eliminando engasgos pesados nos shaders de iluminação diferida.
* **Garantia de Simulação 100% Intacta:** Todo o processamento do UVE atua estritamente na pipeline de renderização. **Nenhuma entidade é deletada ou pausada.** Cidadãos, carros, rotas de tráfego, comércio, indústrias e serviços continuam sendo simulados em tempo integral.

---

## 🔬 Aviso Sobre Fase Experimental & Limites do Motor do Jogo

> [!WARNING]
> **Transparência e Expectativas Reais de Desempenho:**
> * Recursos como o **Pathfind Queue Unchoker**, o **Ultra Visibility Engine (UVE)** e o ajuste profundo de threads de simulação estão em **Fase Experimental**.
> * **Não é garantido que o mod aumentará o desempenho em 100% em todas as configurações de computador ou cidades.** O próprio motor do Cities: Skylines II possui limitações arquiteturais internas e gargalos estruturais de CPU que nenhum mod consegue contornar inteiramente.
> * Os ganhos variam substancialmente dependendo do modelo do seu processador, placa de vídeo, velocidade das memórias RAM, tamanho da população e densidade de construções.
> * **Liberdade de Configuração:** O jogador tem total liberdade para navegar no painel e testar os diferentes perfis (**Básico**, **Equilibrado**, **Agressivo**, **Extremo**) e interruptores individuais para descobrir qual combinação entrega a melhor experiência visual e de fluidez na sua máquina.

---

## 🧠 UltraCore: Otimizações de CPU e Simulação

* **Consultas ECS Zero-Alloc & Cache Inteligente:** Reduz o trabalho do coletor de lixo (*Garbage Collector*) da Unity, diminuindo micro-travamentos (*stuttering*).
* **Desobstrução do Loop de Sem-Teto (Homeless Loop Breaker):** Resolve a falha nativa do jogo em que milhares de moradores de rua ficam acampados em parques calculando rotas infinitamente e sobrecarregando a CPU. Permite evacuação automática periódica ou acionamento imediato por botão.
* **Desobstrução de Pedestres Travados:** Identifica pedestres presos em calçadas, faixas de pedestres ou escadarias de metrô por falhas de colisão de pista.
* **Trânsito Inteligente (Deadlock Breaker):** Diferente de soluções que apagam carros em semáforos, nosso algoritmo protege veículos em operações normais (sinal vermelho, faixas, conversões) e só atua em travamentos circulares reais ou carros fora da pista.

---

## 🎨 Otimizações Gráficas e Estabilidade

* **Otimizador de Sombras HDRP:** Limita sombras exageradas além de 450m e filtra micro-props das passagens de sombra, liberando até 40% da carga de rasterização da GPU.
* **Remoção do Contorno Azul ao Passar o Mouse:** Remove a borda ciano/azul ao passar o cursor sobre edifícios, cidadãos e veículos, economizando uma passagem completa de geometria e pós-processamento por frame (`OutlinesWorldUIPass`).
* **Vegetação e Árvores:** Desativa o balanço de vento nos vértices das folhas e pausa o envelhecimento visual das árvores, economizando processamento sem afetar a silvicultura.
* **Água 100% Estável (Vanilla):** Hidrodinâmica nativa mantida lisa e estável, sem saltos de embarcações.

---

## 🖥️ Painel In-Game Exclusivo (HUD Topo Esquerdo ⚡)

Clique no ícone de raio (**⚡**) na barra superior esquerda para acessar instantaneamente dentro da cidade:
* **17 Ações Rápidas em 1 Clique:**
  * Desafogar Fila de Pathfinding, Desobstruir Trânsito, Evacuar Sem-Teto, Limpar Pedestres Presos, Limpar Reboques/Props Órfãos, Limpar Acidentes, Limpar Lixo, Coleta Funerária, Manutenção de Asfalto, Manutenção de Parques, Socorrer Doentes, Entregar Cartas, Limpar Animais, Concluir Obras, Limpar Táxis Externos, Limpar Tráfego Falso e Limpar Vagas Travadas.
* **Faixa de Telemetria ao Vivo:**
  * FPS Instantâneo, FPS Médio (120q), 1% Low, Tempo de Quadro (ms), Memória GC (MB), Eficiência do Relógio de Simulação (%) e Gap da Fila de Pathfinding (48/48).

---

## 🤖 Transparência & Suporte Ativo do Desenvolvedor (Disclaimer)

Este mod foi desenvolvido com o auxílio de inteligência artificial avançada, sob minha supervisão técnica, curadoria de código, testes exaustivos e conhecimento prático de programação como desenvolvedor.

Estou totalmente comprometido em manter este mod funcional e atualizado:
* **Suporte Contínuo:** Se qualquer atualização futura do Cities: Skylines II quebrar funcionalidades ou se você identificar bugs, lançarei correções prontamente.
* **Código 100% Aberto:** O código-fonte completo está disponível no [GitHub](https://github.com/demarscode2/cities2-ultra-performance-mod) para que a comunidade possa auditar, aprender e confiar no que está sendo executado no jogo.
* **Feedback:** Dúvidas, relatos de testes ou sugestões podem ser compartilhados na página do Paradox Mods ou nas *Issues* do GitHub!
