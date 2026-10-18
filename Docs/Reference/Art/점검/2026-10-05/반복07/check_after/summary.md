# 그림 전체 검사 요약 — 반복07

> **2026-10-18 정리** : 이 문서가 가리키던 그림 중 안 쓰는 시안·견줌판·옛 판은 저장소에서 지웠다 (`(정리함 2026-10-18)` 표시). 다시 보려면 git 기록에서 꺼낸다.

`check_all.py` 가 낸 표다. 그림 171장 · 묶음 11개. 숫자는 **걸린 그림 장 수**다.

## 묶음별

| 묶음 | 프로필 | 장 | 판정 | 실패 규칙 | `color_cap` | `integer_scale` | `isolated` | `near_colors` | `outline` | 경고 합 | 램프 밖 색 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| bg | `mozzi_bg` | 5 | ok | — | — | — | — | — | — | 0 | 183 |
| bldg | `mozzi_prop` | 22 | ok | — | — | — | — | — | 11 | 11 | 226 |
| char_mozzi | `mozzi_char` | 17 | ok | — | — | — | — | — | 7 | 7 | 18 |
| char_people | `mozzi_people` | 17 | ok | — | 1 | — | — | — | 16 | 17 | 144 |
| fx | `mozzi_fx` | 7 | ok | — | — | — | — | — | — | 0 | 1 |
| fx_prop | `mozzi_prop` | 24 | ok | — | — | — | — | — | 24 | 24 | 3 |
| icon | `mozzi_icon` | 28 | ok | — | — | — | — | — | 24 | 24 | 39 |
| piece | `mozzi_prop` | 29 | fail | alpha | 2 | 2 | 4 | 1 | 20 | 29 | 453 |
| tile | `mozzi_prop` | 14 | ok | — | 4 | 2 | 8 | — | 5 | 19 | 55 |
| tile_bg | `mozzi_bg` | 1 | ok | — | — | — | — | 1 | — | 1 | 33 |
| ui | `mozzi_prop` | 7 | ok | — | 2 | — | — | — | 3 | 5 | 181 |
| **합** | | **171** | | | **9** | **4** | **12** | **2** | **110** | **137** | |

- 「램프 밖 색」 은 공용 팔레트에 없는 색 가짓수다. `exact_match: false` 라 실패가 아니고 알리기만 한다.
- `ramp_shape` 경고는 그림이 아니라 팔레트 파일을 보는 것이라 표에서 뺐다 (모든 묶음에 똑같이 뜬다) : 램프 12줄.
- 종류 표(`mozzi_kinds.json` 무리)에 없어 폴더 기본 종류로 넣은 그림 : CakeOne/bean_cake_one.png, CakeOne/jujube_cake_one.png, CakeOne/mugwort_cake_one.png, CakeOne/mugwort_sesame_cake_one.png, CakeOne/pinenut_cake_one.png, CakeOne/pinenut_jujube_cake_one.png, CakeOne/plain_cake_one.png, CakeOne/redbean_bean_cake_one.png, CakeOne/redbean_cake_one.png, CakeOne/redbean_mugwort_sesame_cake_one.png, CakeOne/sesame_cake_one.png, Fx/fx_swirl_a_0.png, Fx/fx_swirl_a_1.png, Fx/fx_swirl_a_2.png, Fx/fx_swirl_a_3.png, Fx/fx_swirl_a_4.png, Fx/fx_swirl_a_5.png, Fx/fx_swirl_a_6.png, Fx/fx_swirl_a_7.png, Fx/fx_swirl_b_0.png, Fx/fx_swirl_b_1.png, Fx/fx_swirl_b_2.png, Fx/fx_swirl_b_3.png, Fx/fx_swirl_b_4.png, Fx/fx_swirl_b_5.png, Fx/fx_swirl_b_6.png, Fx/fx_swirl_b_7.png, Fx/fx_swirl_top_0.png, Fx/fx_swirl_top_1.png, Fx/fx_swirl_top_2.png, Fx/fx_swirl_top_3.png, Fx/fx_swirl_top_4.png, Fx/fx_swirl_top_5.png, Fx/fx_swirl_top_6.png, Fx/fx_swirl_top_7.png, Fx/fx_window_mask.png, Mozzi/acc_none.png, UI/ui_badge_alert.png

## 새로 생긴 것 (알고 두는 목록 밖)

실패 **0** · 경고 **0** · 알고 두는 것 138 (목록은 `check_all.py` 의 `KNOWN` · README 「알고 두는 경고 · 실패」).

없다.

## 실패한 묶음

- piece : `alpha` — 반투명 픽셀이 있는 프레임 1개

## 경고가 걸린 그림

