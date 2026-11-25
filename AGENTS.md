# Instruções do Projeto e Diretrizes para Agentes — Dying Horizon

Este documento reúne todas as diretrizes técnicas, arquiteturais, de gameplay, arte e regras de segurança para o desenvolvimento colaborativo de **Dying Horizon**. Qualquer agente ou desenvolvedor que atue no repositório deve seguir rigorosamente as especificações abaixo.

---

## 1. Visão Geral do Jogo
- **Gênero:** Survival Horror / Roguelike Top-Down 2D pós-apocalíptico retro.
- **Engine:** Unity 6 (6000.6.10f1), Universal Render Pipeline (URP 2D).
- **Ambientação:** Zona Litorânea contaminada por radiação, infestada por fauna e humanos mutantes, com recursos escassos, armadilhas e ruínas de civilização.
- **Plataformas-Alvo:** PC / Linux / Windows (standalone nativo com suporte a controllers e teclado/mouse).

---

## 2. Restrições Essenciais de Arte e Sprites (Regra Inviolável)
- **Estilo Pixel Art 8-Bits Obrigatório:**
  - Todo e qualquer sprite gerado ou importado (personagens, inimigos, armas, projéteis, tilesets, árvores, armadilhas, ícones e itens) deve ter estética **estrita de pixel art 8-bits retro**.
  - Pixels nítidos, sem interpolação, sem anti-aliasing, sem gradientes suaves e com paleta reduzida.
  - Resolução nativa compacta: `16x16` (tiles de terreno, itens, projéteis), `24x24` a `32x32` (props, árvores, armas), e até `64x64` (entidades com animação, como o Sobrevivente e o Zumbi).
  - Fundo sempre transparente em formato PNG RGBA.
- **Configuração Obrigatória na Unity:**
  - Todos os assets em `DyingHorizon/Assets/Sprites/` devem ser importados com:
    - `Texture Type = Sprite (2D and UI)`
    - `Filter Mode = FilterMode.Point (no filter)`
    - `Compression = TextureImporterCompression.Uncompressed`
    - `Generate Mip Maps = false`
    - **Pixels Per Unit (PPU):**
      - `64f`: Personagens e Inimigos (`Survivor`, `Kraby`, `Zombie`, `Birdie`).
      - `32f`: Armas de fogo e fogueiras/estruturas médias.
      - `24f`: Árvores e vegetação costeira (`prop_coastal_palm`, `prop_dead_tree`, `prop_coastal_pine`).
      - `16f`: Tiles de terreno, itens coletáveis, projéteis e sombras circulares.

---

## 3. Estrutura do Workspace e Repositórios
```text
/home/anon/Dying Horiozn/
├── 01_Documentacao/                 # GDD, documentação de game design, lore e mecânicas
├── 02_Concept_Art_e_Mapas/          # Esboços visuais, conceitos de zonas, monstros e UI
├── 03_Sprites_e_PixelArt/           # Repositório de sprites brutos e spritesheets
│   ├── Armas/                       # Sprites originais de armamentos e projéteis
│   ├── Inimigos/                    # Spritesheets e frames individuais dos monstros
│   ├── Personagens/                 # Sprites e animações do Sobrevivente
│   └── Tilemaps/                    # Tilesets 16x16 Wang e atlas da Zona Litoral
├── DyingHorizon/                    # Projeto Unity 6 URP 2D
│   ├── Assets/
│   │   ├── Editor/                  # Scripts de automação, build de cena e inspetores
│   │   ├── Scenes/                  # Cenas principais (SampleScene.unity)
│   │   ├── Scripts/                 # Lógica de gameplay em C#
│   │   │   ├── Environment/         # Gerador procedural, YSort2D, TreeCanopyFader2D
│   │   │   ├── GameHUD.cs           # Interface visual, barras de vida e radiação
│   │   │   ├── GameLoop.cs          # Máquina de estados (Running, Victory, Defeat)
│   │   │   ├── MutantEnemy2D.cs     # IA, perseguição, desvio de obstáculos e dano
│   │   │   ├── PlayerController2D.cs# Controles WASD, física e animação de passos
│   │   │   ├── Projectile2D.cs      # Física e colisão de projéteis
│   │   │   ├── WeaponController2D.cs# Mira 360°, cadência, recuo e disparos
│   │   │   └── WeaponPickup2D.cs    # Coleta de armamentos no chão
│   │   ├── Sprites/                 # Sprites importados e configurados na Unity
│   │   └── Tilemaps/                # RuleTiles e tilesets da Zona Litoral
│   └── Builds/Linux/                # Executáveis standalone compilados
├── AGENTS.md                        # Este guia de governança e arquitetura
└── .gitignore                       # Proteção contra vazamento de credenciais e caches
```

---

## 4. Arquitetura dos Sistemas de Gameplay

