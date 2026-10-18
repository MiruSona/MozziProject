# 정식 그림 — 합성소 화면 (2차 · 기계실)

> **2026-10-18 정리** : 이 문서가 가리키던 그림 중 안 쓰는 시안·견줌판·옛 판은 저장소에서 지웠다 (`(정리함 2026-10-18)` 표시). 다시 보려면 git 기록에서 꺼낸다.

> 2026-10-04 · 반복 04. **사용자 판정 대기.**
> 판정용 그림 : `Docs/Reference/Art/시안/2026-10-04-합성소화면-정식-v2-미리보기.png` (9:16 · 9:20 벽 a · 9:20 벽 b. 도는 중 · 쉼 · 살 수 있음 · 잠김 · 다 됨이 한 화면에 · 버튼은 아래 줄 · 모두 ×1)
> 동작 gif : `Docs/Reference/Art/시안/2026-10-04-합성기계-동작.gif` (×2 · 도는 고리 3바퀴 → 다 됨 → 쉼)
> 스크립트 : `build_synthesis.py` (저장소 뿌리에서 `Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/합성소/build_synthesis.py`)
> **이 폴더는 Unity 프로젝트에 아무것도 넣지 않았다.**

## 결론

- 사용자 피드백(10-04) : 「갈색 나무 벽은 좋다 · 창고 같다 → 기계실 느낌 · 기계 동작 · 빈 받침+간판 · 덮개+자물쇠」 를 반영했다.
- **창고 소품(쌀자루 · 병 선반 · 큰 둥근 창)을 뺐다.** 벽 윗면에 구리 배관 · 빨간 밸브 · 놋쇠 계기판 · 스위치 판 · 톱니와 벨트 · 김 나는 주전자 탱크를 그렸다. 천장도 구리관 + 철망 등으로 바꿨다. 크림 회벽 · 갈색 판벽 · 붉은 마루는 그대로다.
- 벽 후보 둘 : **a**(창 없이 배관 · 계기판 둘 · 주전자 탱크 — 기본으로 `synthesis_bg.png` 에 냈다) · **b**(둥근 창 + 큰 계기판 · 스위치 판 · 톱니).
- 기계는 **208×220 한 캔버스 · 같은 피벗**에 쉼 1 · 도는 중 8 · 다 됨 1 · 살 칸 1 · 잠김 1 을 모두 맞췄다. 바꿔 끼우기만 하면 된다.
- 쉼 그림의 서랍을 **닫힌 모양으로 고쳤다**. 창 속은 모든 프레임에서 빈 회색 통이다 (음양은 코드가 얹는다).
- 1차 배경은 `out/synthesis_bg_v1.png`(정리함 2026-10-18) 로 남겼다 (창고 느낌 · 1차 기록은 아래).

## out 파일

| 파일 | 크기 · 색 | 피벗 | 무엇 |
| --- | --- | --- | --- |
| `synthesis_bg.png` | 540×1200 · 불투명 · 68색 | 아래 가운데 | 기본 배경 = 벽 a. 화면 아래끝에 붙인다 |
| `synthesis_bg_a.png`(정리함 2026-10-18) · `synthesis_bg_b.png` | 540×1200 · 68 · 71색 | 아래 가운데 | 벽 후보 둘 |
| `synthesis_bg_v1.png`(정리함 2026-10-18) | 540×1200 · 49색 | 아래 가운데 | 1차 (창고 느낌). 비교용 |
| `synthesis_machine.png` | 208×220 · 투명 · 36색 | **(104, 200)** | 쉼. 서랍 닫힘 · 창 속 빈 통 |
| `synthesis_machine_run_0.png` ~ `_7.png` | 208×220 · 40~46색 | (104, 200) | 도는 중 8장 고리 (손잡이 돌기 · 몸 덜컹 · 김 방울) |
| `synthesis_machine_run_strip.png` | 1664×220 | — | 위 8장을 가로로 이은 띠 (프레임 하나 208 폭) |
| `synthesis_machine_done.png` | 208×220 · 17색 | (104, 200) | 다 됨. 서랍이 앞으로 열리고 반짝이 |
| `synthesis_slot_buy.png` | 208×220 · 33색 | (104, 200) | 산 적 없는 칸 = 빈 받침 + 빈 간판 (글자는 코드) |
| `synthesis_slot_locked.png` | 208×220 · 20색 | (104, 200) | 잠긴 칸 = 천 덮개 + 밧줄 + 자물쇠 |
| `synthesis_plate.png` | 116×26 · 45색 | — | 나무 표 (남은 시간 · 시작 20G). 9-slice 왼 12 · 위 8 · 오른 12 · 아래 8. 정본은 색을 합친 `화풍통일/out/synthesis_plate.png` (26색, 2026-10-05) 하나다 — `out/` 의 45색 옛 판은 지웠다 |

