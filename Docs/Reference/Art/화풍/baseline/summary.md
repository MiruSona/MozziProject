# 그림 전체 검사 요약 — 개편 전 기준선 (2026-10-05)

`check_all.py` 가 낸 표다. 그림 111장 · 묶음 10개. 숫자는 **걸린 그림 장 수**다.

## 묶음별

| 묶음 | 프로필 | 장 | 판정 | 실패 규칙 | `color_cap` | `integer_scale` | `isolated` | `near_colors` | `outline` | 경고 합 | 램프 밖 색 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| bg | `mozzi_bg` | 5 | ok | — | — | — | — | — | — | 0 | 185 |
| bldg | `mozzi_prop` | 22 | fail | max_colors | 7 | — | — | — | 4 | 11 | 254 |
| char_mozzi | `mozzi_char` | 17 | fail | max_colors | — | — | — | 1 | 7 | 8 | 56 |
| char_people | `mozzi_people` | 17 | ok | — | 1 | — | — | — | 17 | 18 | 145 |
| fx | `mozzi_fx` | 3 | ok | — | — | — | — | — | — | 0 | 1 |
| icon | `mozzi_icon` | 17 | ok | — | — | — | — | — | 16 | 16 | 25 |
| piece | `mozzi_prop` | 9 | ok | — | 5 | — | 3 | 1 | 4 | 13 | 291 |
| tile | `mozzi_prop` | 14 | ok | — | 6 | 3 | 8 | — | 5 | 22 | 56 |
| tile_bg | `mozzi_bg` | 1 | ok | — | — | — | — | 1 | — | 1 | 32 |
| ui | `mozzi_prop` | 6 | fail | max_colors | 6 | — | 1 | 2 | 1 | 10 | 225 |
| **합** | | **111** | | | **25** | **3** | **12** | **5** | **54** | **99** | |

- 「램프 밖 색」 은 공용 팔레트에 없는 색 가짓수다. `exact_match: false` 라 실패가 아니고 알리기만 한다.
- `ramp_shape` 경고는 그림이 아니라 팔레트 파일을 보는 것이라 표에서 뺐다 (모든 묶음에 똑같이 뜬다) : 램프 12줄.
- 종류 표(`mozzi_kinds.json` 무리)에 없어 폴더 기본 종류로 넣은 그림 : Mozzi/acc_none.png

## 실패한 묶음

- bldg : `max_colors` — 한도 48가지를 넘은 그림 6개 : kitchen_pound_1.png 55, kitchen_pound_2.png 53, kitchen_pound_3.png 58, kitchen_pound_4.png 57, kitchen_pound_5.png 60, kitchen_pound_6.png 54
- char_mozzi : `max_colors` — 한도 8가지를 넘은 그림 2개 : body_hop1.png 9, body_hop3.png 32
- ui : `max_colors` — 한도 48가지를 넘은 그림 1개 : ui_bubble_c.png 56

## 경고가 걸린 그림

| 묶음 | 경고 | 그림 |
| --- | --- | --- |
| bldg | `color_cap` | kitchen_pound_1.png, kitchen_pound_2.png, kitchen_pound_3.png, kitchen_pound_4.png, kitchen_pound_5.png, kitchen_pound_6.png, storage_main.png |
| bldg | `outline` | stall_main.png, storage_main.png, synthesis_machine_done.png, synthesis_main.png |
| char_mozzi | `near_colors` | body_hop3.png |
| char_mozzi | `outline` | acc_crown.png, cheek.png, face_02.png, face_03.png, face_05.png, face_07.png, face_10.png |
| char_people | `color_cap` | face_03.png |
| char_people | `outline` | body.png, face_01.png, face_02.png, face_03.png, hair_01_black.png, hair_01_brown.png, hair_01_orange.png, hair_01_purple.png, hair_02_black.png, hair_02_brown.png, hair_02_orange.png, hair_02_purple.png, shirt_blue.png, shirt_green.png, shirt_red.png, shirt_yellow.png, stall_owner.png |
| icon | `outline` | bean.png, bean_cake.png, jujube.png, jujube_cake.png, mugwort.png, mugwort_cake.png, mugwort_sesame_cake.png, pinenut.png, pinenut_cake.png, pinenut_jujube_cake.png, plain_cake.png, redbean_bean_cake.png, redbean_cake.png, redbean_mugwort_sesame_cake.png, sesame.png, sesame_cake.png |
| piece | `color_cap` | kitchen_plate.png, stall_prop_menu.png, stall_prop_register.png, stall_prop_vase.png, synthesis_plate.png |
| piece | `isolated` | stall_prop_menu.png, stall_prop_register.png, stall_prop_vase.png |
| piece | `near_colors` | synthesis_plate.png |
| piece | `outline` | stall_board_topper.png, stall_counter.png, stall_prop_menu.png, stall_prop_vase.png |
| tile | `color_cap` | decor_04.png, decor_05.png, decor_06.png, fence_h.png, fence_post.png, fence_v.png |
| tile | `integer_scale` | decor_03.png, fence_h.png, fence_post.png |
| tile | `isolated` | decor_01.png, decor_03.png, decor_04.png, decor_05.png, decor_06.png, fence_h.png, fence_post.png, fence_v.png |
| tile | `outline` | decor_07.png, decor_08.png, decor_09.png, decor_10.png, fence_v.png |
| tile_bg | `near_colors` | grass_base.png |
| ui | `color_cap` | icon_codex.png, icon_menu.png, icon_upgrade.png, ui_bubble.png, ui_bubble_c.png, ui_button.png |
| ui | `isolated` | ui_bubble_c.png |
| ui | `near_colors` | ui_bubble_c.png, ui_button.png |
| ui | `outline` | ui_bubble_c.png |

## 겹 묶음 (`layers check`)

| 겹 묶음 | 판정 | 조합 수 | 겹 | 경고 |
| --- | --- | --- | --- | --- |
| `layers/mozzi.json` | ok | 10 | body → cheek → face → acc | — |
| `layers/customer.json` | ok | 8 | body → shirt → bottom → face → hair | — |
