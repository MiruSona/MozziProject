# 반복 06 — Unity Play 확인 캡처

2026-10-05 확인 갈래가 Play 모드에서 찍었다. Game 뷰는 540×1200(일부 540×960)이다.
결론은 이렇다. 가판대·주방·합성소·농장 모두 막는 버그는 없었다. 고친 것은 경고 하나(MozziArtKeys CS0162)뿐이다.

찍는 법은 이렇다.
- 화면 연출(UI)은 `VfxManager._ui` 디스패처를 잠깐 바꿔 끼워 멈추고 손으로 0.05초씩 돌렸다. 에디터 일시정지 중에는 Game 뷰가 다시 안 그려진다.
- 농장은 `GameMaster.TimeScale=0` 으로 멈추고 `FarmViewManager.Process(dt)` 를 직접 불렀다.
- 입력은 버튼 `onClick.Invoke()`, 농장은 `TapAtWorld` · `OnFarmMozziDropped` · `SweepFloor` 로 흉내 냈다.
- 이름이 `_grid` · `_crop` 인 파일은 여러 장을 잘라 붙이거나 일부를 키운 그림이다.

## 가판대 (피드백 3 · 7)

| 파일 | 무엇을 본 것 | 판정 |
| --- | --- | --- |
| stall_1200_open | 손님 셋 앉은 자리·말풍선 안 겹침·「뒤에 N명」·계산대 오른끝 이음매 | 통과 |
| stall_1200_queue5_more | 줄 5명일 때 뒤 손님 수 글자 | 통과 |
| stall_walk_a00 ~ a35 | 손님 하나 대접 뒤 남은 손님이 한 칸씩 걸어옴 (0.05초 간격) | 통과 |
| stall_walk_refresh_mid | 걷는 중 새로고침해도 x 가 안 튐 | 통과 |
| stall_reopen_midwalk | 걷는 중 닫고 다시 열면 제자리 | 통과 |
| stall_1200_menu_open / _after_make | 차림표 판 열기 → 한 줄로 떡 하나, 판은 열린 채 손님 표정 바뀜 | 통과 |
| stall_1200_menu_full_disabled | 진열장이 차면 모든 줄이 꺼짐 | 통과 |
| stall_960_menu_open · stall_960_queue | 9:16 에서 판 위끝·줄 | 통과 (판이 진열장을 가림 — 사용자 판단) |
| stall_1200_menu_bottom · stall_960_menu_bottom | (추가 피드백) 차림표 판을 아래로 : 판 아래끝이 버튼 줄 바로 위(8 띄움), 버튼 다섯이 다 보임. 8줄이라 높이 328. 팥떡 줄을 누르면 진열 28 → 29 | 통과 (판이 손님 줄 자리를 덮는다 — 사용자 판단) |

## 주방 네 단계 (피드백 9)

| 파일 | 무엇을 본 것 | 판정 |
| --- | --- | --- |
| kitchen_1200_layout · kitchen_960_layout | 서랍 12칸·찜기·절구·빚기판·접시·버튼 셋, 겹침 없음 | 통과 |
| kitchen_flow1 ~ flow6 | 고름 → 찜 → 침 1/3·3/3 → 빚음 → NEW 떡 → 처음으로 | 통과 |
| kitchen_known_quickmake | 아는 조합은 찜기 한 번에 바로 만듦 | 통과 |
| kitchen_1200_menu_open / _made | 주방 차림표 판 | 통과 |
| kitchen_1200_menu_bottom · kitchen_960_menu_bottom | (추가 피드백) 주방 차림표 판도 버튼 줄 바로 위. 버튼 셋이 다 보임. 콩떡 줄을 누르면 진열 20 → 21. 안내 글줄은 판에 덮인다 | 통과 |

## 합성소 (피드백 13)

