# 정식 그림 — 주방 화면 (2차 · 절구 · 떡메)

> **2026-10-05 (반복 07) : 주방 정본은 `v3/` 로 옮겨 갔다** (`v3/README.md`). 이 폴더에서는 `out/kitchen_plate.png` 만 아직 게임에 쓴다. 아래는 2차 기록이다.

> 2026-10-04 · 반복 04. **사용자 판정 대기.**
> 판정용 그림 : `Docs/Reference/Art/시안/2026-10-04-주방화면-정식-v2-미리보기.png` (9:16 쉼 · 9:20 맞는 순간 · 떡메 7장 띠 · 버튼은 아래 줄 · 모두 ×1)
> 동작 gif : `Docs/Reference/Art/시안/2026-10-04-떡메-동작.gif` (×2 · 쉼 → 세 번 치기 → 쉼)
> 스크립트 : `build_kitchen.py` (저장소 뿌리에서 `Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/주방/build_kitchen.py`)
> **이 폴더는 Unity 프로젝트에 아무것도 넣지 않았다.**

## 결론

- 사용자 피드백(10-04) : 「주방 그림풍은 좋다 · 절구와 떡메를 그려라」 를 반영했다.
- **절구 + 떡메 조각 7장** (쉼 1 + 한 번 치기 6) 을 한 캔버스 · 같은 피벗으로 냈다. 절구 몸통은 7장 내내 같은 자리다.
- 절구를 **왼쪽 가운데에 크게**(몸통 폭 약 150) 두고, 오른쪽에 결과 접시를 놓았다. 확대 없이 ×1 이다 (도트 1px 유지).
- 재료 쟁반을 **넓은 새 조각**(260×48 · 칸 여섯)으로 따로 뽑아 벽 쟁반 위에 얹었다. 재료 아이콘(28×28)이 칸 안에 들어간다.
- 배경 천장 · 벽은 1차 그대로다. **마루만 다시 뽑았다** — 1차 마루의 둥근 밝은 얼룩이 절구 · 접시 자리와 겹쳐서다. 1차 배경은 `out/kitchen_bg_v1.png` 로 남겼다.
- 떡 반죽 단계 그림(찹쌀 → 반죽 → 떡)은 **안 그렸다.** 코드(`StallScreen` 의 `PoundDoughBox` 하나를 찌그러뜨림 · `PoundTaps` 3번)가 단계 그림을 쓰지 않아서다.

## out 파일

| 파일 | 크기 · 색 | 피벗 | 무엇 |
| --- | --- | --- | --- |
| `kitchen_bg.png` | 540×1200 · 불투명 · 64색 | 아래 가운데 | 배경. 화면 아래끝에 붙인다 |
| `kitchen_bg_v1.png` | 540×1200 · 59색 | 아래 가운데 | 1차 (마루에 밝은 얼룩). 비교용 |
| `kitchen_pound_0.png` ~ `_6.png` | 212×241 · 투명 · 44~60색 | **(79, 241)** = 절구 바닥 가운데 | 0 = 떡메 든 쉼 · 1~6 = 한 번 치기 |
| `kitchen_pound_strip.png` | 1484×241 | — | 위 7장을 가로로 이은 띠 (프레임 하나 212 폭) |
| `kitchen_tray.png` | 260×48 · 투명 · 22색 | 왼쪽 위 | 재료 쟁반 · 칸 여섯 |
| `kitchen_plate.png` | 81×45 · 투명 · 37색 | 아래 가운데 | 결과 접시 (받침 달린 분홍 테 접시) |

## 프레임 · 시간 (한 번 치기)

| 순서 | 프레임 | 간격 (권장) | 모습 |
| --- | --- | --- | --- |
| 1 | 0 | 60ms | 떡메 들기 (쉼과 같은 그림) |
| 2 | 1 | 50ms | 내림 |
| 3 | 2 | 50ms | 내림 |
| 4 | 3 | 40ms | 닿음 |
| 5 | 4 | **120ms** | 맞음 — 쌀이 납작 · 튀는 점. 여기서 소리 · `PoundDoughBox` 찌그러뜨림 |
| 6 | 5 | 60ms | 튐 |
| 7 | 6 | 60ms | 올림 |

