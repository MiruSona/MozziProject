# 주방 정식 그림 v3 (반복 07)

> **2026-10-18 정리** : 이 문서가 가리키던 그림 중 안 쓰는 시안·견줌판·옛 판은 저장소에서 지웠다 (`(정리함 2026-10-18)` 표시). 다시 보려면 git 기록에서 꺼낸다.

2026-10-05 에 고른 시안을 바탕으로 주방 그림을 정식으로 뽑았다.
고른 시안은 서랍장 C · 찜기 B · 절구+떡메 B · 빚기판 A · 그릇 B 다 (`Docs/Reference/Art/시안/주방기구-v1/`).
시안의 모양은 그대로 두고 크기와 색 수만 바로잡았다.

- `raw/` : PixelLab 에서 받은 그대로의 그림. 손대지 않는다.
- `work/bg_fixed.png` : 배경을 고친 중간 판이다. 옹이를 지운 6판을 합치고 쟁반 자리를 메웠다.
- `out/` : 게임에 넣을 그림이다. `build_out.py` 가 `raw/` 에서 다시 만든다.
- `견줌/` : 눈으로 보는 판이다. `build_compare.py` 가 만든다.

다시 만들 때는 저장소 뿌리에서 아래 두 줄을 돌린다.
`python "Docs/Reference/Art/정식/주방/v3/build_out.py"`
`python "Docs/Reference/Art/정식/주방/v3/build_compare.py"`

PIL 로는 자르기 · 줄이나 칸 되풀이와 빼기 · 배치 · 바탕 지우기 · 한 색 바꾸기만 했다. 새로 그린 점은 없다.
(예외 하나 — 쟁반 자리는 아래 10번 참고)

## 그림마다 어떻게 만들었나

크레딧(generations)은 PixelLab 이 판마다 알려 준 값이다. 시작 잔액 1737 에서 끝 잔액은 1573 이라 **모두 164** 를 썼다.

