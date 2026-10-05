# 화풍 한 벌 — 프로필 · 팔레트 · 겹 꼴

게임 그림을 한 화풍으로 재고 맞출 때 쓰는 **ArtTool 설정 파일**이 모인 폴더다.
값의 정본은 `Docs/Design/화풍통일/2026-10-05-그림화풍통일설계.md` (3 ~ 6절 · 11절)이고, 여기는 그 값을 툴이 읽는 꼴로 옮긴 것이다.

| 자리 | 무엇 |
| --- | --- |
| `palettes/mozzi_master.json` | 공용 팔레트. 램프 14 무리(음식은 재료별 여섯으로 갈라 파일에는 19줄) |
| `profiles/mozzi_*.yaml` | 프로필 여섯 — 종류마다 외곽선 · 색 수 한도가 다르다 |
| `profiles/palettes/mozzi_master.json` | 위 팔레트의 **사본**. 툴이 읽는 것은 이쪽이다 (아래 「팔레트 사본」) |
| `layers/mozzi.json` · `customer.json` | 겹 묶음 꼴 — 겹 이름과 쌓는 순서 |
| `check_all.py` | 게임 그림 전체를 종류별 프로필로 한 번에 재는 스크립트 |
| `baseline/` | 개편 전(2026-10-05)에 잰 기준선. `summary.md` 부터 본다 |

## 어느 그림에 어느 프로필

명령은 저장소 뿌리에서 돌린다. `<폴더>` 는 검사할 PNG 가 바로 아래 있는 폴더(또는 PNG 한 장)다.

| 그리는 것 | 프로필 | 외곽선 | 색 수 한도 | 검사 명령 |
| --- | --- | --- | --- | --- |
| 찹쌀이 몸 · 표정 · 볼 · 악세 | `mozzi_char` | 한 색 선 `#5E5048` | 8 | `Tools\ArtTool\.venv\Scripts\arttool.exe check --profile Docs\Reference\Art\화풍\profiles\mozzi_char.yaml --in <폴더> --report <보고.json>` |
| 손님 · 주인 | `mozzi_people` | selout | 40 | 위 줄에서 프로필만 `mozzi_people.yaml` |
| 건물 · 기계 · 화면 조각 · 소품 · UI · 농장 장식 · 울타리 | `mozzi_prop` | selout | 48 (권장 : 32px 까지 16 · 64px 까지 32 · 그 위 48) | 프로필만 `mozzi_prop.yaml` |
| 떡 · 재료 아이콘 | `mozzi_icon` | selout | 8 | 프로필만 `mozzi_icon.yaml` |
| 화면 배경 · 하늘 · 길 · 잔디 바탕 | `mozzi_bg` | 없음 | 배경 68 | 프로필만 `mozzi_bg.yaml` |
| 연출 | `mozzi_fx` | 없음 | 1 (흰색) | 프로필만 `mozzi_fx.yaml` |

- 「외곽선」 칸의 selout 은 프로필 값이다. 실제 그림은 대부분 **따뜻한 갈색 한 색 선**이라 툴이 `solid` 로 읽는다 (아래 「알고 두는 경고 · 실패」).

- 프로필이 읽히는지 : `arttool.exe profile show --profile Docs\Reference\Art\화풍\profiles\mozzi_icon.yaml` (종료 0 이면 통과).
- 보고 JSON 의 `status` 가 `fail` 이면 고친다. `warnings` 는 아래 「알고 두는 경고」 가 아니면 본다.
- **시안(게임에 안 넣는 그림)에는 `check` 를 안 돌린다.** `arttool sheet` 비교판으로 눈으로만 본다.
- 한국어 출력이 깨지면 명령 앞에서 `PYTHONIOENCODING=utf-8` 을 주거나 `--report` 파일을 읽는다.

## 검사 길 (설계 11절)