**피벗이 아래끝이 아니다.** 다 됨 그림의 열린 서랍이 발 아래로 내려오므로 캔버스에 발 아래 20 줄을 두었다. 피벗 (104, 200) = 받침 발 가운데 · 정규화 (0.5, 0.0909) (Unity 는 아래가 0).

## 프레임 · 시간

| 상태 | 프레임 | 간격 |
| --- | --- | --- |
| 쉼 (Idle · 넣는 중 Merging) | `synthesis_machine` 1장 | — |
| 도는 중 (Running) | `run_0 → 1 → … → 7 → 0` 고리 | **90ms** 권장 (한 바퀴 0.72초) |
| 다 됨 (Done) | `synthesis_machine_done` 1장 | — (gif 에서는 900ms) |
| 살 수 있음 · 잠김 | `synthesis_slot_buy` · `synthesis_slot_locked` | — |

음양 회전은 계속 코드가 창 원 안에 그린다. 도는 프레임마다 몸이 위아래로 0~1px 덜컹이지만 창 자리는 8장 모두 같다 (재 보니 dy 0).

## 코드에 넘길 것 (540×1200 세계 · 왼쪽 위 원점)

> 9:16 화면 좌표는 y 에서 **240 을 뺀다**. 화면은 아래끝 기준이다.

| 무엇 | 세계 좌표 | 9:16 화면 좌표 |
| --- | --- | --- |
| 벽 시작 (천장 몰딩 아래) | 벽 a y **234** · 벽 b y 246 | y −6 · 6 |
| 마루 시작 (걸레받이 아래) | y **643** (1차와 같다) | y 403 |
| 칸 넷 — 피벗(발 가운데)이 놓일 자리 | (135, 670) · (405, 670) · (135, 950) · (405, 950) (1차와 같다) | y 430 · 710 |
| 조각 왼쪽 위 | (31, 470) · (301, 470) · (31, 750) · (301, 750) | y 230 · 510 |
| 나무 표 (미리보기 값) | 칸 피벗 y + 4 ~ + 30, 폭 150 가운데 맞춤 | — |
| 아래 버튼 줄 (미리보기 값) | 화면 아래끝에서 104 위 ~ 24 위 · 단축 사기 220 · 찹쌀이 도감 120 · 닫기 100. 그 위 22 에 「영구 단축 0/5」 | y 856~936 |
| 천장 (구리관 · 철망 등 둘) | y 0~240 — **9:20 에서만 보인다** | 안 보임 |

기계 조각 안의 자리 (조각 왼쪽 위 원점 · 208×220, 쉼 그림 기준) :

| 무엇 | 조각 좌표 |
| --- | --- |
| 왼쪽 깔때기 입구 가운데 | (56, 20) |
| 오른쪽 깔때기 입구 가운데 | (142, 20) |
| 둥근 창 (음양 · 도는 통) | 가운데 **(97, 117)** · 반지름 31 |
| 서랍 앞판 (쉼 그림) | (52, 164) ~ (120, 194) |
| 다 됨 「?」 · 결과 그림자 가운데 (2026-10-05) | **(97, 174) · ×1** — 다 됨 그림은 서랍이 빠져나와 안쪽 칸이 x 70 ~ 124 · 바닥 y 192 다 (`SynthesisRoomLayout.cs` 의 `OutletX/Y/Scale`). 옛 값 (86, 180) · ×0.5 는 쉼 그림 앞판 가운데였다 |
| 피벗 (발 가운데) | (104, 200) |