| # | out 파일 | 도구 · 판 | 프롬프트 줄기 | seed | job id | 크레딧 | 손질 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `kitchen_drawer_frame.png` 238×284 | create_image_pro_flash 240×256 (시안 C 를 화풍 그림으로) → inpaint_image_pro_flash 로 안판 채우기 | 감나무 붉은 나무 틀 · 놋쇠 모서리 → 「plain very dark warm brown back board」 | 30751 → 30753 | 6a2816b3-64ad-46e0-b990-95c7bac4b5e4 → 6f862406-d221-4883-866d-add1a0c3ef7c | 9 + 9 | 기둥 · 띠의 같은 색 줄을 빼서 테두리를 10px 로 맞췄다. 안판은 같은 구간을 되풀이해 늘렸다. 다리 15줄은 뺐다. 48색 |
| 2 | `kitchen_drawer_cell.png` 50×80 | create_image_pro_flash 52×80 | 서랍 앞판 + 위쪽 놋쇠 손잡이 | 30752 | 9747de87-4308-4f71-9b45-7f8c2d7caddc | 5 | 38×63 으로 작게 나왔다. 손잡이 양옆 판 줄과 손잡이 아래 판 줄을 되풀이해 50×80 으로 늘렸다. 판색 14개를 한 색씩 밝은 저채도로 바꿨다 (원색 42% + #F2E6D6 58%). 외곽선과 놋쇠는 그대로다. 16색. (edit 74eb1362… 5 크레딧은 손잡이만 남아서 안 썼다) |
| 3 | `kitchen_bowl.png` 110×36 | 시안 그릇 B 를 그대로 썼다 | — | — | 59721d09-f8dc-4b57-96eb-a3e4b524a965 (시안) | 0 | 106×32 를 110×36 가운데 아래에 놓았다. 45색 |
| 4 | `kitchen_steamer.png` 150×140 | create_image_pro_flash 156×148, 찜기 B 를 화풍 그림으로 | 통 시루 + 가마솥 · 「WHOLE object visible, 6px margin」 | 30713 | 7202cedd-004a-4eed-bd7c-378b8528a537 | 6 | 아래 가운데에 놓았다. 48색으로 줄였다. (첫 판 a714023f… 6 크레딧은 왼쪽과 아래가 잘려서 버렸다) |
| 5 | `kitchen_steam_0~3.png` 64×64 | create_image_pro_flash 64×64 (청록 바탕) → animate_image 4장 | 흰 김 줄기 → 「steam wisps rising and swaying」 | 30761 → 30762 | 117f82e0-df90-4899-93d7-2d2a79c4629b → 4727891b-8290-49c1-baf1-4ff790e6290f | 5 + 1 | 바탕을 지우고 흰색 한 색으로 바꿨다. 입력 그림과 생성 3장을 0~3 으로 썼다 |
| 6~7 | `kitchen_pound_0~6.png` (쌀) · `kitchen_pound_rough_0~6.png` (거친 반죽) · `kitchen_pound_smooth_0~6.png` (매끈한 떡) 212×241 | 단계 그림 : 시안 절구 B(7f3dade0…) 위의 쌀 자리만 inpaint 해서 둘을 더 만들었다 → 단계마다 animate_image 6장 (첫 장과 끝 장은 쉬는 모습으로 고정) | 「rough half-pounded rice dough … visible grains」 · 「smooth glossy pounded tteok mound」 → 「mallet lifts, swings high, slams down, small bounce, settles」 | 30773 · 30774 → 30781 | 쌀 : f5b8a391-009d-4d10-9591-12342d1d9bc8 → cc31950f-0e7d-4d81-8bfd-7792072db76b · 거친 반죽 : (시안 그대로) → 75a11119-e6f5-4a66-96fe-ec14e420f71d · 매끈 : 893dbd00-449b-4d5d-b952-73f7350e382a → a74221ff-9d56-49d8-992c-99663f8aad01 | 9 + 9 + 5×3 | 청록 바탕을 지웠다 (파랑이 빨강보다 20 넘게 크면 지움). 뽑힌 순서 [0,2,3,4,5,6,1] 로 다시 늘어놓아 4번이 떡을 때리는 장이 되게 했다. 절구 밑바닥은 맨 아랫줄(240)에, 몸통 가운데는 x79 에 맞췄다. 36~37색 |
| 8 | `kitchen_board.png` 150×80 | create_image_pro_flash 152×80, 빚기판 A 를 화풍 그림으로 | 낮은 나무 판 (다리 달린 작은 상) | 30721 | 8f582771-13aa-45d4-994b-236c010b48c9 | 6 | 146×76 을 아래 가운데에 놓았다. 29색 |
| 9 | `kitchen_shape_0~2.png` 64×48 | 덩이는 create_image_pro_flash, 나머지 둘은 edit_image_pro_flash | 덩이 → 「pressed, smoother oval mound」 → 「neat smooth round tteok」 | 30771 · 30776 · 30775 | 60e2942e-86b3-47d6-871a-934b552c191b · b7f7f8e1-3545-4ead-ad25-36ac8705ed95 · fc05d3c6-7eeb-4add-9482-b08e35360cde | 5 + 5 + 5 | 바탕을 지우고 16색으로 줄였다. (animate 33844e0a… 1 크레딧은 떡이 덩이에서 튀어나오는 이상한 모양이라 버렸다) |
| 10 | `kitchen_bg.png` 540×1200 | inpaint_image_pro_flash 256×256 6판 (바닥 옹이) | 「plain warm honey-orange wooden floor planks … no knots」 | 30782~30786 | f00 0e495916-e11a-4b20-9c79-48773600dc32 · f01 a9dc52e4-d4bb-4d82-b57a-2ee012076ae4 · m1 a6508a35-f0bc-4623-a074-761479813e9e · m2 8517d740-0ae0-40f9-9928-719ea49ae710 · f10 fae8559d-00f6-4b98-91fe-bfdd5f05a89c · f11 919706fa-a8f4-4785-9bfc-e7952a5f7be1 | 9×6 | 판마다 마스크 안쪽 점만 붙였다. 마스크 밖이 그대로인지 6판 모두 재 봤다 (바뀐 점 0). **쟁반**은 inpaint(8d8211a8…, seed 30790, 9 크레딧)를 했지만 나무토막 같은 덩어리로 채워져서 버렸다. 그 대신 쟁반 바로 위 벽 줄(y529)을 아래로 되풀이하고, 탁자 윗면의 두 색(외곽선 #330E1E · 윗면 #F5C8A6)을 이어 x245~520 · y530~569 를 메웠다. 그림에 원래 있던 줄과 색만 썼지만 **「PixelLab inpaint 로 지운다」는 지시와 다르다.** 화면 끝에 반쯤 잘린 옹이 둘(왼쪽 x0~5 · y770~800, 오른쪽 x526~539 · y912~936, 41점)도 같은 줄의 옆 칸 색을 되풀이해 지웠다. 62색 |

다시 쓴 것(새로 안 뽑음) : `kitchen_plate` · 재료 아이콘 · `ui_button`.

## 떡 단계를 어떻게 나눴나 — 단계마다 7장을 따로

- 떡메가 절구 테두리 **안으로** 들어가서 떡을 가린다. 그래서 떡을 따로 겹으로 떼면 떡메 앞뒤 순서가 장마다 바뀌어 맞지 않는다.
- 그래서 **단계마다 7장을 통째로** 만들었다. 코드는 단계가 바뀔 때 프레임 배열만 바꿔 끼우면 된다.
- 단계 셋은 다음과 같다.
  - 쌀 (`kitchen_pound_`) : 낟알이 보인다. 지금 코드가 부르는 이름 그대로라서 코드를 안 고쳐도 돌아간다.
  - 거친 반죽 (`_rough_`) : 납작한 흰 덩이다. 시안 그대로다.
  - 매끈한 떡 (`_smooth_`) : 봉긋하고 윤이 난다.
- 처음엔 시안의 흰 덩이를 「쌀」로 쓰려 했다. 그런데 inpaint 로 만든 「거친 반죽」이 오히려 낟알처럼 보여서 둘의 자리를 바꿨다.

## 검사

`arttool check` 결과는 `Docs/Reference/Art/점검/2026-10-05/반복07/주방정식/` 에 있다.

| 묶음 | 프로필 | 파일 | status | must_failed | 경고 |
| --- | --- | --- | --- | --- | --- |
| 소품 | mozzi_prop | 29장 (서랍 틀 · 칸 · 그릇 · 찜기 · 떡메 21 · 판 · 빚기 3) | ok | 없음 | outline 24장 (selout+light / mixed 판정 — 외곽선이 검정은 아니라 reline 은 안 했다) · integer_scale 2장 (서랍 틀 · shape_1, 부드러움 0.18 > 0.15) · isolated 2장 (shape_0 · shape_1) · near_colors 1장 (서랍 틀 65쌍) · ramp_shape (프로필 램프 경고, 그림과 무관) |
| 배경 | mozzi_bg | kitchen_bg | ok | 없음 | ramp_shape (프로필 램프 경고) |
| 흰 이펙트 | mozzi_fx | kitchen_steam_0~3 | ok | 없음 | ramp_shape (프로필 램프 경고) |

색 수 : 서랍 틀 48 · 칸 16 · 그릇 46 · 찜기 48 · 떡메 36~37 · 판 29 · 빚기 16 · 김 1 · 배경 62 (모두 투명을 뺀 값이고, 한도 안이다).

## 코드에 넘길 것

| 무엇 | Resources 자리 | 크기 | 피벗 · 겹 | 프레임 · 시간 | 코드 쪽 할 일 |
| --- | --- | --- | --- | --- | --- |
| 배경 | `Art/Screen/kitchen_bg` | 540×1200 | 맨 뒤 | — | 덮어쓰기만 하면 된다 |
| 서랍장 틀 | `Art/Screen/kitchen_drawer_frame` | 238×284 | 왼쪽 위 (292,470), 배경 바로 위 | — | 지금은 상자 그림이다 → 이 그림으로 바꾼다. 9-slice 없이 Simple 로 쓴다 |
| 서랍 칸 | `Art/Screen/kitchen_drawer_cell` | 50×80 | 칸 자리 그대로, 틀 위 | — | `MakeSliced(ui_button)` → 이 그림을 Simple 로 바꾼다 (크기가 딱 맞아 9-slice 가 필요 없다). 색 곱하기(tint)는 그대로 둔다. 아이콘은 y30, 숫자는 y62 자리라 판이 비어 있다 |
| 그릇 | `Art/Screen/kitchen_bowl` | 110×36 | (240,778) | — | sliced 상자 → Simple 그림. 칸 28 · 사이 4 그대로 |
| 찜기 | `Art/Screen/kitchen_steamer` | 150×140 | (30,900)~(180,1040) | — | MakeToolBox → 그림 |
| 김 | `Art/Fx/kitchen_steam_0~3` | 64×64 흰 한 색 | 찜기 위 겹. 견줌판에서는 (73,842) 에 알파 200 으로 놓았다 (뚜껑 손잡이 바로 위) | 4장 되풀이 · 견줌 GIF 는 장마다 0.15초 | 새 연출. 색은 코드에서 곱한다 |
| 떡메 (쌀) | `Art/Screen/kitchen_pound_0~6` | 212×241 | 피벗 (79/212, 0) 그대로 | `PoundFrameSec` · `PoundHitFrame=4` 그대로 | 덮어쓰기만 하면 된다 |
| 떡메 (거친 반죽 · 매끈) | `Art/Screen/kitchen_pound_rough_0~6` · `kitchen_pound_smooth_0~6` | 212×241 | 같다 | 같다 | `SpriteKeys` 에 접두어 둘을 더한다. `KitchenMortarView` 가 단계마다 `frames` 배열을 바꿔 끼우게 한다 (치기 횟수로 쌀 → 거친 → 매끈) |
| 빚기판 | `Art/Screen/kitchen_board` | 150×80 | (370,960) | — | MakeToolBox → 그림. 판은 다리 달린 작은 상이라 **윗면이 BoardTop 보다 아래(+4~+18줄)에 있다** → `PlateY = BoardTop - 2` 를 **`BoardTop + 14`** 로 바꾼다 (`ResultIconBottom = PlateY - 23` 는 따라 내려간다) |
| 빚기 | `Art/Screen/kitchen_shape_0~2` | 64×48 | 아래 가운데 (32,42) 를 (PlateX, PlateY) 에 | 3장, 견줌 GIF 는 0.25 · 0.25 · 0.6초 | 새 연출 |
| 옮겨 넣기 | `Editor/FormalArtCopy.cs` | — | — | — | 지금 줄은 `주방/out/…` 을 가리킨다 → `주방/v3/out/…` 으로 바꾸고 새 파일 줄을 더한다 |
| MozziArtImport | — | — | — | — | 더할 줄 없음. 칸 · 그릇 모두 쓸 크기 그대로라 9-slice 가 필요 없다. 떡메 피벗은 코드(`KitchenScreenSetup`)에서 준다 |
| check_all.py | `Docs/Reference/Art/화풍/check_all.py` | — | — | — | 78~79줄 `("bldg","max_colors"/"color_cap","kitchen_pound_*.png", …)` KNOWN 두 줄을 지운다 (새 떡메는 37색). 새 파일(`kitchen_drawer_*` · `kitchen_bowl` · `kitchen_steamer` · `kitchen_board` · `kitchen_shape_*` · `kitchen_pound_rough/smooth_*` · `kitchen_steam_*`)의 프로필 갈래를 더한다 (김은 mozzi_fx) |

## 볼 파일 + 판단할 점

| 볼 파일 (`견줌/`) | 판단할 점 |
| --- | --- |
| `주방_전체_1x.png` · `주방_전체_2x.png`(정리함 2026-10-18) · `주방_작업대_2x.png`(정리함 2026-10-18) | 서랍장 · 그릇 · 찜기 · 절구 · 판이 한 화면에서 같은 그림체로 보이나. 칸 색 곱하기 뒤에도 아이콘 · 숫자가 잘 읽히나 |
| `주방_작업대_치기빚기중_2x.png`(정리함 2026-10-18) | 빚는 떡이 판 윗면에 앉아 보이나 (PlateY 를 +14 로 옮긴 자리) |
| `서랍장틀_2x.png`(정리함 2026-10-18) · `시안_정식_2x.png`(정리함 2026-10-18) | 테두리를 10px 로 줄이고 다리를 뺀 서랍장이 시안 C 와 같아 보이나. 다리가 없어 벽에 건 장처럼 보이는데 괜찮나 |
| `떡메_rice_2x.gif`(정리함 2026-10-18) · `떡메_rough_2x.gif`(정리함 2026-10-18) · `떡메_smooth_2x.gif`(정리함 2026-10-18) (+ `_띠.png`) | 떡메가 떠 보이지 않나. 4번 장(0.12초)이 「때림」으로 읽히나. 마지막 장이 살짝 튀었다 0번으로 돌아가는 것이 어색하지 않나 |
| 같은 GIF 셋 | 쌀 → 거친 반죽 → 매끈한 떡 차이가 한눈에 보이나 (거친 반죽과 매끈한 떡이 비슷하면 다시 뽑을 후보) |
| `김_3x.gif`(정리함 2026-10-18) · `김_띠_3x.png`(정리함 2026-10-18) | 김 4장이 되풀이될 때 끊겨 보이나 (뒤로 갈수록 옅어지는 판이라 3→0 에서 튈 수 있다) |
| `빚기_3x.gif`(정리함 2026-10-18) · `빚기_띠_3x.png`(정리함 2026-10-18) | 덩이 → 누른 덩이 → 둥근 떡으로 읽히나 |
| `배경_옛판_새판.png`(정리함 2026-10-18) | 바닥 옹이가 다 지워졌나. 옹이가 사라져 바닥이 너무 밋밋하지 않나. 쟁반 자리(서랍장 왼쪽 x245~291 만 보인다)가 자연스럽나 |
| `소품_2x.png`(정리함 2026-10-18) | 칸 판색이 너무 하얗거나 탁하지 않나 |