| 묶음 | 경고 | 그림 |
| --- | --- | --- |
| bldg | `outline` | kitchen_pound_0.png, kitchen_pound_1.png, kitchen_pound_2.png, kitchen_pound_3.png, kitchen_pound_4.png, kitchen_pound_5.png, kitchen_pound_6.png, stall_main.png, storage_main.png(정리함 2026-10-18), synthesis_machine_done.png, synthesis_main.png |
| char_mozzi | `outline` | acc_crown.png, cheek.png, face_02.png, face_03.png, face_05.png, face_07.png, face_10.png |
| char_people | `color_cap` | face_03.png |
| char_people | `outline` | body.png, face_01.png, face_02.png, face_03.png, hair_01_black.png, hair_01_brown.png, hair_01_orange.png, hair_01_purple.png, hair_02_black.png, hair_02_brown.png, hair_02_orange.png, hair_02_purple.png, shirt_blue.png, shirt_green.png, shirt_red.png, shirt_yellow.png |
| fx_prop | `outline` | fx_swirl_a_0.png, fx_swirl_a_1.png, fx_swirl_a_2.png, fx_swirl_a_3.png, fx_swirl_a_4.png, fx_swirl_a_5.png, fx_swirl_a_6.png, fx_swirl_a_7.png, fx_swirl_b_0.png, fx_swirl_b_1.png, fx_swirl_b_2.png, fx_swirl_b_3.png, fx_swirl_b_4.png, fx_swirl_b_5.png, fx_swirl_b_6.png, fx_swirl_b_7.png, fx_swirl_top_0.png, fx_swirl_top_1.png, fx_swirl_top_2.png, fx_swirl_top_3.png, fx_swirl_top_4.png, fx_swirl_top_5.png, fx_swirl_top_6.png, fx_swirl_top_7.png |
| icon | `outline` | bean.png, bean_cake.png, bean_cake_one.png, jujube_cake.png, mugwort.png, mugwort_cake.png, mugwort_cake_one.png, mugwort_sesame_cake.png, mugwort_sesame_cake_one.png, pinenut.png, pinenut_cake.png, pinenut_cake_one.png, pinenut_jujube_cake.png, plain_cake.png, plain_cake_one.png, redbean_bean_cake.png, redbean_bean_cake_one.png, redbean_cake.png, redbean_cake_one.png, redbean_mugwort_sesame_cake.png, redbean_mugwort_sesame_cake_one.png, sesame.png, sesame_cake.png, sesame_cake_one.png |
| piece | `color_cap` | stall_prop_register.png, stall_prop_vase.png |
| piece | `integer_scale` | kitchen_drawer_frame.png, kitchen_shape_1.png |
| piece | `isolated` | kitchen_shape_0.png, kitchen_shape_1.png, stall_prop_register.png, stall_prop_vase.png |
| piece | `near_colors` | kitchen_drawer_frame.png |
| piece | `outline` | kitchen_board.png, kitchen_bowl.png, kitchen_pound_rough_0.png, kitchen_pound_rough_1.png, kitchen_pound_rough_2.png, kitchen_pound_rough_3.png, kitchen_pound_rough_4.png, kitchen_pound_rough_5.png, kitchen_pound_rough_6.png, kitchen_pound_smooth_0.png, kitchen_pound_smooth_1.png, kitchen_pound_smooth_2.png, kitchen_pound_smooth_3.png, kitchen_pound_smooth_4.png, kitchen_pound_smooth_5.png, kitchen_pound_smooth_6.png, kitchen_steamer.png, stall_board_topper.png, stall_counter.png, stall_prop_vase.png |
| tile | `color_cap` | decor_04.png, decor_05.png, decor_06.png, fence_v.png |
| tile | `integer_scale` | fence_h.png, fence_post.png(정리함 2026-10-18) |
| tile | `isolated` | decor_01.png, decor_03.png, decor_04.png, decor_05.png, decor_06.png, fence_h.png, fence_post.png(정리함 2026-10-18), fence_v.png |
| tile | `outline` | decor_07.png(정리함 2026-10-18), decor_08.png(정리함 2026-10-18), decor_09.png(정리함 2026-10-18), decor_10.png(정리함 2026-10-18), fence_v.png |
| tile_bg | `near_colors` | grass_base.png |
| ui | `color_cap` | icon_codex.png(정리함 2026-10-18), icon_upgrade.png |
| ui | `outline` | ui_badge_alert.png, ui_bubble_c.png, ui_button.png |

## 겹 묶음 (`layers check`)

| 겹 묶음 | 판정 | 조합 수 | 겹 | 경고 |
| --- | --- | --- | --- | --- |
| `layers/mozzi.json` | ok | 10 | body → cheek → face → acc | — |
| `layers/customer.json` | ok | 8 | body → shirt → bottom → face → hair | — |
