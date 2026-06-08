# ANTIGRAVITY.md

This file provides specific operational guidelines and system instructions for the **Antigravity** coding assistant when working on the MeowStar repository.

---

## 🧭 1. Planning Mode & Artifact Guidelines (계획 수립 및 산출물 규칙)

Antigravity는 변경의 성격과 규모에 따라 **Planning Mode(계획 모드)**를 적용하여 작업을 신중히 진행합니다.

### 1.1 계획을 수립해야 하는 경우 (When to Plan)
*   프로젝트 아키텍처에 중대한 변경이 발생하는 경우.
*   구현 요구사항이 모호하여 의사결정이 필요한 경우.
*   기존 설계 사상(POCO Service - MonoBehaviour Manager 분리 등)과 대조적인 변경인 경우.

위의 경우, 다음의 흐름을 따릅니다:
1.  **Research**: 소스 코드를 수정하지 않고 코드베이스와 Unity 설정을 조사합니다.
2.  **Create Implementation Plan**: `/Users/broyears/.gemini/antigravity/brain/<conv-id>/implementation_plan.md` 파일을 생성/업데이트하여 제안하고 사용자 승인을 기다립니다. (`request_feedback = true`)
3.  **Execute**: 승인 완료 후 `task.md`를 생성하여 작업 항목을 추적하며 작업을 수행합니다.
4.  **Verify**: NUnit EditMode 테스트를 실행하여 무결성을 검증하고 `walkthrough.md`를 작성하여 보고합니다.

### 1.2 계획을 수립하지 않는 경우 (When NOT to Plan)
*   질의/조사성 요청 (예: "X 로직이 어디에 구현되어 있나요?")
*   단순한 오타 수정, 구문 에러 수정, 주석 추가 등 단순 코드 패치 작업.
*   이미 승인된 계획의 사소한 피드백 반영.

---

## 💬 2. Communication Style (소통 및 링크 컨벤션)

*   **한국어(Korean) 작성**: 모든 설명과 보고는 친절한 한국어로 간결하게 작성합니다.
*   **파일 링크 활성화**: 답변 내에서 참조되는 모든 파일 경로와 코드 심볼은 반드시 `file://` 스키마가 포함된 마크다운 하이퍼링크로 만듭니다. (백틱 `` ` ``으로 링크를 감싸지 마십시오. 감싸면 링크 클릭이 불가능해집니다.)
    *   *올바른 예*: [WorldTreeService.cs](file:///Users/broyears/Documents/GitHub/MeowStar/Assets/Scripts/WorldTree/WorldTreeService.cs)
    *   *잘못된 예*: [`WorldTreeService.cs`](file:///Users/broyears/Documents/GitHub/MeowStar/Assets/Scripts/WorldTree/WorldTreeService.cs)
*   **답변 요약**: 작업 완료 시 수행된 내용을 핵심 요약하여 보고합니다.

---

## 🛠 3. Unity & C# Specific Instructions (Unity 및 C# 특화 지침)

Antigravity 에이전트는 MeowStar 프로젝트에서 코드를 생성하거나 리팩토링할 때 다음 지침을 반드시 준수해야 합니다.

### 3.1 Memory & GC Alloc Optimization (가비지 컬렉션 최적화)
*   **Update 루프 내 할당 금지**: `Update()` 또는 자주 호출되는 메서드 내에서의 `new` 연산자, `LINQ` 사용, 박싱(Boxing) 유발 코드를 금지합니다.
*   **구조체(Struct) 활용**: 임시 벡터 연산, 간단한 데이터 팩 등은 스택 영역을 사용하는 `struct` 또는 `readonly struct`로 설계하여 힙 할당을 방지합니다.
*   **Null 비교 최적화**: Unity `UnityEngine.Object`를 상속한 객체들의 Null 비교(`obj == null`)는 C# 순수 객체와 달리 네이티브 브릿지를 거쳐 무겁습니다. 가급적 핵심 로직 내부(POCO)에서는 Unity Object와 격리된 순수 C# 객체를 설계하십시오.

### 3.2 Async & Threading (비동기 프로그래밍)
*   Unity의 싱글 스레드 특성을 고려하여 코루틴(Coroutine)이나 `UniTask`/`Task`를 사용할 때 메인 스레드 컨텍스트를 침범하지 않도록 주의하고, 파괴(Destroy) 시점에 비동기 작업이 중단되도록 `CancellationToken`을 적절히 주입합니다.

### 3.3 Test-Driven Development (테스트 중심 개발)
*   새로운 핵심 비즈니스 로직(Track A)을 작성할 때 반드시 이에 매칭되는 NUnit EditMode 단위 테스트 코드를 `Assets/Tests/EditMode/` 하위에 작성하고, CLI 상에서 검증을 마칩니다.

---

## 🤖 4. Collaborative Agent Usage (하위 에이전트 활용)

Antigravity는 대규모 조사나 작업 분할이 필요한 경우 하위 에이전트(Subagent)를 활용할 수 있습니다.
*   **`research` subagent**: 대용량 코드 검색, 웹 문서 읽기 등 병렬 검색이 필요한 경우 백그라운드 위임.
*   **`self` subagent**: 현재 컨텍스트와 동일한 능력을 가진 별도 에이전트에게 샌드박스 독립 개발 위임.
*   하위 에이전트 실행 시 주기적인 폴링(Polling)을 하지 않고, 시스템 알림 메시지를 수신하여 비동기 처리합니다.
