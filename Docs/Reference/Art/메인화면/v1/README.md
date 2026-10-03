# 메인 화면 목업 v1 — 부품과 스크립트

> 2026-10-03 반복 03 · **시안용이다. 게임에 넣지 않았다.**
> 시안 그림은 `Docs/Reference/Art/시안/지난판/메인화면/2026-10-03-메인화면-가.png` · `-나.png` · `-부품표.png` 에 있다.

## 폴더

| 자리 | 무엇 |
| --- | --- |
| `raw/` | PixelLab 이 준 원본 그대로 (이름 끝 숫자가 씨앗) |
| `parts/` | 시안에 쓴 부품. 원본에서 **자르기·배경 지우기만** 한 투명 PNG |
| `build_mockup.py` | 부품을 다듬고 시안 세 장을 굽는 PIL 스크립트 |

다시 굽기 (저장소 뿌리에서) : `Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/메인화면/v1/build_mockup.py`

## 부품 (시안에 쓴 것)

| 부품 | 크기 (자른 뒤) | 도구 · 캔버스 · 씨앗 | 원본 |
| --- | --- | --- | --- |
| `cafe_34` 가판대 3/4 (가) | 144×146 | `create_image_pro_flash` 208×224 · 3101 | `cafe34_3101` |
| `synth_34` 합성소 3/4 (가) | 176×170 | pro_flash 208×224 · 3202 | `synth34_3202` |
| `cafe_front` 가판대 정면 (나) | 201×173 | pro_flash 256×256 · 3302 | `cafefront_3302` |
| `synth_front` 합성소 정면 (나) | 147×217 | pro_flash 192×256 · 3401 | `synthfront_3401` |
| `chest` 창고 상자 | 78×56 | pro_flash 96×96 · 3502 | `chest_3502` |
| `forest` 먼 숲 띠 | 400×69 | `create_image_pixflux` 400×96 · 3601 · side · lineless · flat shading | `forest_3601` |
| `clouds` 구름 | 400×120 | pixflux 400×200 · 3701 · side · lineless · basic shading | `clouds_3701` |
| `path` 길 (나) | 340×60 | pixflux 400×64 · 3901 · high top-down · lineless · flat shading | `path_3901` |
| `ui_panel` 종이 패널 · `ui_tab` 작은 판 | 258×122 · 91×35 | `create_ui_asset` 384×384 · 3801 · elements = panel, button, icon_button ×4, tab | `uikit_3801` |
| `ui_btn` 메뉴 버튼(빈) | 48×48 | pro_flash 64×64 · 4002 · `no_background=false` (청록 바탕) | `btn_blank_4002` |
| `ui_btn_menu` 햄버거 버튼 | 48×50 | pro_flash 64×64 · 4101 | `btn_menu_4101` |
| `ui_capsule` 재화 캡슐 | 106×29 | pro_flash 112×36 · 4401 | `capsule_4401` |
| `ui_ribbon` 리본 제목 띠 | 126×23 | pro_flash 160×40 · 4301 | `ribbon_4301` |
| `icon_book` · `icon_basket` · `icon_scroll` | 20×25 · 22×24 · 19×22 | pro_flash 32×32 · 4201 · 4202 · 4203 | `icon_*` |
| `icon_coin` | 18×19 | pro_flash 24×24 · 4204 | `icon_coin_4204` |

안 쓴 후보 : `cafe34_3102` · `synth34_3201` · `cafefront_3301` · `chest_3501` · `btn_blank_4001`(종이 면이 뚫려서 왔다).

찹쌀이 · 풀밭 · 담장 · 재료 아이콘 · 손님은 **지금 게임 그림**(`ProjectMozziUnity/Assets/_Project/Resources/Art/`)을 읽어 그대로 얹었다.
글꼴은 게임이 쓰는 `Assets/_Project/Fonts/ONEMobilePOP.ttf` (둥근 글꼴)다. ×2 로 키운 뒤 얹어 글은 또렷하다.

## 프롬프트

공통 꼬리 (건물·상자) : `Cozy pixel art, soft muted pastel palette, low saturation, dark brown colored 1px outline (no pure black), 3-tone cel shading with hue shift, light from top-left, no anti-aliasing, no dithering, clean pixel clusters. Whole building visible, no ground, no text.`

