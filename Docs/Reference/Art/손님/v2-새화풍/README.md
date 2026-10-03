# 손님 새 화풍 — 파츠 세트 초안 (2026-10-03 · 반복 02)

> **시안이다. 게임에는 아직 안 넣었다.** 사용자가 보고 정한 뒤 코드 갈래가 배선한다.
> 가판대 주인(64×80 · 2.5등신)을 세웠더니 지금 손님(48×64 · 점 눈 · 단색)이 너무 단순해 보여서, 주인과 같은 화풍·등신으로 다시 그렸다.

## 결론

- **PixelLab 으로 맨 기본체 1장을 뽑고, 그 기본체에 `inpaint_image` 로 머리·옷·표정만 입힌 판을 뽑은 뒤, 기본체와 달라진 칸만 떼어 겹으로 만들었다.**
  inpaint 는 마스크 밖을 한 픽셀도 안 바꾸므로 모든 겹이 같은 캔버스·같은 자리에 딱 맞는다.
- 그림은 전부 PixelLab 픽셀이다. PIL 은 떼어 내기 · 색 단 정리 · 색 갈아 끼우기 · 외딴 점 지우기만 했다.
- 겹 46장 = 몸 1 + 표정 3 + 머리 6모양×4색 + 윗옷 3모양×4색 + 아래옷 2모양×3색. 섞으면 **1,728가지** 겉꼴이 나온다 (표정 빼고).

## 겹 규격

| 항목 | 값 |
| --- | --- |
| 캔버스 | **64×80** (모든 겹 같음 · 투명 배경 · 알파는 0 과 255 뿐) |
| 기준점 | 가로 가운데 x=32 · **발끝 y=76** (그림이 차 있는 마지막 줄. 주인 B안도 76) |
| 몸이 차지하는 자리 | x 16~47 · y 5~74. 가장 넓은 머리(hair_05)는 x 7~56 |
| 겹 순서 (아래 → 위) | `body` → `bottom` → `shirt` → `face` → `hair` |
| 얼굴 칸 | x 21~42 · y 23~34 — 표정 겹이 통째로 덮는 자리. 머리 겹은 이 칸을 안 건드린다 |

- 순서가 지금 게임(몸·머리·윗옷·표정)과 다르다. **머리가 맨 위**여야 앞머리가 눈썹을 덮고, 긴 머리가 어깨 옷 위로 내려온다.
- `body` 는 흰 반팔·회색 반바지를 입은 대머리 기본체다. 윗옷·아래옷·머리 겹을 **늘 하나씩 얹는 것**이 전제다 (안 얹으면 기본체가 그대로 보인다).

## 파츠 목록

| 겹 | 파일 이름 | 가짓수 | 무엇 |
| --- | --- | --- | --- |
| 몸 | `body` | 1 | 기본체 |
| 표정 | `face_01` · `face_02` · `face_03` | 3 | 기본 · 웃음(눈 감고 입 벌림) · 시무룩(처진 눈 + 눈물 + 처진 입 + 눈썹) |
| 머리 | `hair_<모양 01~06>_<색>` | 6 × 4 | 01 삐죽 단발 · 02 똑단발 · 03 긴 생머리 · 04 양갈래 땋은 머리 · 05 뽀글 파마 · 06 짧은 가르마 |
| 머리색 | `black` · `brown` · `orange` · `gray` | 4 | 지금 게임의 `purple` 대신 `gray` (어르신) |
| 윗옷 | `shirt_<모양 01~03>_<색>` | 3 × 4 | 01 후드 · 02 반팔 · 03 가디건(안에 흰 셔츠) |
| 윗옷색 | `red` · `green` · `blue` · `yellow` | 4 | 지금 게임과 같은 이름 |
| 아래옷 | `bottom_<모양 01~02>_<색>` | 2 × 3 | 01 긴 바지 + 흰 운동화 · 02 주름 치마 + 흰 양말 + 갈색 구두 |
| 아래옷색 | `navy` · `beige` · `gray` | 3 | |