| 파일 | 무엇을 본 것 | 판정 |
| --- | --- | --- |
| synth_merge_f00 ~ f17 | 빨려 들어감 → 창 안 회전·앞뒤 밝기·놀란 얼굴·색 물듦·번쩍임 | 통과 |
| synth_1200_4running_p00 / p50 / p98 | 네 칸 동시 진행, 시간 따라 색이 물듦 | 통과 |
| synth_1200_done_two · open_done | 다 된 칸의 ? 실루엣·반짝이 | 통과 |
| synth_1200_take_anim_early / _end · take_result | 꺼내기 → 결과판 NEW | 통과 |
| synth_1200_reopen · reopen_running | 돌던 중 다시 열면 회전부터 이어 감 | 통과 |
| synth_1200_idle_4slots · synth_960_running | 빈 칸·9:16 | 통과 |

### 합성 연출 다시 (추가 피드백 D — 반죽 소용돌이 · 깔때기 속으로)

위 표의 `synth_merge_*` · `synth_1200_4running_*` 는 옛 연출(찹쌀이 둘이 도는 판)이다. 아래가 지금 판이다.

| 파일 | 무엇을 본 것 | 판정 |
| --- | --- | --- |
| synth_swirl_enter_f0 ~ f5 | 깔때기에 앉은 두 마리가 제자리에서 아래로 내려가 그릇 앞 가장자리 밑으로 사라짐 (0.06초 간격). 몸통 앞을 안 지남 | 통과 |
| synth_swirl_enter_f6 ~ f8 | 다 들어간 뒤 창 안 소용돌이 시작 + 번쩍 + 반짝이 → 대기 | 통과 |
| synth_swirl_run_p00_a ~ c | 진행 0% · 0.11초 간격 세 장. 프레임이 넘어가고 색은 넣은 두 마리(빨강 · 초록) 그대로 | 통과 |
| synth_swirl_run_p50_a ~ c | 진행 50%. 두 색이 서로 쪽으로 반쯤 물듦 | 통과 |
| synth_swirl_run_p100_a ~ c | 진행 거의 100%. 35% 까지 물듦 (빨강 + 초록은 갈색 · 풀색으로 탁해진다 — 사용자가 볼 것) | 통과 |
| synth_swirl_1200_states · synth_swirl_960_states | 기계 넷이 다른 상태 (도는 중 · 다 됨 · 재료 얹은 빈 칸 · 같은 종 둘로 도는 중). 창 밖으로 안 삐져나옴 | 통과 |
| synth_swirl_reopen_running · synth_swirl_1200_reopen | 돌던 중 다시 열면 들어가기 없이 바로 소용돌이 (첫 장은 「돌아왔어요」 판에 가려 있다) | 통과 |

- 위 `synth_swirl_*` 는 빈틈이 있는 처음 판 그림으로 찍었다. 반죽 사이 회색은 그림의 빈틈으로 창 유리가 비친 것이다.

### 다시 뽑은 판을 들인 뒤 (사용자가 regen 판을 고름)

`R/Art/Fx/fx_swirl_*` 24장을 다시 뽑은 판(정본)으로 바꾸고 다시 찍었다. 원본 낱장과 바이트가 같다 (24/24).

| 파일 | 무엇을 본 것 | 판정 |
| --- | --- | --- |
| synth_swirl2_run_p00_a ~ c | 진행 0% · 0.11초 간격 세 장 (주황 + 파랑). 창이 두 색으로 꽉 차고 빈틈(회색)이 없다. 프레임이 7 → 0 → 1 로 넘어간다 | 통과 |
| synth_swirl2_run_p100 | 진행 98%. 두 색이 서로 34% 물들어 탁한 갈색 · 보라빛 회색이 된다. 창은 그대로 꽉 차 있다 | 통과 (탁한 색은 사용자가 볼 것) |
| synth_swirl2_1200_four · synth_swirl2_960_four | 기계 넷 전체 화면 540×1200 · 540×960(9:16). 도는 중 둘 · 다 됨 · 재료 얹은 빈 칸. 소용돌이가 창 테두리 밖으로 안 나간다 | 통과 |
- 살 칸 · 잠긴 칸은 이 세이브에 없어(칸 넷을 다 샀다) 못 찍었다.