| 부품 | 글머리 |
| --- | --- |
| 가판대 3/4 | `Small modern rice-cake cafe building exterior for a cozy farm game, top-down 3/4 view, large roof and visible front wall with door. Cream white walls, pink-and-white striped awning over a large glass window showing rice cakes on display, small round hanging signboard with a rice cake icon (no text), flower planter box and a small chalkboard stand by the door. Storybook cottage scale.` |
| 합성소 3/4 | `Cute rice-cake mill workshop building exterior for a cozy farm game, top-down 3/4 view, large warm roof and visible front wall with a round door. On the roof a friendly round cream-and-pink mochi-making machine with two funnels on top and a small chimney puffing a little steam, a round window showing a turning drum. Wooden walls, rice sacks, a wooden mortar and mallet and a barrel beside the wall. Bakery / rice cake shop feeling, not a laboratory, no flasks, no chemistry.` |
| 가판대 정면 | `Modern rice-cake cafe storefront for a cozy game, straight-on front view facade (flat elevation, no perspective). Wide pink-and-white striped awning, large glass display window with rice cakes on trays, glass door, blank wooden signboard above the awning with a rice cake icon (no text), flower boxes under the window, two potted plants, a small bench and a chalkboard stand in front. Cream walls, pastel pink trim, tiled roof edge on top.` |
| 합성소 정면 | `Cute rice-cake mill workshop storefront for a cozy game, straight-on front view facade (flat elevation, no perspective), narrow and tall. Wooden walls, warm tiled roof, a friendly round cream-and-pink mochi-making machine built into the front with two funnels on top, a round glass window showing a turning drum, and a small chimney with a little steam puff. Round wooden door, rice sacks and a wooden mortar with mallet beside it. Rice cake shop feeling, not a laboratory, no flasks.` |
| 창고 상자 | `Large wooden storage chest for a cozy farm game, top-down 3/4 view, lid closed, metal corner bands and a round latch, with a smaller wooden crate and a rice sack stacked beside it.` |
| 먼 숲 | `distant forest treeline silhouette band, side view horizon, rounded fluffy tree tops in 2 tones of muted blue-green fading into pale sky, atmospheric perspective, flat pastel sky blue background above, trees fill the bottom half and run across the full width, cozy pixel art, low saturation, no dithering` |
| 구름 | `fluffy cumulus clouds in 3 tones (white, cream, pale lavender shadow) on a flat pastel sky blue background, several separate clouds of different sizes, side view, cozy pixel art, low saturation, clean pixel clusters, no dithering, no gradient` |
| 길 | `top-down sandy dirt path ground texture, flat pale sand color with sparse small pebbles and tiny grass tufts at the edges, low detail ground, readable, cozy pixel art, soft muted low saturation, no dithering, fills the whole canvas` |
| UI 묶음 | `storybook game UI kit: parchment paper panel with wooden frame, rounded wooden buttons with paper face, cozy pixel art, soft muted low-saturation palette, dark brown 1px outline (no pure black), 3-tone shading, no anti-aliasing, no text` · color_palette `apricot parchment #E8B887, wood brown #9C644E and #6D3D2B` |
| 빈 버튼 | `Empty square game UI button on a flat solid teal background: rounded wooden frame (warm brown #9C644E, dark edge #6D3D2B) with a plain apricot parchment paper face (#E8B887) filled in, blank face, no icon, no text. Front flat view.` + UI 꼬리 |
| 햄버거 버튼 | 위와 같은 틀에 `showing three thick horizontal dark brown bars stacked (menu icon)` (투명 바탕) |
| 캡슐 | `Game UI capsule plate, wide horizontal pill shape: rounded wooden frame … plain apricot parchment paper face, completely blank` |
| 리본 | `Game UI title ribbon banner, horizontal, muted sage green cloth ribbon with folded swallow-tail ends, completely blank, no text, no emblem.` |
| 아이콘 | `Game UI icon: closed storybook with a soft pink cover and a round white rice cake emblem…` · `woven shopping basket with a pink-and-white striped cloth and rice cakes…` · `rolled parchment quest scroll tied with a pink ribbon…` · `single round gold coin with a small rice cake emblem embossed` |

UI 꼬리 : `Cozy storybook pixel art, low saturation, dark brown 1px outline (no pure black), 3-tone shading, light from top-left, no anti-aliasing.`

## PIL 이 한 일 (그리거나 다시 칠한 곳은 없다)

- 자르기 : 투명 여백 걷기, UI 묶음에서 패널·작은 판 떼기, 길 가운데 340px, 구름 그림 아래쪽 안개 띠 잘라 내기.
- 배경 지우기 : 구름·숲의 하늘색을 **색 한 값**으로 지움 · 빈 버튼의 청록 바탕을 모서리에서 번져 가며 지움.
- 이어 붙이기 : 숲 띠와 길은 원본 + 좌우 뒤집은 것을 이어 540 폭을 채움 · 패널은 9조각(모서리 그대로, 변과 속은 타일처럼 반복).
- 하늘 바탕은 구름 그림의 바탕색 `#D4EEFA` 한 색이다. 찹쌀이 몸 색은 게임처럼 종 색을 곱했다.

## 깨끗하지 않은 곳

- **건물이 조사 문서의 크기(180~220)보다 작게 왔다.** 208×224 캔버스에 주문해도 그림은 144×146 · 176×170 이다. 크게 쓰려면 캔버스를 256 으로 올려 다시 뽑는다.
- **3/4 건물은 모서리에서 본 비스듬한 각(아이소메트릭에 가까움)이다.** 농장 타일의 「정면 3/4」과 각이 조금 다르다.
- **종이 패널이 무겁다.** 테두리 조각이 진한 적갈색에 장식이 많아 중심 화풍보다 RPG 쪽이다. 캡슐·버튼(밝은 나무)과 톤이 다르다.
- 작은 판을 9조각으로 늘린 알림 패널은 종이 무늬가 91px 마다 되풀이된다.
- 구름 한 덩이가 원본 그림 가장자리에서 잘려 있다 (화면 밖으로 밀어 가렸다).
- 찹쌀이·풀밭·재료 아이콘은 외곽선이 없고 채도가 높다 — 새로 뽑은 건물(외곽선 · 낮은 채도)과 나란히 두면 차이가 보인다. 이번엔 손대지 않았다.
- 손님(나)은 지금 게임의 48×64 그림이라 건물보다 픽셀은 같은 굵기지만 검정 외곽선이다.
- 버튼 이름 「도감 · 상점 · 임무 · 설정」은 자리표시다. 늘 보일 메뉴가 무엇인지는 아직 안 정했다.