- 탭 한 번에 0→6 을 한 번 돌고 0 에서 멈춘다 (한 번 약 0.44초). 쉬는 동안은 0 한 장.
- 피벗은 절구 바닥 가운데라 떡메가 위로 올라가도 절구가 안 움직인다. 왼쪽 위로 떡메 머리가 나가므로 캔버스 위쪽 여백이 크다.

## 코드에 넘길 것 (540×1200 세계 · 왼쪽 위 원점)

> 9:16 화면 좌표는 y 에서 **240 을 뺀다**. 화면은 아래끝 기준이다.

| 무엇 | 세계 좌표 | 9:16 화면 좌표 |
| --- | --- | --- |
| 벽 시작 (천장 몰딩 아래) | y 254 | y 14 |
| 마루 시작 (걸레받이 아래) | y **673** | y 433 |
| 절구 피벗 (바닥 가운데) | **(190, 1000)** · 조각 왼쪽 위 (111, 759) | (190, 760) |
| 재료 쟁반 왼쪽 위 | **(255, 520)** · 크기 260×48 | (255, 280) |
| 쟁반 칸 여섯 (안쪽 · 세계 x) | 276~304 · 313~342 · 350~379 · 387~417 · 424~455 · 462~491, y **526~565** | y 286~325 |
| 쟁반 칸 (쟁반 안 x) | 21~49 · 58~87 · 95~124 · 132~162 · 169~200 · 207~236, y 6~45 | — |
| 남은 수 글자 (미리보기 값) | 칸 가운데 x · 쟁반 아래 + 12 (y 580) | y 340 |
| 결과 접시 (아래 가운데) | **(420, 1000)** · 조각 왼쪽 위 (380, 955). 떡(32×32)은 접시 위끝 + 4 에 가운데 맞춤 | (420, 760) |
| 아래 버튼 줄 (미리보기 값) | 화면 아래끝에서 104 위 ~ 24 위 · 떡메 치기 220 · 레시피 도감 120 · 닫기 100 | y 856~936 |
| 천장 · 종이 등 셋 | y 0~240 — **9:20 에서만 보인다** | 안 보임 |

- 재료 아이콘은 칸 아래끝에 바닥을 맞춰 가운데 놓는다. 수가 0 이면 아이콘을 어둡게 한다 (미리보기 값).
- 겹 순서 : 배경 → 쟁반 → 재료 아이콘 → 절구+떡메 → 접시 → 결과 떡 → UI.

## 도구 · 씨앗 · 프롬프트 (2차)

| raw | 도구 · 크기 · 회 | 씨앗 | 화풍 참고 | 프롬프트 요지 |
| --- | --- | --- | --- | --- |
| **`mortar_raise_20701`** (씀) | `create_image_pro_flash` 240×256 · 9 | 20701 | 주방 벽 작업대 48×48 조각 (16색으로 줄인 base64) | `Straight front view, flat 2D, slightly from above so the inside of the bowl is visible. One big cute round wooden rice-cake mortar (Korean jeolgu / Japanese usu): a wide thick barrel-shaped warm brown wooden bowl with darker wooden bands … filled with a soft white mound of steamed sticky rice. Above it, a big wooden mallet (tteokme) with a thick barrel head and a long handle floats raised high, tilted, ready to strike down; no hands, no person. … about 190 pixels wide. No text.` |
| **`pound_20702/f0~f6`** (씀) | `animate_image` 6 프레임 · 6 | 20702 | first_frame = `mortar_raise_20701` | `the big wooden mallet swings down hard and strikes the white rice inside the mortar with a thump (the rice squashes flat), then lifts back up to where it started. The mortar does not move.` |
| `tray_20902` (버림) | `create_image_pro_flash` 252×64 · 9 | 20902 | 작업대 조각 | `One long shallow light honey-brown wooden tray divided by thin wooden walls into six equal empty square compartments …` — 너무 작게 왔다 |
| **`tray_20904`** (씀) | `create_image_pro_flash` 252×64 · 9 | 20904 | style = `tray_20902` | `… seen from above at a steep angle … One long wooden tray that fills the whole canvas width edge to edge … thick outer rim … six equal empty deep square compartments in one row, each compartment as tall as it is wide …` |
| `plate_20903` (버림) | `create_image_pro_flash` 96×64 · 5 | 20903 | 작업대 조각 | `One cute round shallow cream ceramic serving plate with a soft pink rim, sitting on a small round light wooden stand …` — 너무 작게 왔다 |
| **`plate_20905`** (씀) | `create_image_pro_flash` 128×80 · 6 | 20905 | style = `plate_20903` | `One big cute round shallow cream ceramic serving plate with a soft pink rim, sitting on a low round light wooden stand with little feet. The plate fills most of the canvas width …` |
| **`floor_20105`** (씀) | `create_image_pro` 540×420 · 40 | 20105 | 1차 마루 `floor_20103` 64×64 조각 (16색 base64) | `… A cozy cafe kitchen wooden plank floor seen from a low front angle: warm light honey-brown wooden floorboards with planks running horizontally in neat rows, thin darker seams between planks, a few small wood grain knots. Even, uniform lighting everywhere: no light spots, no bright circles, no rugs, no shadows, no stains. Only the floor, nothing else.` |

