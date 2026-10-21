# ⚡ Ultra Performance & Simulation Booster (Beta v1.3.1)
### *O Mod Definitivo de Desempenho e Simulação para Cities: Skylines II*

![Ultra Performance Booster](Properties/Thumbnail.png)

O **Ultra Performance & Simulation Booster** foi desenvolvido sob medida para resolver os gargalos mais críticos do Cities: Skylines II: **queda drástica de FPS, lentidão extrema da simulação na CPU e congelamento do tráfego por cálculo excessivo de rotas (pathfinding)**.

> 🏙️ **Testado e Comprovado em Cidades Reais:** Testado com sucesso em uma megalópole de **700.000 habitantes** que travava constantemente e operava em lentidão extrema. Apenas com a ativação de algumas opções-chave, a simulação recuperou a fluidez e a taxa de quadros subiu significativamente!

---

## ⚠️ Recomendações e Avisos Importantes (Leia Antes de Usar)

* 💾 **Faça Backup do seu Save:** Embora todas as operações do mod utilizem chamadas nativas e seguras da ECS (`EntityManager`), sempre salve uma cópia do seu jogo antes de realizar limpezas em massa.
* **Use Ações Rápidas com Consciência:** Botões como *Concluir Obras Instantaneamente* ou *Remover Todos os Animais* operam em escala global no mapa.
* **Impactos Diretos na Gestão Urbana:**
  - **Polícia Automática:** Mantém a criminalidade da cidade permanentemente zerada. Viaturas policiais deixam de congestionar as ruas.
  - **Funerária Automática:** Os cidadãos que falecerem são recolhidos instantaneamente para a eternidade, eliminando o acúmulo de corpos nos imóveis e os rabecões nas ruas.
  - **Lixo Automático:** Esvazia o lixo de todos os imóveis, retirando caminhões coletores lentos do trânsito.
  - **Manutenção de Rodovias e Parques:** Mantém a qualidade do asfalto em 100% e o lazer em nível máximo sem lentidão nas pistas.
  - **Ambulâncias e Postos de Saúde:** Cura doentes e resolve emergências médicas sem ambulâncias travadas em engarrafamentos.

---

## 🔍 Problemas Conhecidos & Comportamentos Normais

1. **Caminhões Pequenos / Sem Trailer Temporariamente:**
   - Ao executar a limpeza de carretinhas ou ao ativar a remoção de reboques órfãos, alguns caminhões articulados podem ficar pequenos (apenas o cavalinho mecânico) durante alguns instantes.
   - **Solução:** É um comportamento visual temporário normal da ECS. Deixe o jogo rodar na velocidade 1x por alguns segundos e os veículos atualizarão suas malhas automaticamente.
2. **Motos e Bicicletas "Fantasmas":**
   - Ao ativar a opção **"Ocultar Cidadãos (NPCs)"**, o pedestre que pilota a bicicleta ou a motocicleta é ocultado, mas o veículo de duas rodas permanece visível, dando a impressão de "bicicletas andando sozinhas".
   - **Solução:** Se isso incomodar visualmente, ative as opções dedicadas **"Ocultar Bicicletas"** e **"Ocultar Motocicletas"** no painel do mod.

---

## 🖥️ Painel In-Game Exclusivo (HUD no Topo Esquerdo)

Esqueça a necessidade de abrir o menu de configurações do jogo. O mod adiciona um ícone nativo com o símbolo de **Raio ⚡** na barra superior esquerda (**GameTopLeft**, ao lado de ferramentas como Better Transit View e Park Manager).

Ao clicar no ícone, um painel flutuante estilizado se abre com:

