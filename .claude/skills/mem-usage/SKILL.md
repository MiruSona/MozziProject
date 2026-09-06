---
name: mem-usage
description: Use when starting work on any part of the game, after making a decision or hitting a pitfall, before a completion report, or when handing work to a subagent — 작업 시작 · 결정·함정 직후 · 완료 보고 전 · 서브에이전트에게 맡길 때 mem 에 찾고 남기는 법.
---

# 기억 저장소 (mem) 쓰기

**언제 무엇을 남기나**만 정한다. 명령의 자세한 꼴은 `mem help <명령>`, 설치·구조는 `Memory/사용법.md`.

## 언제 무엇을

| 이럴 때 | 이렇게 |
| --- | --- |
| 작업 시작 | `mem search <주제> <낱말>` 로 지난 결정·주의를 먼저 본다 |
| 결정을 내렸다 / 함정을 밟았다 | 그 자리에서 `mem add --type decision` 또는 `--type caution --severity …` |
| 소단계 끝 · 완료 보고 전 | `--type history` 1건(공수 포함) · 남은 일은 `--type todo`. 긴 조사·설계는 md 문서로 쓰고 mem 에는 한 줄(`--sources file:경로`) |
| 결정을 뒤집었다 | 새로 쓰지 말고 `mem set <옛id> --by <새id>` (새 기억이 아직 없으면 `--by-new`) |
| 환경 사실을 알았다 (Unity 판·기계·계정 같은 것) | `--type fact` — sources 필수, 바뀌면 `mem set 옛id --by 새id` 로 덮는다 |
| 서브에이전트에게 맡길 때 | `SubagentStart` 훅이 요약과 「`mem search` 로 시작 · `mem add` 로 끝내고 id 보고」 세 줄을 넣어 준다. 그래도 **`--scope` 값은 프롬프트에 적는다** — 훅은 목록만 보여주고 고르지 않는다. 이 스킬 경로(`.claude/skills/mem-usage/SKILL.md`)도 같이 적는다. 서브에이전트가 남긴 기억은 메인이 `mem show <id>` 로 확인한다 |

`--type` 은 일곱 : `decision` `caution` `issue` `todo` `history` `howto` `fact`.
`--title` · `--summary` · `--tags` · `--scope` 는 늘 필요하고,
`decision` · `issue` · `caution` · `fact` 는 `--sources` 도 있어야 한다.

## scope 는 다섯 중 하나

| scope | 무엇 |
| --- | --- |
| `game` | 게임 전체에 걸린 것 (일정·저장소 구조·툴 연결) |
| `design` | 기획 — 시스템·수치·재미 |
| `art` | 그림 · 스프라이트 · UI |
| `sound` | 효과음 · 음악 |
| `code` | Unity 코드 · 구조 |

목록 밖은 거절이다. 갈래가 늘면 먼저 `mem tags --add-scope <이름>`.
**툴 자체 이야기는 여기 남기지 않는다** — 그 툴 저장소의 `Docs/Todo/` 에 적는다.

## 안 넣는 것

- 코드를 보면 바로 아는 것
- 규칙 문서(`AGENTS.md` · 스킬)에 이미 있는 것
- 이번 세션에서만 쓸 메모
- 비밀값 (키 · 토큰 · 경로에 든 개인 정보)
- "~해라" 지시문 — 기억은 사실을 적는 자리다
- 기계 고유값(IP·MAC·제품키)은 `fact` 가 아니라 git 제외 `.local.md`

## 그밖에

- 확신이 없으면 `--hold` 로 넣는다. 사람이 볼 때까지 검색·훅에 안 뜬다.
- 저장하기 전에 보고 싶으면 `mem add --check` (관문만 돌리고 저장은 안 한다).
- 오류가 나면 작업을 멈추지 않는다. 보고에 「기억 못 넣음」 한 줄만 적는다.
