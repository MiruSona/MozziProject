# 아티팩트용 WebGL 로딩 페이지

`index.html` 은 **claude.ai 아티팩트에 올려 폰으로 게임을 돌려 보는 페이지**다.
2026-09-20 에 폰(LTE)에서 로딩이 자꾸 멈추던 것을 푼 판(v10)을 그대로 떠 왔다.
아티팩트를 새로 만들면 이 고침이 사라지므로 여기에 보관한다.

- 출처 : <https://claude.ai/artifact/9MNyDKwFET7oM9DojGKDGZ> 의 `index.html`
- 관련 기억 : `20260920-f5fe6235` (막힘 실측) · `20260920-39d7aa99` (올리는 손 흐름) · `20260920-c3b896f3` (크기 줄이기)

## 왜 이 페이지가 필요한가

**Unity 로더에는 재시도가 없다.** 큰 파일 셋을 한꺼번에 받다가 하나가 끊기면 그대로 영영 멈춘다.
회선이 약한 폰에서 8MB 급 파일은 자주 끊긴다.

그래서 받는 일을 로더에게 맡기지 않고 **페이지가 직접 한다.**

1. 파일을 **한 개씩 순서대로** 받는다 (틀 → 자료 → 엔진).
2. **30초 동안 한 바이트도 안 늘면 끊고 그 파일만 처음부터 다시** 받는다 (최대 6번).
3. 다 받으면 브라우저 캐시가 차 있다. 로더에게는 **같은 주소를 그대로** 준다 — 로더는 캐시에서 집어간다.

`blob:` 주소로 바로 넘기는 길은 **아티팩트 보안 정책(`connect-src`)이 막는다.** 그래서 캐시를 채우는 방식이다.

## 건드리면 안 되는 것

- **Brotli 압축 + `decompressionFallback = true` 는 한 짝이다.**
  아티팩트 서버는 `Content-Encoding` 헤더를 안 준다. 브라우저가 스스로 못 푸니 Unity 가 자바스크립트로 푼다.
  폴백을 끄면 「파일이 깨졌다」로 실패한다. (설정 자리는 `Assets/_Project/Scripts/Editor/WebGlBuild.cs`)
- **`cacheControl: "no-store"`** — 로더의 IndexedDB 캐시를 끈다. 우리가 이미 브라우저 캐시를 채워 뒀다.
- **파일당 15MB 상한** 이 있다. 지금 엔진이 8.78MB 라 여유가 있지만, 한 파일이 이 선을 넘으면 올릴 수 없다.
  줄일 거리는 기억 `20260920-c3b896f3` 에 있다.
- 아티팩트는 `.unityweb` 확장자를 **거절한다.** 세 바이너리는 `.wasm` 이름으로, `contentType` 은
  `application/wasm` 으로 올린다. 이름 바꿔 올리는 짝은 아래 표와 같다.

| 빌드 산출물 (`Build/WebGL/Build/`) | 아티팩트에 올리는 이름 |
| --- | --- |
| `WebGL.framework.js.unityweb` | `framework.wasm` |
| `WebGL.data.unityweb` | `data.wasm` |
| `WebGL.wasm.unityweb` | `code.wasm` |
| `WebGL.loader.js` | `loader.js` |

## 새 빌드를 올릴 때 손대는 자리

`index.html` 안에서 빌드마다 바뀌는 값은 넷뿐이다.

| 자리 | 지금 값 | 무엇인가 |
| --- | --- | --- |
| `var BASE` | `"Build/v8/"` | 올린 파일이 놓인 폴더. **판 번호를 꼭 올린다** |
| `PARTS[].size` | 76650 · 4210186 · 8780874 | 파일 크기(바이트). 진행 표시에만 쓴다 |
| `var TOTAL` | 13067710 | 세 파일 크기의 합 |
| `productName` 등 | `ProjectMozziUnity` | 바뀔 일은 거의 없다 |

**`BASE` 의 판 번호를 안 올리면 폰에 남은 옛 파일이 그대로 쓰여 「안 고쳐진 것」처럼 보인다.**
올리고 나면 옛 경로는 `files` 에 `null` 을 줘 내린다.
주소를 쓰는 곳이 다섯(로더 `src` · `dataUrl` · `frameworkUrl` · `codeUrl` · 진행 표시)인데
`BASE` 한 곳에 모아 뒀으니 **거기만 바꾼다.**

## 한 가지 더

페이지 맨 앞 `<head>` 의 짧은 스타일 한 줄은 아티팩트 쪽이 붙인 것이다. 떠 온 그대로 두었다.