## 폴더

| 자리 | 무엇 |
| --- | --- |
| `raw/` | PixelLab 이 낸 그대로 (기본체 · 파츠 입힌 판 14장 · 마스크 6장 · 마스크 만든 `masks.py`) |
| `src/` | 떼어 낸 겹의 **기준색 한 벌** — 색 바꿀 자리가 파랑 6단으로 정리돼 있다 |
| `layers/` | 색을 구운 겹 46장 (게임에 넣을 때 이것을 옮긴다) |
| `ref/owner_B.png` | 견줄 것 : 가판대 주인 1차 B안 64×80 |
| `bake_parts.py` | `raw/` → `src/` · `layers/` 굽기 + 검사. `python bake_parts.py` |
| `make_previews.py` | `Docs/Reference/Art/시안/` 의 시안 석 장 만들기 (지금 석 장은 `시안/지난판/손님/` 으로 옮겨 두었다) |
| `bake_log.txt` | 마지막으로 구운 판의 검사 출력 |

## 색 — 6단 램프

기준색(파랑)은 `outline · deep · shade · base · light · shine` 여섯 단이다. 굽는 스크립트가 이 여섯 색을 표의 색으로 1:1 로 갈아 끼운다.
살색 · 흰 깃 · 흰 양말 · 신발처럼 파랑이 아닌 칸은 그대로 둔다. 새 색은 `bake_parts.py` 의 `*_COLORS` 에 기본색 한 줄만 더하면 된다.

| 단 | outline | deep | shade | base | light | shine |
| --- | --- | --- | --- | --- | --- | --- |
| 기준색 | `#0A0C28` | `#1A2C68` | `#28428C` | `#365EBA` | `#6E8CE1` | `#AAC3FA` |
| 머리 black | `#0F0E11` | `#201D23` | `#2D2A32` | `#3A3640` | `#625D6A` | `#9994A0` |
| 머리 brown | `#21140C` | `#452B19` | `#623E26` | `#7E5234` | `#9A6F52` | `#BEA18C` |
| 머리 orange | `#341E0E` | `#6E3F1D` | `#9C5C2E` | `#C87A42` | `#D49262` | `#E3BC9F` |
| 머리 gray | `#28282A` | `#555459` | `#79787E` | `#9C9AA2` | `#B1AFB6` | `#CDCCD0` |
| 윗옷 red | `#35100D` | `#71211B` | `#A0352C` | `#CD4A40` | `#D86A61` | `#E6A49F` |
| 윗옷 green | `#172915` | `#31572D` | `#487B42` | `#5F9E58` | `#7AB374` | `#A9CEA5` |
| 윗옷 blue | `#101F31` | `#224269` | `#356094` | `#4A7EBE` | `#6995CC` | `#A1BDDE` |
| 윗옷 yellow | `#3B3014` | `#7D652B` | `#B19142` | `#E3BC5C` | `#E9C97A` | `#F1DEB1` |
| 아래옷 navy | `#0F121D` | `#1F273E` | `#2E3857` | `#3E4A70` | `#5C698F` | `#939BB8` |
| 아래옷 beige | `#322A1E` | `#6B593F` | `#977F5C` | `#C2A47A` | `#CFB692` | `#E0D1BB` |
| 아래옷 gray | `#1C1D20` | `#3C3D43` | `#55575F` | `#6E707A` | `#8B8D97` | `#B4B6BC` |

- 단 나누기 : 그 겹에서 가장 많은 파랑을 base 로 보고, 밝기 비율 0.30 · 0.62 · 0.88 · 1.15 · 1.70 에서 자른다.
- 머리·윗옷은 파랑이 아닌 어두운 선도 outline 단으로 본다 (후드 외곽선이 갈색으로 나왔다). 아래옷은 안 그런다 (갈색 구두가 물든다).

