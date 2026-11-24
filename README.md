# Fluff Invasion

Jogo 3D do tipo **Tower Defense** feito na **Unity 6** para o trabalho prático de
**Computação Gráfica e Realidade Virtual (UPF, 2026/2)**, do Prof. Dr. Rafael Rieder.

O jogador defende a vila contra ondas de monstros que seguem um caminho até a base.
Para isso, constrói e melhora torres de defesa e também luta com a própria espada.

## História

Numa noite sem lua, um antigo círculo mágico escondido nos arredores da vila voltou a brilhar.
Ninguém lembrava quem o havia desenhado, mas todos logo descobriram para que servia: era um **portal para o inferno**.

Do outro lado, criaturas de todos os tipos começaram a atravessar, de galinhas possuídas e morcegos
a demônios, yetis e até um dragão. Em fila, elas marcham pelo único caminho que leva ao coração da vila,
e basta **uma** delas chegar lá para que tudo seja tomado pelas trevas.

Os moradores fugiram, e sobrou apenas um herói com sua espada, a velha forja do ferreiro e os locais
onde antigos guardiões ergueram torres de defesa. Cada criatura derrotada deixa para trás moedas,
que pagam novas torres e armas melhores.

Mas as ondas não param: a cada investida, os monstros voltam mais fortes e mais rápidos.
E quando a última onda chegar, o **senhor do inferno** em pessoa vai atravessar o portal,
gigante, furioso e chamando reforços. Resista até o fim e feche o portal de vez.

## Downloads