- 겹 순서 : 배경 → 위 줄 기계 → 위 줄 표 → 아래 줄 기계 → 아래 줄 표 → 말풍선 → UI.
- 1차 README 의 창 가운데 값 (74, 112) 은 이번 캔버스에서 틀어졌다. 위 표 값을 쓴다.

## 도구 · 씨앗 · 프롬프트 (2차)

| raw | 도구 · 크기 · 회 | 씨앗 | 화풍 참고 | 프롬프트 요지 |
| --- | --- | --- | --- | --- |
| **`wall_20401`** (벽 a · 씀) | `create_image_pro` 540×440 · 40 | 20401 | 1차 벽 `wall_20201` 의 128 조각 (base64) | `Flat 2D side-view … one back wall only … The back wall of a cute cozy little machine workshop where a friendly rice-cake machine works (a warm, gentle boiler room, not a scary factory). Upper wall: warm cream plaster. Lower half: warm brown vertical wooden plank wainscoting … shiny copper pipes … round red valve wheels; two round brass pressure gauges …; a small wooden switch panel …; a small round brass vent grille; a cute round copper kettle-shaped water tank on the right with a little puff of white steam; a few brass gears and a leather belt … No machines on the floor, no sacks, no jars, no shelves, no people, no text.` |
| **`wall_20402`** (벽 b · 씀) | `create_image_pro` 540×440 · 40 | 20402 | 같은 조각 | `… a cute small cozy machine room in a rice-cake workshop, warm and friendly like a storybook boiler room … one large round wooden porthole window. Around it: copper pipes … one big round brass gauge … a wooden board with a row of round toggle switches and a tiny red lamp, a small chimney-like vent pipe … a big brass cogwheel and a smaller one linked by a belt …` |
| `wall_20402_window_20403` | `inpaint_image_pro_flash` 160×160 · 6 | 20403 | — (마스크 = 흰 창 속) | `Inside of a round porthole window …: pale light-blue glass with a soft sky and two diagonal white glare streaks, no frame change.` |
| **`top_20404`** (씀) | `create_image_pro` 540×360 · 40 | 20404 | 1차 천장 `top_20202` 조각 | `… ONLY the top part of a cozy little machine workshop interior: … two thick horizontal wooden ceiling beams; two shiny copper pipes … two cute round warm-glowing lamps with little brass cages hang on short chains … crown molding … along the very bottom edge … Nothing else` |
| `machine_closed_20501` | `inpaint_image_pro_flash` · 9 | 20501 | 원본 = 1차 기계(gallery `fe3ad599`) · 마스크 (56,168,76,44) | `A fully closed flat wooden drawer, front panel flush with the cream and pink machine body, small pink handle in the middle, no opening, no gap …` — 오른쪽에 세로 줄 하나가 남았다 |
| **`machine_closed_20502`** (쉼 · 씀) | `inpaint_image_pro_flash` · 9 | 20502 | 원본 = 20501 · 마스크 (124,164,16,48) | `Right end of the closed wooden drawer front, then the soft pink lower band … continue smoothly. No vertical line, no gap.` |
| **`run_20801/f0~f8`** (도는 중 · 씀) | `animate_image` 8 프레임 · 7 | 20801 | first_frame = `machine_closed_20502` | `machine working in a loop: the small pink crank lever on the right side spins around in a full circle, the round body gently rattles and bounces up and down a little, tiny white steam puffs pop out from the top. The machine stays in place, same size, the window glass stays plain grey, the drawer stays closed.` |
| **`machine_done_20802`** (다 됨 · 씀) | `edit_image_pro_flash` · 9 | 20802 | 원본 = `machine_closed_20502` | `Same machine, finished state: the wooden drawer at the bottom is pulled open toward the viewer (empty inside …), with a few small yellow and white sparkle stars … Keep everything else exactly the same …` |
| **`stand_sign_20601`** (살 칸 · 씀) | `create_image_pro_flash` 224×240 · 9 | 20601 | style = 1차 기계 | `An empty dark wooden low platform base, the same base the machine … stands on … one cute wooden signboard on a short thick wooden post: a wide rounded-corner board with a soft pink frame and a blank plain cream face … no text` |
| **`covered_20602`** (잠김 · 씀) | `create_image_pro_flash` 224×240 · 9 | 20602 | style = 1차 기계 | `The same cute rice-cake machine … completely hidden under a soft cream cloth sheet draped over it … tied around the middle with a brown rope and a small brass padlock … The dark wooden base peeks out at the bottom.` |
| **`plank_20901`** (나무 표 · 씀) | `create_image_pro_flash` 160×40 · 6 | 20901 | 기계 받침 40×36 조각 | `one blank wide wooden plank label board … rounded-corner warm brown wooden plaque with a darker wooden frame and two tiny brass nails at the ends, the face is plain and empty …` |