천장 `top_20104` · 벽 `wall_20101` 은 1차 그대로다 (1차 프롬프트는 아래 「1차 기록」).

## 손질 (PIL — 자르기 · 줄 잇기 · 배치 · 프레임 잇기만)

- 배경 : 천장 0~237 · 344~359 줄 + 벽 9~427 줄 + **마루 `floor_20105` 0~412 줄 + 126~239 줄** (413 + 114 = 527 줄). 두 조각 모두 널 이음 줄에서 끊었다.
- 떡메 : 7장 모두 (16, 2)~(228, 243) 칸으로 잘랐다 (212×241). 절구 몸통은 원본에서 이미 7장 같은 자리(x 26~163 · 아래끝 243)라 옮기지 않았다.
- 쟁반 : `tray_20904` 의 (7, 8)~(245, 56) 를 자른 뒤 **양 끝 테두리 세로 줄을 11 줄씩 되풀이**해 260 폭으로 늘렸다 (칸 안은 안 건드림).
- 접시 : `plate_20905` 의 (23, 17)~(104, 62).

## 깨끗하지 않은 곳

- **떡메를 칠 때마다 쌀이 다시 봉긋해진다.** 0번(쉼)이 봉긋한 쌀이라, 세 번 치는 동안 쌀이 점점 떡이 되는 모습은 없다. 반죽 단계는 코드의 `PoundDoughBox` 가 맡는다.
- 떡메 손잡이가 사람 손 없이 떠 있다 (부탁대로). 맞음 프레임 4 · 5 의 흰 튀는 점이 1~2px 로 작다.
- 새 마루에 눈 모양 옹이가 여럿 있다 (밝은 얼룩은 없어졌다). 접시 아래 옹이가 접시 받침과 겹쳐 보일 수 있다.
- 쟁반은 벽 그림의 옛 작은 쟁반 위에 덮어 얹는다 — 옛 쟁반 둘레가 몇 px 비칠 수 있다 (미리보기에서는 안 보였다).
- 작업대가 화면 오른쪽 끝에서 잘리는 것 · 천장 오른쪽 끝 작은 분홍 얼룩은 1차 그대로다.

## 쓴 크레딧 (2차 · 주방 몫)

절구 9 + 떡메 동작 6 + 쟁반 9 × 2 + 접시 5 + 6 + 마루 40 = **84회**.

## 1차 기록 (v1 · 2026-10-04 오전)

- 배경 `out/kitchen_bg_v1.png` : 천장 `top_20104` 0~237 · 344~359 줄 + 벽 `wall_20101` 9~427 줄 + 마루 `floor_20103` 0~386 · 34~173 줄.
- `wall_20101` (create_image_pro 540×440 · 씨앗 20101 · 가판대 배경 128 조각 화풍) : 분홍 발 문 · 선반 둘 · 둥근 창 · 작업대 · 칸 여섯 쟁반.
- `top_20102` 는 문 · 작업대가 다시 나와 버렸다. `top_20104` (씨앗 20104) 는 합성소 1차 천장 `top_20202` 를 화풍으로 줬다.
- `floor_20103` (씨앗 20103) 은 둥근 밝은 얼룩이 두세 개 있어 2차에서 바꿨다.
- 1차 크레딧 160회 (4 × 40).