| | Link |
|---|---|
| Executável (Windows) | **[Baixar o jogo](COLOQUE_AQUI_O_LINK_DO_EXECUTAVEL)** |
| Vídeo demonstrativo | **[Assistir à demonstração](https://youtu.be/NZmpliFDs-U)** |

## Equipe

| Integrante | GitHub |
|---|---|
| Alice Neumann | [`alice`](https://github.com/alice-neumann) |
| Gabriel Trentini Teixeira | [PRRTZ](https://github.com/PRRTZ) |

---

## Como jogar

O objetivo é impedir que os monstros cheguem à base. Se **um único monstro** alcançar a base, ou se o
herói morrer, a fase está perdida. Para vencer, é preciso resistir a todas as ondas, incluindo o **chefe** da última onda.

### Controles

| Tecla | Ação |
|---|---|
| **W A S D** / setas | Mover o herói |
| **Shift** | Correr |
| **Espaço** | Pular |
| **J** / clique esquerdo | Atacar com a espada |
| **1 a 4** | Escolher a torre (perto de um local de construção) |
| **E** | Construir a torre escolhida / vender uma torre construída |
| **U** | Fazer o upgrade da torre mais próxima |
| **N** | Chamar a próxima onda antes da hora (ganha moedas de bônus) |
| **E** (na forja) | Abrir o menu de melhorias do herói (**1** = dano, **2** = vida) |
| **R** / **M** | Na tela de fim de jogo: jogar de novo / voltar ao menu |
| **Esc** | Pausar / voltar |

### Fluxo de uma partida
1. No **menu principal**, escolha **Jogar**, a **dificuldade**, o **mapa** e digite seu nome.
2. Antes de cada onda há um tempo de **preparação**: ande até os locais de construção e monte as torres.
3. Derrote monstros para ganhar **moedas** e use-as para construir, melhorar as torres ou melhorar o herói na **forja**.
4. Ao fim de cada onda você ganha um bônus de moedas e recupera um coração.
5. Na última onda aparece o **chefe**. Vencendo, a pontuação vai para o **ranking**.

---

## Funcionalidades

### Menu principal
- **Jogar:** escolha da dificuldade (**Normal** ou **Difícil**), do mapa e do nome do jogador.
- **Ranking:** top 5 por dificuldade, salvo no computador.

### Níveis de dificuldade
| | Normal | Difícil |
|---|---|---|
| Ondas | 15 | 20 |
| Monstros por onda | 3, 5, 7…  | 4, 6, 8… |
| Vida dos monstros | +18% por onda | ×1,3 e +17% por onda |
| Velocidade dos monstros | +3% por onda (até 1,5×) | ×1,15 e +3,5% por onda (até 1,8×) |
| Intervalo entre monstros | 0,8 s | 0,6 s |
| Tempo de preparação | 15 s | 12 s |
| Chefe | 15× a vida de um monstro, reforços a cada 8 s | 20× a vida, reforços a cada 6 s |
| Moedas por monstro | 6 (+6% por onda) | 5 (+5% por onda) |
| Multiplicador de pontos | ×1 | ×1,5 |

Os valores ficam em `Assets/Scripts/GameData.cs`.

### Estruturas defensivas (4 tipos)
| Torre | Dano | Intervalo | Alcance | Projétil | Comportamento |
|---|---|---|---|---|---|
| **Turret** | 6 | 0,25 s | 6 | bala | tiro rápido em um alvo |
| **Ballista** | 30 | 1,2 s | 9 | flecha | tiro preciso e de longo alcance |
| **Cannon** | 40 | 2 s | 8 | bala de canhão | dano em área (raio 2) |
| **Catapult** | 50 | 3 s | 12 | pedra em arco | dano em área (raio 3) e voo em arco |

- **Detecção:** cada torre procura o inimigo mais próximo dentro do alcance, gira a arma até ele e só dispara quando está alinhada.
- **Construção:** as torres são construídas pelo herói, que precisa ir até os locais pré-definidos (TowerSpots). Cada torre custa moedas.
- **Upgrade:** até o nível 3. Cada nível aumenta o dano em 35%, a cadência em 12% e o alcance em 0,5, e a torre cresce visualmente.
- **Venda:** devolve 50% de tudo o que foi investido na torre.

### Projéteis
Cada disparo cria um projétil a partir da torre, que se desloca até o alvo (em linha reta ou em arco),
detecta a colisão com o inimigo por *trigger*, aplica o dano (direto ou em área) e é destruído
ao acertar ou ao sair da área válida.

### Inimigos
- Mais de 20 monstros diferentes (Chicken, Pig, Crab, Bat, Ghost, Demon, Yeti, Cthulhu, Dragon…), com animações.
- Seguem um caminho definido por *waypoints* (pontos de entrada e caminho até a base).
- Têm vida e barra de vida, recebem dano das torres e da espada, e são destruídos ao zerar a vida, com efeito de confete.
- Causam dano ao herói quando encostam nele e causam **derrota ao alcançar a base**.
- Ficam mais resistentes e mais rápidos a cada onda.
- **Chefe** na última onda: 2,2× maior, com muita vida, barra de vida no topo da tela, chama **reforços** periodicamente e fica **furioso** (mais rápido) com metade da vida.

### Condições de vitória e derrota
- **Vitória:** resistir a todas as ondas da dificuldade escolhida, derrotando o chefe.
- **Derrota:** um inimigo alcançar a base, ou o herói morrer.
- A tela final mostra o motivo, a onda alcançada, a pontuação e a posição no ranking, com música de vitória ou de derrota.

### Pontuação e ranking
Pontos por monstro derrotado (valem mais nas ondas avançadas), por onda vencida, pelo chefe e pela vitória
(com bônus pelos corações restantes). A pontuação é multiplicada pela dificuldade e salva no ranking
ao fim da partida, na vitória ou na derrota.

### Economia e melhorias do herói
- Moedas por monstro derrotado, bônus ao fim de cada onda e bônus por chamar ondas antes da hora.
- **Forja:** melhora o dano da espada e a vida do herói (cada compra de vida cura totalmente).

### Efeitos visuais e sonoros
- Confete ao derrotar monstros, rastro do golpe da espada, efeitos mágicos e partículas.
- Torres que crescem a cada upgrade e animações de ataque, dano e morte.
- HUD com corações, moedas, onda atual, contagem até a próxima onda, pontuação, barra do chefe e avisos na tela.
- Música de fundo, sons ambientes, passos, ataques, torres, início de onda, vitória e derrota.

### Mapas
- **Mapa 1 – Castelo**
- **Mapa 2 – Vilarejo** (noite)

---

## Executar a partir do código-fonte
1. Instale a **Unity 6000.6.7f1** (pelo Unity Hub).
2. Clone o repositório:
   ```bash
   git clone https://github.com/PRRTZ/Jogo-Tower-Defense.git
   ```
3. Abra a pasta no Unity Hub (**Add > Add project from disk**).
4. Abra a cena `Assets/Scenes/MainMenu.unity` e aperte **Play**.

Para gerar o executável: **File > Build Profiles**, confira as cenas **MainMenu**, **Map_01** e **Map_02**, nesta ordem, e clique em **Build**.

## Estrutura dos scripts
| Pasta / script | Função |
|---|---|
| `MainMenu.cs`, `GameData.cs` | Menu, dificuldades, sessão da partida e ranking |
| `EnemySpawner.cs`, `Jogo/BossController.cs` | Ondas, força dos monstros por onda e chefe |
| `EnemyMovement.cs`, `EnemyHealth.cs`, `EnemyContactDamage.cs` | Movimento por waypoints, vida e dano dos inimigos |
| `Tower.cs`, `Projectile.cs` | Detecção, mira, disparo e projéteis das torres |
| `TowerBuilder.cs`, `TowerSpot.cs`, `Torres/TowerLevel.cs` | Construção, venda, preços e upgrades das torres |
| `Economia/` | Moedas e recompensas |
| `Jogo/GameOverManager.cs`, `Jogo/ScoreManager.cs` | Vitória, derrota, pontuação e ranking |
| `Player*.cs`, `Health.cs`, `Player/PlayerForge.cs` | Herói: movimento, ataque, vida e forja |
| `UI/` | HUD (ondas, moedas, torres), pausa e tema visual |
| `SoundManager.cs`, `EventSounds.cs`, `AmbientSound.cs` | Música e efeitos sonoros |

## Créditos dos assets
- Cenário, torres e itens: [Kenney](https://kenney.nl) (Tower Defense Kit, Castle Kit, Fantasy Town Kit, Hexagon Kit, Nature Kit, Platformer Kit, Skyboxes, Cursor Pack)
- Personagens e animações: [KayKit](https://kaylousberg.itch.io) e [Quaternius](https://quaternius.com)
- Efeitos visuais: Hovl Studio
- Interface: pacotes de UI em pixel art
- Engine: Unity 6

_Assets gratuitos, usados para fins acadêmicos._