| 차례 | 무엇 | 명령 |
| --- | --- | --- |
| 1 | 프로필 여섯 통과 | `arttool.exe profile show --profile Docs\Reference\Art\화풍\profiles\mozzi_<이름>.yaml` — 끝남 (2026-10-05) |
| 2 | 개편 전 기준선 | `baseline/` 에 있다 — 끝남 (2026-10-05) |
| 3 | 갈래마다 자기 그림만 | 위 표의 `check` 명령. 실패 0 · 경고는 알고 두는 것만 |
| 4 | 시안 · 비교판 | `arttool.exe sheet --in <그림…> --out <판.png> --label` → `SendUserFile` 로 보낸다 |
| 6 | 합친 뒤 전체 다시 | `Tools\ArtTool\.venv\Scripts\python.exe Docs\Reference\Art\화풍\check_all.py --out Docs\Reference\Art\점검\<날짜>\check_after --label "개편 뒤"` → `summary.md` 의 「새로 생긴 것」 이 0 인지 본다 — 끝남 (2026-10-05, `점검/2026-10-05/check_after/`) |

- 5(Play 캡처) · 7(APK)은 코드 갈래 · 뒷정리 갈래 일이라 설계 문서를 본다.
- `check_all.py` 는 Resources 그림을 임시 폴더로 복사해 잰다 (원본은 읽기만). 이미 결과가 있는 폴더에는 `--force` 없이 안 쓴다.
- 알고 두는 경고 · 실패는 스크립트의 `KNOWN` 목록으로 따로 센다. 요약 맨 위 「새로 생긴 것」 에는 목록 밖만 나온다. 목록을 고치면 아래 표도 같이 고친다.
- 종류 표에 있지만 일부러 지운 그림(메뉴 세움판 `stall_prop_menu` · 음양 반쪽 연출 `fx_yinyang_half` · 주방 쟁반 `kitchen_tray`)은 `REMOVED` 에 적는다.
- 종류 나눔은 `점검/2026-10-05/style/` 의 종류 표를 쓴다. 거기 없는 새 그림은 폴더 기본 종류로 들어가고 요약에 이름이 찍힌다 (`Screen/` 의 새 그림은 `piece` → `mozzi_prop`, 떡 낱개 `CakeOne/` 은 `icon` → `mozzi_icon`).

## 알고 두는 경고 · 실패

2026-10-05 개편 뒤 재검사(`점검/2026-10-05/check_after/`)에서 남은 것 전부다. 이 밖의 것이 뜨면 고친다.

| 경고 · 실패 | 어디 | 왜 두나 |
| --- | --- | --- |
| **실패** `alpha` | 진열장 `display_case` | 뒤 유리를 반투명 50% 로 깔았다. **반투명 없음 규칙의 단 하나 예외**다 (사용자 확정 2026-10-05) |
| **실패** `max_colors` · 경고 `color_cap` | 떡메 `kitchen_pound_1~6` | 주방 갈아엎기 때 바뀐다 |
| `outline` (53장) | 사람 16 · 아이콘 15 · 찹쌀이 표정 · 볼 · 악세 7 · 건물 4 · 소품 3 · 장식 · 울타리 5 · UI 3 | 프로필은 `selout` 인데 갈색 한 색 선은 `solid`, 빛 쪽이 옅은 선은 `selout+light` 로 읽힌다. 찹쌀이 표정 · 볼은 선 없는 기호다. **툴이 외곽선 값을 하나만 받아서** 풀 수 없다 |
| `ramp_shape` (램프 12줄) | 모든 검사에 똑같이 | 그림이 아니라 팔레트를 본다. 「밝은 쪽이 노랑으로 안 간다」 · 「색조 차 5° 미만」 은 **밝기 위주 램프로 정한 결과**다. 「단 수 모자람」 은 짧은 램프를 **툴이 못 받아** 채운 칸 때문이다 |
| `ramp_colors` 「램프 밖 색 N가지」 | 모든 검사 | 실패가 아니다. 그림을 팔레트에 맞춘 적이 없어 알리기만 한다 (`exact_match: false`) |
| `color_cap` | 손님 머리통 `face_03` (18 > 16) | 겹마다 한도(머리통 16 · 몸 40)가 다른데 **툴이 긴 변 하나로만 한도를 고른다** |
| `color_cap` | 금전등록기 · 꽃병 (34 · 36 > 32) | 작은 소품은 설계상 16 이지만 크기로 못 갈라 32 로 잰다. 색 줄이기는 다음 판 |
| `color_cap` | 꽃 장식 `decor_04~06` · 울타리 `fence_v` | 꽃잎 잡색 합치기는 다음 판. `fence_v` 는 32px 이라 16 칸에 걸린다 (설계 한도 32 안) |
| `color_cap` | 메뉴 아이콘 `icon_codex` · `icon_upgrade` (43 · 45 > 32) | 이번 범위 밖 |
| `isolated` | 장식 다섯 · 울타리 셋 · 소품 둘 | 풀잎 점 · 통나무 결 · 잔 점. 1배에서 안 깨져 보인다 |
| `integer_scale` | `fence_h` · `fence_post` | 통나무 결이 부드러운 확대처럼 읽힌다 |
| `near_colors` | 잔디 바탕 | 이음 없는 판의 색 번짐 |

