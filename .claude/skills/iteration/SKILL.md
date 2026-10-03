---
name: iteration
description: Use when the user has played the game and gives feedback, or types /iteration — 사용자가 한 판 놀고 느낀 것을 던질 때 「피드백 → 갈래 나누기 → 고치기 → 새 판 → 기록」 한 바퀴를 같은 순서로 도는 법 (2026-10-03 부터의 일하는 방식).
---

# 반복 한 바퀴 (iteration)

9단계 뒤의 일하는 방식이다. 왜 이렇게 하나는 `Docs/Todo/진행상황.md` 「지금 일하는 방식 — 반복」 절에 있다.
이 스킬은 **순서만** 고정한다. 판단(무엇을 고칠지)은 사용자가 한다.

**한 바퀴 = 사용자가 놀기 → 피드백 → 고치기 → 새 APK 판 → 기록.** 판 번호(v12, v13…)가 반복 번호다.
목표 플랫폼은 PC(Windows) + 안드로이드다. 2026-10-03 까지는 WebGL zip 이었다 — 옛 절차는 git 이력.

## 순서

| # | 무엇 | 누가 |
| --- | --- | --- |
| 1 | 피드백 받아 적기 | AI |
| 2 | 갈래 나누기 + Won't 대조 | AI 제안 · **사용자 확정** |
| 3 | 기록 파일 만들기 (피드백·결정까지) | AI |
| 4 | 갈래별로 고치기 (병렬) | 서브에이전트 |
| 5 | 검토 · 컴파일 0 · lint 0 | AI (메인) |
| 6 | 안드로이드 APK 빌드 | 서브에이전트 |
| 7 | APK 올리기 또는 폰에 설치 | **사용자** |
| 8 | 기록 파일 마무리 · 색인 · mem · 커밋 승인 | AI |

### 1. 피드백 받아 적기

- 사용자 말을 **그대로** 번호 목록으로 적는다. 요약하거나 고쳐 쓰지 않는다.
- 새 기록 파일을 `Docs/History/반복/00-틀.md` 복사로 만든다. 이름은 `NN-YYYY-MM-DD.md` (NN 은 반복 번호 두 자리).
- 놀이 시간·놀았던 판 번호를 물어보지 않는다. 사용자가 말했으면 적고, 아니면 「안 잼」.

### 2. 갈래 나누기 + Won't 대조

- 갈래는 다섯 : `[코드]` `[그림]` `[소리]` `[VFX]` `[기획]`. 한 피드백이 둘에 걸치면 둘 다 붙인다.
- 「이거 있었으면」 류는 `Docs/Design/02-기능목록.md` **Won't 표에 먼저 대 본다.**
  - Won't 에 있다 → 「안 한다」로 적고 사용자에게 알린다. 뒤집는 것은 사용자 몫.
  - 없다 → 사용자에게 넣을지 묻는다. 넣으면 기능목록에 한 줄 더한다. **3단계로 돌아가지 않는다.** HTML 프로토도 새로 안 만든다.
- 표를 사용자에게 보여 **고칠 것 · 미룰 것 · 안 할 것** 을 확정받는다. 여기서 한 번 멈춘다.

### 3. 기록 파일

- 「피드백」 「결정」 절을 채운다. 「고친 것」 이하는 4~6 뒤에 채운다.

### 4. 갈래별로 고치기

- **갈래가 다르면 서브에이전트를 한 메시지에서 같이 띄운다.** 같은 갈래 안은 하나로 묶는다.
- **한 폴더는 한 갈래만.** 코드는 `Assets/_Project/Scripts/`, 그림은 `Assets/_Project/Resources/Art/` 와 `Docs/Reference/Art/`, 소리는 `Assets/_Project/Resources/Audio/` (아직 없다 — 첫 소리 갈래가 만든다).
- **UI 프리팹과 씬(`Assets/_Project/Resources/Prefabs/` · `Assets/_Project/Scenes/`)은 코드 갈래만 만진다.** 다른 갈래가 프리팹 값을 바꿔야 하면 「넘긴 것」 표에 적어 코드 갈래가 합친다.
- 모델 : 설계·새 핵심 코드는 Opus(`general-purpose`), 짧은 수정·반복은 로컬 (`CLAUDE.md` 기준). 로컬은 순차.
- 서브에이전트 프롬프트에 꼭 넣는 것 (서브는 이 대화를 못 본다) :
  1. 피드백 원문과 확정된 결정
  2. 내 갈래의 폴더와 **건드리면 안 되는 자리** (위 규칙)
  3. `AGENTS.md` 규칙 요약 — 한국어 · 설명 수준 · 흐름 배선은 코드에 · ScriptableObject 는 수치 통만
  4. 코드면 `Tools/CodeTool/Docs/Guide/코딩컨벤션.md` 를 먼저 읽을 것
  5. 끝낼 때 컴파일 확인 (`unity command` 로 에러 0) · `stage lint <고친 파일>` 종료 0
  6. 돌려줄 것 여섯 (`report-format` 스킬) — 특히 **넘긴 것 / 안 건드린 자리**
  7. mem 은 `--scope` 를 정해 주고 `.claude/skills/mem-usage/SKILL.md` 경로를 같이 적는다
