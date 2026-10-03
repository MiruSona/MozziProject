# 가판대 화면 시안 v6 — 손님이 어디를 보나 견주기 · 부품

> 2026-10-03 반복 03 · **시안용이다. 게임에 넣지 않았다. 사용자 판정 대기.**
> 시안 그림 : `Docs/Reference/Art/시안/2026-10-03-가판대화면-v6-손님옆모습.png` (지금 · 옆모습 · 3/4 뒷모습, 9:16 같은 배율)
> 스크립트 : `build_screens_v6.py` (v5 스크립트를 복사해 고쳤다. v4 스크립트를 불러 쓴다)

## 결론

- **v5 안쪽 버전에서 손님 그림만 바꿨다.** 배경 · 주인 · 진열장 · 차림표 · 아이콘 자리는 v5 와 같다.
- **옆모습** : 맨 앞 손님(파란 후드)은 오른쪽(주인 쪽)을, 둘째 손님(초록 점퍼)은 왼쪽(계산대 쪽)을 본다.
- **3/4 뒷모습(덤)** : 둘 다 몸을 계산대 쪽으로 돌렸다. 얼굴은 옆선만 조금 보인다.
- 도트 굵기는 v5 와 같다 (270×480 을 ×2). 구도만 견주는 판이라 1px 로 다시 그리지 않았다.

## 판마다 바뀐 것

| 판 | 맨 앞 손님 | 둘째 손님 | 주문 말풍선 |
| --- | --- | --- | --- |
| 지금 (`front`) | v3 `customer_big` (앞) | v4 `customer_big_b` (앞) | 머리 위 가운데 |
| 옆모습 (`side`) | `customer_big_side` (오른쪽을 봄) | `customer_big_b_side` (왼쪽을 봄) | 머리 위에서 오른쪽(얼굴 앞)으로 22 |
| 3/4 뒷모습 (`back34`) | `customer_big_back34` (오른쪽 위) | `customer_big_b_back34` (왼쪽 위) | 오른쪽으로 16 |

- 손님 자리는 v5 와 같다. 맨 앞 x 100, 둘째 x 218, 발 y 560 (세계 좌표 272×608).

## 부품

| 부품 | 크기 | 어디서 (씨앗) | 손질 (PIL) |
| --- | --- | --- | --- |
| `customer_big_side` | 69×145 | `edit_image_pro_flash` 글 편집 · 바탕 = v3 `customer_big_14101` 원본 · 18101 | 자르기 |
| `customer_big_back34` | 64×145 | 같은 법 · 바탕 = 18101 판의 `source_image_id` · 18102 | 자르기 |
| `customer_big_b_side` | 68×151 | 같은 법 · 바탕 = v4 `customer_big_15201` 원본 · 18201 | 자르기 |
| `customer_big_b_back34` | 68×151 | 같은 법 · 바탕 = 18201 판의 `source_image_id` · 18202 | 자르기 |
| 배경 · 주인 · 진열장 · 아이콘 · 말풍선 | — | v3 · v4 · v5 부품 그대로 (배경 원본은 `../v5/raw/` 를 읽기만 한다) | v5 와 같음 |

- 네 장 다 128×160 캔버스, 배경 없음(`no_background`)으로 받았다. 한 장 6회, 모두 첫 판에 썼다 (버린 후보 없음).
- 3/4 뒷모습 둘은 옆모습 판을 낼 때 돌려받은 `source_image_id` 로 편집했다. 이 id 가 올린 원본인지 옆모습 결과인지는 도구가 밝히지 않는다 (둘 다 쓸 만하게 나왔다).

### 프롬프트 (method=text)

- 옆모습 (파란 후드) : `Same character, same size, same art style, same outfit (blue hoodie, dark navy jeans, white sneakers), same short brown hair. Change only the pose: the character now stands in side view, body and face turned to the right (facing right), seen in profile, standing still and waiting at a shop counter. Keep the same height and proportions.`
- 3/4 뒷모습 : 같은 머리말 + `Change only the pose: the character is now seen in three-quarter back view, mostly showing the back of the head and back of the hoodie, body turned away from the viewer toward the upper right, face turned to the right so a little of the cheek and profile is visible, standing still waiting at a shop counter. Keep the same height and proportions.`
- 초록 점퍼는 옷 설명을 `green zip jacket over white shirt, dark gray jeans, white sneakers` · `same messy black hair` 로, 방향을 left / upper left 로 바꿨다.

## 깨끗하지 않은 곳

- **옆모습 판에서 두 손님이 서로 마주 본다.** 둘 다 주인 쪽을 보게 돌렸는데, 주인이 위에 있어서 눈높이로는 서로를 보는 것처럼 읽힌다.
- **옆모습은 시선이 수평이다.** 주인은 위(계산대 뒤)에 있어서 올려다보는 느낌은 없다.
- 3/4 뒷모습은 얼굴이 거의 안 보여 기다림 · 시무룩 표정을 쓰기 어렵다. 손님 겹 그림(표정 셋)을 이 방향으로 다시 뽑아야 한다.
- 옆모습 · 3/4 뒷모습은 겹 그림이 아닌 한 장이다 (지금 큰 손님과 같다).
- 그 밖의 흠(9:20 천장 띠 · 베레모가 칠판을 조금 가림 등)은 v5 와 같다.