## 만든 법

1. **기본체** — `create_image_pixflux` 64×80 · 씨앗 **5101** · `view=side` · `direction=south` · `outline=single color black outline` · `shading=basic shading` · `no_background=true`.
   글머리 : 「cute chibi person base mannequin, full body, 2.5 heads tall chibi, completely bald smooth head with no hair, big round brown anime eyes, small mouth, pink blush cheeks, plain white t-shirt, plain gray shorts, simple shoes, standing straight facing front, arms relaxed at sides, symmetrical pose」.
2. **마스크** (`raw/masks.py`) — 흰 칸만 다시 그린다.
   - 머리 : y 0~56 · x 4~59 에서 얼굴 칸 · 목 · 윗옷 자리를 뺀 것. 긴 머리(03 · 04)는 어깨 위(y 37~58 · x 11~22 와 41~52)를 더한 `mask_hairlong`.
   - 윗옷 : y 37~56 · x 15~48 + 손 자리. 아래옷 : y 57~76 다리 자리. 얼굴 : 얼굴 칸. 시무룩 눈썹·입 : `mask_sadB`.
3. **파츠 입히기** — `inpaint_image(image_url=기본체, mask_image_base64=마스크, description=…, seed=…)`. 색 바꿀 파츠는 전부 **「royal blue」** 로 시켰다 (살색과 색상이 멀어서 가르기 쉽다).

| 파츠 | 씨앗 | 글머리 (요지) |
| --- | --- | --- |
| hair_01 | 6111 | boy short messy royal blue hair with uneven spiky bangs |
| hair_02 | 6112 | girl royal blue bob haircut, chin length, soft uneven bangs |
| hair_03 | 6123 | long straight royal blue hair flowing down over both shoulders to the waist, side-swept bangs, white t-shirt |
| hair_04 | 6124 | royal blue hair tied in two low braided pigtails resting on the front of both shoulders, white t-shirt |
| hair_05 | 6115 | middle-aged auntie with short curly permed royal blue hair |
| hair_06 | 6116 | man with neat short royal blue hair, side parted, ears visible |
| shirt_01 | 6201 | cozy royal blue hoodie with long sleeves, front pocket and drawstrings, skin colored hands visible |
| shirt_02 | 6202 | royal blue short sleeve t-shirt with a round collar, bare skin colored arms |
| shirt_03 | 6203 | royal blue knit cardigan, buttons, open over a white collared shirt |
| bottom_01 | 6401 | long royal blue trousers down to the ankles and white sneakers |
| bottom_02 | 6402 | royal blue pleated knee-length skirt, bare legs, white socks and brown shoes |
| face_02 | 6311 | happy face, eyes closed in joy drawn as thick dark upward curved arcs, wide open smiling mouth |
| face_03 눈 | 6321 | sad disappointed face, drooping eyelids, small blue teardrop, downturned mouth |
| face_03 눈썹·입 | 6322 | slanted worried eyebrows, small downturned frowning mouth (`mask_sadB`) |

   머리 글머리 끝에는 공통으로 「glossy hair highlight and darker shade strands, dark outline, pixel art」, 옷에는 「cloth fold shading, dark outline, pixel art」 를 붙였다.
4. **떼어 내기** (`bake_parts.py`) — 기본체와 달라진 칸만 남긴다 → 파랑을 6단으로 정리 → 색 굽기.
   - 표정 겹은 얼굴 칸을 살색까지 통째로 가져온다 (구멍이 안 난다). `face_01` 은 기본체의 눈을 그대로 뗀 것이다.
   - 긴 머리는 어깨 줄(y 37) 아래에서 파란 칸과 어두운 선만 남긴다 — 같이 다시 그려진 흰 옷·팔을 버리려는 것이다.
   - PixelLab 이 파츠를 입히며 기본체 윤곽을 몇 칸 깎는다(정수리 한 줄 등). 「어느 파츠를 얹어도 가려지거나 깎이는 칸」 32개를 몸에서 뺐다.

