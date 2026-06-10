# 냥스타 키친 — 코드 프레임 (Code Skeleton)

형훈_GDD.md 기준으로 만든 Unity 게임 프레임입니다. 로직 스켈레톤만 포함하며,
실제 아트/씬/프리팹/AdMob SDK는 비어 있습니다.

## 구조

```
Scripts/
├─ Core/        GameManager, Singleton, GameEvents(이벤트 버스)
├─ Economy/     EconomyManager(골드/보석/재료/업그레이드), IdleIncomeManager(방치 수익)
├─ Hunting/     HuntingManager(사냥 미니게임 로직 + 콤보)
├─ Customer/    CustomerInstance(FSM), CustomerManager(스폰 스케줄러)
├─ Save/        SaveData, SaveManager(JSON 저장/오프라인 정산)
├─ Ads/         AdManager(AdMob 연동 스텁)
├─ UI/          UIManager(패널 스택), HudView(이벤트 구독 예시)  ← 개발자 B
└─ Data/        ScriptableObject 정의 + GameDatabase + Enums
```

## 부트스트랩 방법 (Unity 에디터에서)

1. 빈 `Bootstrap` 씬 생성.
2. 빈 GameObject `_Managers` 에 다음 컴포넌트를 모두 추가:
   `GameManager, SaveManager, EconomyManager, HuntingManager,
    CustomerManager, IdleIncomeManager, AdManager, UIManager`
   - **SaveManager 가 다른 매니저보다 먼저 Awake** 되도록 Script Execution Order
     에서 SaveManager 를 최상단으로 올린다 (Economy 등이 Awake 에서 SaveData 를 읽음).
3. `Create > Nyangsta > GameDatabase` 로 DB 에셋 생성, 콘텐츠 SO 들을 만들어 등록.
4. `GameManager`, `EconomyManager`, `HuntingManager`, `CustomerManager` 인스펙터의
   `database` 필드에 위 GameDatabase 에셋을 할당.

## 콘텐츠 데이터 만들기 (개발자 B)

`Create > Nyangsta > Ingredient / Menu / Customer / Upgrade` 로 GDD §5 테이블의
재료·메뉴·손님·업그레이드를 SO 자산으로 채운다. 각 자산의 `id` 는 고유해야 한다.

## 핵심 루프 연결

- 사냥: UI 가 `HuntingManager.StartHunt(region)` → 타겟 탭마다 `RegisterHit()` → 종료 시 자동 정산.
- 서빙: `CustomerManager` 가 자동으로 손님 스폰 + 선호 메뉴 자동 조리/판매.
- 업그레이드: UI 버튼 → `EconomyManager.TryPurchaseUpgrade(upgrade)`.
- HUD: `GameEvents.GoldChanged` 등 이벤트 구독 (HudView 참고).

## 테스트

`Tests/EditMode/EconomyMathTests.cs` — 비용 곡선 / 오프라인 정산 수식 검증
(Window > General > Test Runner > EditMode).

## TODO (다음 단계)

- 업그레이드 효과를 실제 매니저 수치에 반영 (조리 속도, 좌석 수, GPS 등).
- AdManager 에 Google Mobile Ads 플러그인 연동.
- 씬/프리팹/UI 패널 제작 (개발자 B).
- 지역 이동·세계수 시스템 (형년_GDD 컨셉) — 확장 시 추가.
```