### 1. 🔘 Aba de Ações Rápidas (14 Limpezas Instantâneas em 1 Clique)
* **Desobstruir Trânsito:** Remove pontualmente veículos em bloqueio circular real (gridlock), preservando semáforos e conversões normais.
* **Limpar Trailers, Reboques e Vans (Props):** Remove contêineres soltos, reboques órfãos e furgões persistentes inspecionados via Find It (`Trailer pequeno Prop`, `Trailer intermodal prop`, `trailer para tronco`, `Reboque prop`, `trailer de viagem`, `vans de entrega`).
* **Limpar Acidentes:** Remove todos os eventos de batida e carros capotados das pistas.
* **Limpar Todo Lixo:** Zera o lixo de todos os imóveis da cidade.
* **Coleta Funerária:** Recolhe todos os óbitos pendentes imediatamente.
* **Manutenção de Ruas:** Restaura 100% da condição do asfalto em todas as vias.
* **Manutenção de Parques:** Restaura o estado de conservação de todos os parques e praças.
* **Socorrer Cidade:** Cura imediatamente todos os doentes e acidentados.
* **Entregar Correspondências:** Zera cartas e encomendas acumuladas.
* **Limpar Animais:** Remove cães e animais soltos para poupar CPU de pathfinding.
* **Concluir Obras:** Conclui instantaneamente todas as construções e elevações de nível.
* **Limpar Táxis Externos:** Remove táxis vindos de fora do mapa pelas conexões regionais.
* **Limpar Tráfego Falso:** Remove veículos fictícios que apenas atravessam a cidade.
* **Limpar Vagas:** Libera vagas de estacionamento travadas.

### 2. 🎛️ Aba de Ajustes em Tempo Real (Live Settings)
* **Predefinições em 1 Clique:**
  - 🟢 **Básico:** Serviços automáticos essenciais, bloqueio de táxis de fora e limpeza de tráfego passante.
  - 🟡 **Médio (Recomendado):** Automações completas, animais invisíveis, água otimizada, ícones de alerta ocultos e sem vento nas árvores.
  - 🔴 **Máximo (FPS Extremo / Megalópoles):** Para cidades acima de 500k habitantes. Pedestres, veículos e árvores invisíveis, água estática e crescimento instantâneo.
* **Alternadores Individuais:**
  - Ocultar Cidadãos (NPCs)
  - Ocultar Veículos Gerais
  - Ocultar Bicicletas
  - Ocultar Motocicletas
  - Ocultar Árvores
  - Desativar Vento nas Árvores
  - Ocultar Ícones de Erro / Alerta Flutuantes
  - Desobstrução Automática Inteligente de Trânsito
* **Seletor de Simulação de Água:**
  - ❄️ *Congelada (Máximo FPS)*
  - 💧 *Otimizada (Cálculo suave espaçado)*
  - 🌊 *Vanilla (Padrão do Jogo)*

### 3. 💡 Aba de Dicas & Status
- Informações sobre o funcionamento da simulação, guia de resolução de gargalos e barra inferior de feedback instantâneo de cada comando acionado.

---

## 🚦 Trânsito Inteligente: Como Funciona a Nova Desobstrução?

Diferente de mods antigos que apagavam carros aguardando o semáforo abrir ou passageiros embarcarem (o que provocava recálculos em massa no processador), o algoritmo de trânsito deste mod possui **imunidade total a operações legítimas**:
- **Nunca remove:** carros esperando semáforo (`BlockerType.Signal`), faixa de pedestres (`Crossing`), pontes móveis (`Limit`) ou veículos efetuando manobra de estacionamento (`ParkingSpace`).
- **Detecta Deadlocks Reais:** Analisa a cadeia de bloqueadores e atua apenas se houver travamento circular (o carro da frente fecha o de trás e ninguém anda) ou carros presos fora da malha viária navegável.

---

## 📦 Instalação e Compilação

Para compilar e gerar a build:
```bash
dotnet build ./UltraPerformanceMod/UltraPerformanceMod.csproj -c Release
```
Os arquivos são copiados diretamente para a pasta de mods locais do Cities: Skylines II:
`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\UltraPerformanceMod`

---

## Transparência & Desenvolvimento (Aviso de IA e Suporte Ativo)

Este mod foi desenvolvido com o auxílio de inteligência artificial de última geração, sob minha supervisão técnica, curadoria de código e testes em cidades reais de grande escala (como a megalópole de 700k habitantes). Como criador, possuo conhecimento de programação e estou ativamente comprometido com a estabilidade e evolução do projeto.

* **Suporte Ativo:** Caso novas atualizações oficiais do Cities: Skylines II quebrem alguma função ou você encontre qualquer comportamento inesperado (bug), darei manutenção imediata e lançarei atualizações corretivas.
* **Código Aberto no GitHub:** O código-fonte completo está disponível publicamente para que qualquer jogador ou modder possa inspecionar a lógica, auditar a segurança e confiar plenamente no que está rodando em sua máquina.
* **Feedback:** Deixe seus comentários, sugestões ou dúvidas na página do mod no Paradox Mods ou no GitHub!
