# ProjectMozzi2

2D 게임 프로젝트다. 기획부터 다시 쓰고 새 Unity 프로젝트로 프로토타입을 만든다.

## 폴더 지도

| 폴더 | 무엇 |
| --- | --- |
| `Docs/` | 기획·설계·조사·할 일 문서. 참고 자료는 `Docs/Reference/` |
| `Tools/` | 툴 여덟 (git 서브모듈). DiagramTool · ArtTool · SoundTool · CodeTool · EffortTool · AIMemoryTool · ProtoTool · GamedesignTool |
| `ProjectMozziUnity/` | Unity 프로젝트 **(아직 없다. 사용자가 Unity Hub 로 만든다)** |
| `Memory/` | AI 기억 저장소 (`mem`). 결정·주의·기록이 여기 쌓인다 |

## 처음 받을 때

```
git clone --recurse-submodules https://github.com/MiruSona/ProjectMozzi2.git
git lfs install
```

이미 받았는데 `Tools/` 가 비어 있으면 :

```
git submodule update --init --recursive
```

그림·소리·영상은 **Git LFS** 로 받는다. `git lfs install` 을 안 하면 파일 대신 짧은 글자만 들어온다.

## 시작하려면

`Docs/Todo/진행상황.md` 부터 읽는다. 규칙은 `AGENTS.md` (Claude Code 는 `CLAUDE.md` 도).