## 통한 길 · 막힌 길

| 길 | 결과 |
| --- | --- |
| `inpaint_image` + 내 마스크 + 기본체와 diff | **통함.** 마스크 밖 픽셀 차이 0. 한 장 20 생성 |
| 머리 마스크에서 얼굴 칸을 네모로 빼기 | 첫 판(y 22~35 · x 19~44)은 앞머리와 단발 밑단이 **일자로 잘렸다.** 칸을 눈·입만(y 23~34 · x 21~42)으로 줄이니 나아졌다 |
| 긴 머리를 윗옷 자리와 안 겹치게 뽑기 | **막힘.** 머리가 어깨에서 끊기고 팔 옆에 가는 줄만 남았다. 어깨 위를 마스크에 넣고 머리 겹을 윗옷 위에 얹는 쪽으로 바꿨다 |
| 얼굴 칸을 inpaint 로 「더 큰 반짝이 눈」으로 바꾸기 (씨앗 6301 · 6302) | **버림.** 눈이 흐릿한 갈색으로 나와 기본체의 눈(짙은 윗눈꺼풀)보다 약했다. 기본 눈은 기본체 것을 쓴다 |
| 갈색 머리 · 빨간 후드로 바로 뽑기 (씨앗 6101 · 6201) | 그림은 좋았지만 갈색이 살색과 색상이 가까워 가르기 어렵다 → 파랑으로 다시 뽑았다 |
| 완성 손님을 통째로 뽑아 색으로 가르기 (씨앗 4201 한 장) | 안 씀. 기본체가 손님마다 달라 섞을 수 없다 |

## 깨끗하지 않은 곳 (숨기지 않고 적는다)

| 어디 | 무엇 |
| --- | --- |
| `hair_04` (양갈래) | 팔 바깥선 8칸(x 18 · 19 · 44 · 45, y 51~55)을 PixelLab 이 깎았는데 다른 머리에서는 필요한 칸이라 몸에 남겼다. 반팔과 같이 쓰면 땋은 머리 옆으로 팔 선이 1px 보인다 |
| `hair_05` (파마) | 옆머리가 어깨 줄(y 37)에서 **일자로 끝난다** — 마스크 끝이다. 어깨 마스크를 넣어 다시 뽑으면 나아진다 |
| `hair_02` (똑단발) | 앞머리가 얼굴 칸 윗줄(y 22)에서 일자다. 똑단발이라 어색하지는 않다 |
| `face_03` | 눈은 6321 판, 눈썹·입은 6322 판을 합친 것이라 눈 색이 기본보다 조금 밝다. 눈썹은 앞머리가 있는 머리(01~05)에서는 가려진다 |
| 긴 머리 + 후드 | 머리가 후드 소매를 덮어 손이 안 보인다. 틀린 것은 아니지만 팔이 없는 것처럼 보일 수 있다 |
| 치마 밑단 | 검은 선이 색을 바꿔도 검정이다 (아래옷은 어두운 선을 outline 단으로 안 본다) |
| 나이·성별 | 기본체가 하나라 얼굴·체형은 다 같다. 머리 모양·색(파마 · gray)으로만 나이를 낸다 |
| 소품(모자 · 안경) | **안 만들었다** |

## 검사 (bake_log.txt)

- 되돌림 : 몸 + 겹 하나를 PixelLab 원본과 견줘 「남는 칸(삐져나옴)」은 hair_04 의 8칸뿐, 나머지 0.
- 「모자란 칸」 hair_02 6 · hair_03 8 · hair_04 4 는 일부러 버린 칸(흰 옷 조각 · 외딴 점)이다. 윗옷 겹이 그 자리를 덮는다.
- 눈으로 본 조합 : 시안 12명 + 머리 4 × 윗옷 3 = 12 조합. 1,728가지를 다 보지는 않았다.
