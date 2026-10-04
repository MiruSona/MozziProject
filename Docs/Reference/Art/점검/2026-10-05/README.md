# 그림 전체 점검 자료 (2026-10-05)

게임 그림 111장을 ArtTool 로 잰 결과물이다. 풀이는 `Docs/Research/2026-10-05-그림전체점검과화풍조사.md` 에 있다.
읽기 자료다 — 여기 그림을 게임에 넣지 않는다.

| 폴더 | 무엇 |
| --- | --- |
| `sheets/` | 종류별 비교판 (확대 · 실루엣 · 4색 · 흐림). `icon` · `char_mozzi` · `char_people` · `tile` · `ui_fx` · `piece` · `bldg` · `bg` |
| `feedback/` | v16 피드백 셋 비교 그림 — 손님 머리 ×6, 진열장 위 떡 (왼쪽 지금 · 오른쪽 바닥 맞춤) ×4, 합성 서랍 칸 대 찹쌀이 ×3, 잰 숫자 `feedback_measure.json` |
| `template_over/` | `template render --over` 세 판 — 기계 208×220 · 찹쌀이 `char_small 64` · 떡 `icon_set 32` |
| `style/` | `style extract --by-folder` 결과 — 종류별 팔레트 JSON · 견본 PNG · 프로필 · 보고서 · `kinds.json` |
| `check/` | 종류별 `check` 보고서 JSON |
| `measure/` | 장마다 잰 표 `measure.md` · `measure.json` · 요약 둘 · 잰 스크립트 셋 (`measure.py` · `fb.py` · `sumcheck.py`) |

- 원본 그림 자리는 `ProjectMozziUnity/Assets/_Project/Resources/Art/` 다. 이 점검은 읽기만 했다.
- 스크립트의 원본 경로는 절대 경로다. 다른 자리에서 돌리려면 맨 위 경로만 바꾼다.