- 서브의 「다 됐다」를 믿지 않는다. 메인이 diff 를 본다.

### 5. 검토

- 갈래를 다 받으면 「넘긴 것」을 코드 갈래(또는 메인)가 합친다.
- 컴파일 에러 0 · `Tools/GamedesignTool/bin/stage.exe lint <고친 파일…>` 종료 0 을 메인이 직접 확인한다.
- 그림이 바뀌었으면 Play 모드 화면을 한 번 찍어 본다 (`capture_game_view --source screen`, 결과는 `Assets/Temp` 에 떨어지니 지운다).
- 표 데이터를 고쳤을 때 : `ProjectMozziUnity/GameData/*.json` 을 고치면 `Tools/DataTool/bin/datatool.exe` 의 validate → export → gen (또는 Unity 메뉴 `Mozzi/Data/Validate+Export+Gen`) 을 돌린다.

### 6. 안드로이드 APK 빌드

서브에이전트에게 맡긴다. 눈금은 **서브 10분 + 검토 3분** (2026-10-03 실측 — 빌드 404초).

1. Play 모드를 끈다. 다른 세션이 같은 에디터를 쓰고 있으면 먼저 알린다 — 빌드 중 다른 Unity 명령은 전부 시간 초과다.
2. 판을 올릴 때는 `Scripts/Editor/AndroidBuild.cs` 의 `Version` 상수를 먼저 고친다.
3. `unity command eval_file --detach` 로 한 줄짜리 .cs 를 넘긴다 : `Mozzi.Editor.AndroidBuild.Build();`
4. 끝은 `Editor.log` 의 `[AndroidBuild] 결과` 줄로 본다. Succeeded · 오류 0 을 확인한다.
5. 산출물은 `Build/itch/ProjectMozzi-android-vNN.apk` (gitignore). **zip 은 안 만든다.**
6. 바이트 수를 기록 파일 머리 표에 적는다. `ProjectSettings` 가 더러워지지 않았는지 `git status` 로 본다.

### 7. APK 올리기 또는 설치 (사용자)

- 사용자가 APK 를 itch.io 에 올리거나 폰에 직접 옮겨 설치한다.
- AI 는 APK 경로와 판 번호를 알리고 멈춘다. AI 는 itch.io 에 못 올린다.

### 8. 마무리

1. 기록 파일의 「고친 것」 「미룬 것」 「넘긴 것」 을 채운다.
2. `Docs/Todo/진행상황.md` 「반복 기록」 표에 한 줄 (회 · 날짜 · 판 · 한 줄).
3. 고친 시스템이 있으면 `Docs/Design/03-시스템-*.md` 에 **바뀐 것만** 적는다. 미리 쓰지 않는다.
4. `mem add --type history --scope game` 1건 — 왜 · 무엇을 · 다음에. 눈금이 어긋났으면 까닭 한 줄.
5. **커밋은 사용자 승인 뒤.** 반복 1회 = 커밋 1번 (기록 + 코드 + 그림). 메시지는 「반복 NN — 한 줄」.

## 하지 않는 것

- `stage ask` · `stage check` · 판정 기준 숫자. 기준은 사용자가 느낀 것이다.
- 수치표 「확정」 · 부재료 이름 「확정」. 어색하면 그때 고친다.
- 피드백에 없는 것을 끼워 고치기. 눈에 띄면 「미룬 것」에 적고 사용자에게 말한다.
- 반복 끝 기준 정하기. 사용자가 「됐다」 할 때 모양을 보고 적는다.

## 공수 눈금 (실측에서)

| 소단계 | 눈금 | 근거 |
| --- | --- | --- |
| 지목된 고침 4건 + 빌드 + 게시 | 서브 14분 + 메인 5분 | 09-21 실측 (mem `20260921-4794a351`) |
| APK 빌드 | 서브 10분 + 검토 3분 | 2026-10-03 실측 — 빌드 404초 |
| 그림 한 벌 (PixelLab 또는 PIL) | 사람이 봐야 끝 — 눈금 없음 | 그림은 판정이 사용자 몫 |