### 4.1. Controles e Movimentação (`PlayerController2D.cs`)
- **Teclas Primárias:** `[W]`, `[A]`, `[S]`, `[D]` (com fallback automático para as setas direcionais).
- **Normalização de Vetor:** Velocidade idêntica nas diagonais sem acúmulo de magnitude (`moveSpeed = 4.5f`).
- **Ciclo de Animação 8-Bits:** Alternância de 4 passos (`0 -> 1 -> 0 -> 2`) a 8 FPS entre `Survivor_Walk_0`, `Survivor_Walk_1` e `Survivor_Walk_2`.
- **Orientação:** Espelhamento automático via `spriteRenderer.flipX` conforme o vetor horizontal.

### 4.2. Sistema de Armas e Combate (`WeaponController2D.cs`, `Projectile2D.cs`)
- **Seleção no Teclado:**
  - `[1]`: Pistola 1911 (cadência rápida, precisão pontual).
  - `[2]`: Escopeta Enferrujada (disparo em cone de 5 balotes, alto impacto a curta distância).
  - `[3]`: Fuzil de Assalto (disparos contínuos de alta velocidade).
  - `[0]`: Guardar arma (movimentação desarmada).
- **Mira e Gatilho:** Mira livre em 360° apontando para as coordenadas de tela do mouse. Disparo no clique esquerdo com spawn de projéteis físicos e efeito visual de recuo/fagulha.
- **Feedback de Combate:** Flash vermelho de dano (`hitFlashTimer`), knockback direcional e colisão balística contra mutantes.

### 4.3. Geração Procedural da Zona Litorânea (`CoastalProceduralGenerator.cs`)
- **Semente Determinística:** Geração reproduzível configurada via `seed: 2026`.
- **Topografia em Camadas:**
  - Oceano profundo com barreira física de colisão composta (`CompositeCollider2D`).
  - Animação procedural de ondas oceânicas e linha de espuma dinâmica (`AnimatedTile`).
  - Orla de areia e transição orgânica para vegetação com `RuleTile_PraiaAreia` e `RuleTile_VegetacaoCosteira`.
- **Profundidade e Cobertura de Árvores:**
  - Ordenação dinâmica por coordenada vertical através de `YSort2D`.
  - Sistema de transparência suave `TreeCanopyFader2D`: o sobrevivente caminha livremente sob as copas das árvores e palmeiras, que atenuam para 40% de opacidade quando ele se posiciona atrás delas.

### 4.4. Inteligência Artificial dos Inimigos Mutantes (`MutantEnemy2D.cs`)
- **Zumbi Mutante:**
  - Ciclo de passada contínua de 10 frames 8-bits extraídos do spritesheet original (`spr_zombie_walk_0..9`).
  - Orientação dinâmica com espelhamento `flipX` direcionado ao movimento.
  - Perseguição com raio de detecção de 7.5m e desvio tangencial de obstáculos via `CircleCast` (não trava em troncos ou rochas).
  - Ronda/patrulha orgânica no entorno do ponto de origem enquanto o jogador estiver fora de alcance.
- **Kraby:** Mutante resistente patrulhando as proximidades da areia.
- **Birdie:** Mutante ágil com voo rápido e investida.

---

## 5. Políticas de Segurança e Gestão de Segredos
- **Nunca comitar segredos:** Nenhuma chave de API (Gemini, PixelLab), token do Trello ou credencial deve ser versionada em texto puro ou nos arquivos de código.
- **Regras do `.gitignore`:**
  - Todos os arquivos `.env`, `.env.*`, `*token*`, `*secret*`, `*key*` e `*credentials*` estão estritamente bloqueados.
  - Configurações de MCP locais com chaves de terceiros são mantidas fora do versionamento.

---

## 6. Equipe e Endereçamento de Tarefas no Trello
Tarefas, issues e implementações devem ser devidamente rotuladas no Trello conforme a respectiva área de responsabilidade:

- **Alexandre Kauan (Programação / C# Unity):**
  - Implementação de mecânicas, física 2D, IA de inimigos, controles e GameLoops.
  - **Etiqueta Trello:** `Programação: Alexandre Kauan` *(Azul)*.
- **Ian Coelho (Game Design / Sistemas):**
  - GDD, balanceamento de atributos, economia, progressão e design de fases/armadilhas.
  - **Etiqueta Trello:** `Game Design: Ian Coelho` *(Verde)*.
- **Vinicius Figueiredo (Arte / Pixel Art 8-Bits):**
  - Criação e aprovação de sprites, animações e tilesets (estrito cumprimento da regra 8-bits).
  - **Etiqueta Trello:** `Arte: Vinicius Figueiredo` *(Roxo)*.
- **João Vitor Fidelix (QA / Testes):**
  - Testes de gameplay, homologação de builds, validação de colisões e reporte de bugs.
  - **Etiqueta Trello:** `QA / Testes: João Vitor Fidelix` *(Laranja)*.
- **THILAPIA (Áudio / Trilha Sonora & Músicas):**
  - Composição de trilhas retro/chiptune, efeitos sonoros (SFX) e ambientação.
  - **Etiqueta Trello:** `Áudio / Música: THILAPIA` *(Amarelo)*.

**Quadro Oficial do Trello:** [Dying Horizon](https://trello.com/b/Xo5jwWOj/dying-horizon)