- 툴이 바뀌어야 풀리는 것(외곽선 여러 값 · 짧은 램프 · 겹마다 색 한도)은 `Tools/ArtTool/Docs/Todo/2026-10-05-화풍통일준비피드백.md` 에 적었다.

## 팔레트 사본

ArtTool 은 팔레트 파일을 **프로필 파일이 있는 폴더 아래에서만** 찾는다 (`..` 로 올라가는 경로는 거절한다).
그래서 `profiles/palettes/` 에 사본을 둔다. **고칠 때는 `palettes/mozzi_master.json` 만 고치고** 아래로 사본을 맞춘다.

```
Tools\ArtTool\.venv\Scripts\python.exe Docs\Reference\Art\화풍\check_all.py --out <결과 폴더> --sync-palette
```

두 파일이 다르면 `check_all.py` 가 멈춘다 (종료 2).

- 툴이 램프 길이를 하나로 요구해, 짧은 램프는 **밝은 끝 색을 되풀이해 5칸**으로 채웠다. 진짜 칸 수는 파일의 `real_len` 에 있다.
- 2026-10-05 뒷정리 : `line` 은 툴 밝기 순(`#211D16` `#352E27` `#4A2622` `#3A2E25`)에 찹쌀이 몸 선 `#5E5048` 을 밝은 끝으로 더했다. `grass` 는 풀 장식 새 색 다섯(`#314F2A` ~ `#BBD76E`)으로 바꿨다. 잔디 바탕 `#C0CF7D` 는 `mozzi_bg` 그림이라 램프에 안 넣었다.
- 원작 색 넷의 자리 : `pink[3]` `#F8BBC0` · `accent[2]` `#FA6270` · `violet[4]` `#AE74DB` · `navy[1]` `#393651` (0 이 가장 어두운 칸).

## 겹 묶음 꼴

ArtTool 의 겹 묶음은 「`layers.json` + `<겹>/<그림>.png`」 폴더다. 게임 그림은 낱장으로 흩어져 있어(`body_hop0.png` · `face_01.png` …),
검사할 때는 `layers/<이름>.json` 을 `layers.json` 으로 복사하고 겹 폴더를 임시로 세운다. `check_all.py` 가 그렇게 해서 `layers check` 를 돌린다.

| 꼴 | 캔버스 | 겹 (아래 → 위) | 피벗 | 파일 이름 |
| --- | --- | --- | --- | --- |
| `mozzi.json` | 64×64 | `body` → `cheek` → `face` → `acc` | 아래 가운데 · 바닥 줄 y 50 | `body_hop0~3` · `cheek` · `face_01~10` · `acc_crown` · `acc_none` |
| `customer.json` | 120×240 | `body` → `shirt` → `bottom` → `face` → `hair` | (66, 240) 발끝 가운데 | `body` · `shirt_<색>` · `bottom_<꼴>` · `face_01~03` · `hair_<꼴>_<색>` |

- `cheek` 은 11×11 그림을 코드가 좌우에 붙이는 겹이라 **겹 파일로는 못 넣는다** (겹은 캔버스 크기여야 한다). 순서 자리만 잡아 두고 비워 둔다.
- `bottom` 은 치마 · 정장이 들어갈 자리다. 아직 그림이 없다.
- 새 겹 그림을 그렸으면 같은 꼴로 폴더를 세워 `arttool.exe layers check --in <겹 묶음 폴더>` 를 돌린다.
