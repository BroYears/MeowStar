# M1 그레이박스 프로토타입 — 세팅 가이드

GDD v0.2 섹션 12(M1) 구현용 스크립트 11개. 큐브·캡슐만으로 코어 루프 1바퀴를 검증한다:
**강에서 생선 줍기 → 그릴에 투입 → 구이 픽업 → 손님 서빙 → 돈 줍기 → 건설 존에서 확장**

## 0. 프로젝트 준비
1. Unity 2022 LTS, 3D (URP) 템플릿으로 프로젝트 생성.
2. 이 `UnityPrototype_M1` 폴더를 통째로 `Assets/_Project/Scripts/`에 복사.
3. **Edit > Project Settings > Player > Active Input Handling = "Both"** (또는 Input Manager(Old)). 레거시 Input 사용.

## 1. 플레이어 (캡슐)
```
Player (Capsule)
├─ CharacterController (Radius 0.4, Height 1.8)
├─ PlayerController.cs
├─ StackHolder.cs
└─ StackAnchor (빈 오브젝트, 위치 = 머리 위 약 Y+2.2)
```
- StackHolder의 `stackAnchor`에 StackAnchor 할당, `capacity = 3`.
- 기본 Capsule Collider는 **삭제** (CharacterController가 콜라이더 역할).
- CharacterController는 트리거 존과 자동으로 충돌 이벤트가 발생하므로 Rigidbody 불필요.

## 2. 카메라
- Main Camera: Rotation X ≈ 50°, 플레이어 뒤·위에 고정 배치.
- 빈 오브젝트에 간단한 팔로우 스크립트를 붙이거나, M1에서는 맵이 작으니 **고정 카메라**로도 충분.

## 3. 스택 아이템 프리팹 2종
- `Fish` : 작은 Cube(0.5×0.25×0.8, 파란색) + `StackItem.cs` (type = Fish) + BoxCollider(isTrigger ✔)
- `GrilledFish` : 같은 크기 Cube(주황색) + `StackItem.cs` (type = GrilledFish) + BoxCollider(isTrigger ✔)

## 4. 존 만들기 (공통 패턴)
존 = **바닥에 깔린 납작한 Cylinder/Plane + Collider(isTrigger ✔)**.
시각용 머티리얼은 반투명(URP/Lit, Surface = Transparent)으로 만들어 `zoneRenderer`에 할당.

### 4a. 채집 존 (강)
```
GatherZone_Fish (Cylinder, Scale 2.5/0.05/2.5, isTrigger)
└─ GatherZone.cs : itemPrefab = Fish, gatherInterval = 0.5
```

### 4b. 그릴 (조리 스테이션)
```
Grill (Cube 본체)
├─ Zone (자식, 납작 Cylinder, isTrigger) + CookStation.cs
│    inputType = Fish, outputPrefab = GrilledFish, cookTime = 2
│    outputSlots = 카운터 위 빈 오브젝트 3개 (완성품 놓일 자리)
└─ OutputSlot_1~3 (빈 오브젝트)
```

### 4c. 테이블 + 손님
```
Table (Cube)
├─ Zone (자식, 납작 Cylinder, isTrigger) + TableZone.cs
│    moneyPilePrefab = MoneyPile 프리팹
├─ SeatPoint (빈 오브젝트 — 손님이 서는 위치)
└─ MoneySpawnPoint (빈 오브젝트 — 지폐 더미 위치)
```
- `Customer` 프리팹: Capsule(노란색) + `Customer.cs`. **Collider의 isTrigger ✔** (플레이어와 안 부딪히게).
- `MoneyPile` 프리팹: 납작한 초록 Cube + `MoneyPile.cs` + BoxCollider(isTrigger ✔), amount는 코드가 설정.
- 빈 오브젝트에 `CustomerSpawner.cs`: tables 배열, spawnPoint/exitPoint(맵 가장자리), interval 5초.

### 4d. 건설 존
```
BuildZone_Table2 (납작 Cylinder, isTrigger) + BuildZone.cs
   totalCost = 100, targetToActivate = 미리 배치해둔 비활성 Table2
   costLabel = 자식 3D Text(TextMesh) — 남은 비용 표시
```

## 5. 매니저
빈 오브젝트 `_Managers`에 `EconomyManager.cs` + `GrayboxHUD.cs`.

## 6. 검증 체크리스트 (M1의 목적 — GDD 12 참조)
- [ ] 이동이 즉각적이고 답답하지 않은가? (`moveSpeed`, `joystickRadius` 튜닝)
- [ ] 스택이 기분 좋게 출렁이는가? (`followSmoothTime` 0.05~0.1 사이 튜닝)
- [ ] 생선 3개 들고 그릴→테이블 동선을 돌 때 "내가 일하는 맛"이 있는가?
- [ ] 돈 더미를 지나갈 때 줍는 쾌감이 있는가? (효과음 임시라도 넣어볼 것)
- [ ] 건설 존에서 돈이 빠져나가며 시설이 생길 때 만족스러운가?
- [ ] **재미없으면**: 아트가 아니라 수치(속도·간격·출렁임)부터 튜닝. 그래도 안 되면 피벗 논의.

## 7. M2에서 할 일 (이 코드의 의도된 한계)
- `ItemType` enum → ScriptableObject(IngredientData/MenuData) 교체
- 손님 직선 이동 → NavMeshAgent + 줄서기
- 인내심 말풍선·치우기(TrashZone)·직원 AI·세이브
- OnGUI HUD → 정식 UI (개발자 B)