## 농장 (v15 피드백 2 ~ 5)

| 파일 | 무엇을 본 것 | 판정 |
| --- | --- | --- |
| farm_start_t0 / t1 / t3 | 시작 직후·1초·3초. 40마리 중 21 → 29 → 40 마리가 걷는다. 서로 0.9 반지름 안으로 겹친 쌍 0 | 통과 |
| farm_decor025_behind / _front · farm_decor025_sort | 발 y 로 앞 장식(decor_025 풀숲)과 앞뒤가 맞음 (sortingOrder 250 < 260 < 273) | 통과 |
| farm_fence_side_slide · farm_fence_side_crop | 담장 밖에 놓으면 미끄러져 돌아옴. 옆 담장 기둥이 찹쌀이 앞에 그려짐 | 통과 (기둥 겹침은 사용자 판단) |
| farm_fence_bottom_crop | 아래 담장 위 WanderBottom 0.6 자리 | 통과 |
| farm_store_f0_held ~ f8 · farm_store_frames_grid | 궤짝에 넣기 0.05초 간격. 화면 y 548 ~ 576, 아래·밖으로 가는 프레임 없음 | 통과 |
| farm_storage_after_tap | 넣는 중 궤짝을 누르면 보관함 화면이 열림 (찹쌀이가 가로채지 않음) | 통과 |
| farm_place_from_storage | 보관함에서 꺼낸 마리 : 놓은 자리 그대로·크기 1·기본 표정·정렬·콜라이더 켜짐 | 통과 |
| farm_full40_place_refused | 40/40 일 때 꺼내기 거절, 보관함으로 돌아옴 | 통과 |
| farm_drop_mid / _landed · farm_drop_grid | 낱개가 발밑에 떨어지고 숫자가 없음 | 통과 |
| farm_stepped_item_before / _after · _grid | 찹쌀이 밑 재료를 누르면 재료가 먼저 주워짐 (팝업 안 뜸) | 통과 (기록으로 확인, 재료는 몸에 가려 안 보임) |
| farm_partial_before / _shake / _after · _grid | 창고 98/100 에서 10 꾸러미 → 8 남고 흔들림, 100/100 에서 다시 눌러도 그대로 | 통과 |
| farm_merge30_falling / _landed | 35개를 떨구면 30개로 뭉침, 합계는 그대로 | 통과 |
| farm_skip_popup · farm_skip_bundles | 디버그 Skip 뒤 재료별 꾸러미 (그림 + 수량), 담장 안 | 통과 |
| farm_bottom_items_crop | 아래 줄 더미 없음, 재료가 담장에 안 걸림 | 통과 |
| farm_mozzi_tap_popup | 찹쌀이 탭 → 표정 바뀜 + 팝업 | 통과 |
| farm_after_reload | 저장 → 다시 열기 : 마릿수·보관함·창고·더미 수가 그대로 | 통과 |

## 사용자가 볼 것

- **바닥 더미가 150.1만까지 쌓여 있었다** (farm_start_t0). 창고가 다 차야 생산이 멈추고, 바닥 더미에는 상한이 없다. 그래서 오래 비우면 끝없이 늘어난다. 상한을 둘지는 수치·설계 판단이다.
- **옆 담장 기둥이 찹쌀이 앞에 그려진다** (farm_fence_side_crop).
- **오른쪽 끝 찹쌀이가 화면 버튼 셋에 가려진다** (farm_fence_side_slide 오른쪽 아래).
- **꾸러미 숫자가 찹쌀이에 가려 안 보일 때가 많다** (farm_skip_bundles). 바닥 물건은 찹쌀이 뒤에 그린다.
- **창고가 꽉 찬 재료가 찹쌀이 밑에 있으면** 그 자리를 눌러도 재료만 흔들리고 찹쌀이는 안 눌린다 (바닥 재료 → 찹쌀이 순서라서).
- **궤짝에 넣을 때 찹쌀이가 「보관함」 글씨 위를 지나간다** (farm_store_frames_grid).
