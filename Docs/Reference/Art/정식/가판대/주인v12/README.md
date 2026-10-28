# 정식 그림 — 가판대 주인 v12 (손님 화풍 · 2026-10-28)

> 고른 시안 : 후보 A (`../raw/owner_v12_A_12102.png`). 견줌판은 `Docs/Reference/Art/시안/2026-10-28-가판대주인-v12-손님화풍.png`.
> 스크립트 : `build_owner_v12.py` (저장소 뿌리에서 `Tools/ArtTool/.venv/Scripts/python.exe -I Docs/Reference/Art/정식/가판대/주인v12/build_owner_v12.py`)

## 결론

- 손님과 같은 **120×240 전신 · 정면**. 축소 · 확대 없이 시안 크기 그대로다. 머리 꼭대기 8px, 발끝은 캔버스 맨 아래 줄.
- A 는 겉옷이 검정 소매뿐이라, PixelLab edit 로 **분홍(`#FED7D7` 계열) 오프숄더 재킷**을 팔에 입혔다.
  edit 판은 얼굴 · 머리 결까지 조금씩 흔들려서 **두 팔 상자만** A 위에 붙였다.
- 둘레 검정 선은 짙은 갈색 `#352E27`(어두운 면 옆 `#211D16`). 안쪽 선은 그대로다 (10-05 화풍통일 B-1 과 같은 색 · 고리 방식).
- **v12b (10-28 같은 날 수정)** : 짝짝이 부츠를 **양쪽 짧은 흰 부츠**로. 왼다리(x 34~57, y 172~239)를 좌우 반전해 오른다리 자리(새 x = 116 − x)에 놓고 옛 긴 부츠 칸(x 58~85)은 지웠다. 라벤더 램프는 부츠 상자 (30,205)-(90,240) 안에서만 손님 운동화 흰 램프로 (#BCBADF→#FAF9F9 · #ABA4CF·#9D93C1→#DDDAE1 · #8578A4→#B3AFC1 · #64507A→#88859A). 크레딧 0. 옛 판은 `out/Screen/stall_owner_v12a_boots_lavender.png`.
- 40색 · 반투명 0 · ArtTool `mozzi_people` check 통과 (외곽선 `mixed` 경고는 A 원본부터 있던 것).

## 만든 것

| 파일 | 무엇 |
| --- | --- |
| `out/Screen/stall_owner.png` | 정식 주인 120×240. `FormalArtCopy` 가 `Resources/Art/Screen/stall_owner.png` 로 복사 |
| `../raw/owner_v12_A_pink_12501.png` | PixelLab `edit_image_pro_flash` 결과 (참고 : 옛 주인을 120 폭으로 줄인 `../raw/owner_ref_cur_120.png`) |
| `../raw/owner_v12_A_pink_merge.png` | A + edit 판 두 팔 상자 (선 바꾸기 전) |
| `../화풍통일/check/check_people_owner_v12_final.json` | check 보고 |

## 어떻게 만들었나

| # | 무엇 | 길 |
| --- | --- | --- |
| 1 | 분홍 겉옷 | `edit_image_pro_flash` reference · 씨앗 12501 · **1회 9 생성** |
| 2 | 팔만 가져오기 | 상자 (8,118)-(47,170) · (70,118)-(110,170) 을 edit 판에서 A 로 |
| 3 | 둘레 선 | `arttool reline --pick dark --scope ring --tol 14 --depth 2 --color #352E27 --color-dark #211D16` |
| 4 | 색 수 | `arttool merge-colors --max-colors 40 --keep #352E27,#211D16` |

## 코드에 넘길 것

- 자리 `StallLayout.OwnerX = 373` · `OwnerY = 498` (그림 왼쪽 위). 옛 125×174 판의 518 에서 20 올렸다 —
  그대로 두면 계산대 윗면(y 660)에 가슴 아래가 잘려 분홍 소매가 안 보인다.
- 캡처 : `Docs/Reference/Art/점검/2026-10-28/stall_owner_v12.png` · `stall_owner_v12_before_after.png`