마루는 1차 `floor_20203` 그대로다.

## 손질 (PIL — 자르기 · 줄 잇기 · 배치 · 프레임 잇기만)

- 배경 a : 천장 `top_20404` 0~211 줄 + 몰딩 338~359 줄 + 벽 `wall_20401` 25~433 줄(맨 위 몰딩 버림 · 걸레받이 끝 검은 줄까지) + 마루 0~398 · 26~183 줄.
- 배경 b : 천장 0~245 줄(천장 몰딩은 뺐다) + 벽 `wall_20402` 43~439 줄. 벽 b 는 위 0~42 줄이 흰 띠로 와서 버리고 **벽 b 자신의 몰딩**을 이음 줄로 썼다. 흰 창 속은 inpaint 조각을 (188, 100) 에 붙였다.
- 기계 조각 : raw 224×240 에서 (8, 9)~(216, 229) 를 잘랐다. 살 칸 · 잠김은 그림 칸 가로 가운데를 x 112 에, 아래끝을 발 줄(raw y 209)에 맞춰 옮긴 뒤 같은 칸으로 잘랐다.
- 도는 중 : ① 프레임마다 발 줄을 쉬는 그림에 맞춰 옮겼다 ② **창 속 원(raw 가운데 (105,126) · 반지름 30)을 쉬는 그림 것으로 덮었다.** animate_image 가 창 속에 김 덩이를 그려 넣어서다. f8 은 f0 과 거의 같아 뺐다.

## 깨끗하지 않은 곳

- 도는 프레임 2 · 4 의 창 테 가장자리에 반짝이 점 한두 개가 남는다 (덮은 원 바깥).
- 도는 동작은 손잡이 돌기 · 몸 덜컹 · 김 방울이다. 크게 움직이지 않는다 — 음양 회전과 함께 봐야 「도는」 느낌이 난다.
- 다 됨 그림은 17색으로 다른 프레임보다 색이 적다. 몸 색이 조금 평평하다.
- 벽 a 는 9:16 에서 위쪽 배관이 화면 위끝에서 잘려 보인다. 골드 표가 왼쪽 위 밸브 · 계기판을 가린다.
- 벽 b 의 창 속은 inpaint 조각이라 둘레 1px 이 조금 다르다.
- 마루는 1차 그대로 (벽돌 쌓듯 규칙적 · 원근 없음).
- 기계 선(짙은 갈색)이 찹쌀이 선보다 짙다 (1차와 같다).

## 쓴 크레딧 (2차 · 합성소 몫)

벽 40 × 2 + 천장 40 + 창 6 + 서랍 9 × 2 + 도는 동작 7 + 다 됨 9 + 살 칸 9 + 잠김 9 + 나무 표 6 = **184회**.

## 1차 기록 (v1 · 2026-10-04 오전)

- 배경 `out/synthesis_bg_v1.png`(정리함 2026-10-18) : 천장 `top_20202` 0~245 · 344~359 줄 + 벽 `wall_20201` 41~421 줄 + 마루 `floor_20203` 0~398 · 26~183 줄. 둥근 창 · 병 선반 · 쌀자루만 있는 빈 방 → **사용자 판정 「창고 같다」**.
- 기계 `raw/synthesis_machine_v1_out.png` (177×194) : `machine_20301` (pro_flash 224×240 · 씨앗 20301, style = `정식/메인/out/synthesis_main.png`) 의 (24,17)~(200,210). 서랍이 살짝 열린 채 왔다.
- 1차 크레딧 129회 (배경 3 × 40 + 기계 9).
