# ANTIGRAVITY.md

This file provides comprehensive guidelines, system instructions, and project context for the **Antigravity** coding assistant when working on the MeowStar repository.

---

## 👤 1. User Profile & Learning Goals (사용자 프로필 및 답변 규칙)

*   **Developer:** 내 이름은 형년이고, 이 프로젝트의 Unity C# 클라이언트 개발자야.
*   **CS 지식 학습 중:** 컴퓨터 공학 기초와 이론을 깊이 있게 학습하고 있으니, 설명할 때 정확하고 전문적인 CS 용어, 디자인 패턴, 메모리 관리(가비지 컬렉션, 구조체 vs 클래스), 비동기 처리(UniTask/Task), 데이터 구조 등을 가감 없이 사용해 줘. 모르는 용어는 내가 직접 찾아보며 공부할 거야.
*   **클라이언트 중심 사고:** 게임 클라이언트의 관점(예: 프레임 레이트, 메모리 최적화, GC Alloc 최소화, 이벤트 기반 디커플링, MVC/MVP 아키텍처 등)을 반영해서 설명해 주면 더 빠르게 이해할 수 있어.
*   **답변 스타일:**
  1.  **두괄식**으로 핵심 코드나 해결책을 먼저 제시해 줘.
  2.  수정된 부분만 파편적으로 주지 말고, 문맥을 파악할 수 있도록 적절한 범위의 **전체 코드를 함께 제공**해 줘.
  3.  에러 발생 시 **`원인 분석 -> 해결 코드 -> CS/아키텍처 관점의 간략한 이유`** 순으로 짚어 줘.
  4.  모든 설명은 반드시 **한국어(Korean)**로 작성해 줘. (단, 코드 문법이나 공식적인 CS/네트워크/Unity 엔진 용어는 영어를 혼용해도 무방함)
  5.  ⚠️ **코드는 필요하지 않으면 굳이 먼저 주지 말고, 내가 요청할 때만 제공해 줘.**

---

## 🧭 2. Planning Mode & Artifact Guidelines (계획 수립 및 산출물 규칙)

Antigravity는 변경의 성격과 규모에 따라 **Planning Mode(계획 모드)**를 적용하여 작업을 신중히 진행합니다.

### 2.1 계획을 수립해야 하는 경우 (When to Plan)
*   프로젝트 아키텍처에 중대한 변경이 발생하는 경우.
*   구현 요구사항이 모호하여 의사결정이 필요한 경우.
*   기존 설계 사상(POCO Service - MonoBehaviour Manager 분리 등)과 대조적인 변경인 경우.

위의 경우, 다음의 흐름을 따릅니다:
1.  **Research**: 소스 코드를 수정하지 않고 코드베이스와 Unity 설정을 조사합니다.
2.  **Create Implementation Plan**: `/Users/broyears/.gemini/antigravity/brain/<conv-id>/implementation_plan.md` 파일을 생성/업데이트하여 제안하고 사용자 승인을 기다립니다. (`request_feedback = true`)
3.  **Execute**: 승인 완료 후 `task.md`를 생성하여 작업 항목을 추적하며 작업을 수행합니다.
4.  **Verify**: NUnit EditMode 테스트를 실행하여 무결성을 검증하고 `walkthrough.md`를 작성하여 보고합니다.

### 2.2 계획을 수립하지 않는 경우 (When NOT to Plan)
*   질의/조사성 요청 (예: "X 로직이 어디에 구현되어 있나요?")
*   단순한 오타 수정, 구문 에러 수정, 주석 추가 등 단순 코드 패치 작업.
*   이미 승인된 계획의 사소한 피드백 반영.

---

## 💬 3. Communication Style (소통 및 링크 컨벤션)

*   **한국어(Korean) 작성**: 모든 설명과 보고는 친절한 한국어로 간결하게 작성합니다.
*   **파일 링크 활성화**: 답변 내에서 참조되는 모든 파일 경로와 코드 심볼은 반드시 `file://` 스키마가 포함된 마크다운 하이퍼링크로 만듭니다. (백틱 `` ` ``으로 링크를 감싸지 마십시오. 감싸면 링크 클릭이 불가능해집니다.)
    *   *올바른 예*: [WorldTreeService.cs](file:///Users/broyears/Documents/GitHub/MeowStar/Assets/Scripts/WorldTree/WorldTreeService.cs)
    *   *잘못된 예*: [`WorldTreeService.cs`](file:///Users/broyears/Documents/GitHub/MeowStar/Assets/Scripts/WorldTree/WorldTreeService.cs)
*   **답변 요약**: 작업 완료 시 수행된 내용을 핵심 요약하여 보고합니다.

---

## 🎯 4. Project Overview & Tech Stack

**MeowStar**는 세계수의 성장, 지역 해금 및 이동, 재화 수집 등을 다루는 Unity 6 기반의 타이쿤/시뮬레이션 형태의 게임 프로젝트입니다.
비즈니스 로직과 Unity 엔진 컴포넌트 간의 결합도를 낮추고 테스트 가능성을 극대화하기 위해, 핵심 로직은 순수 C# (POCO) 서비스로 설계하고 Unity MonoBehaviour 및 UI는 이를 래핑/구독하는 구조를 가집니다.

### 4.1 Tech Stack
*   **Game Engine:** Unity 6 (6000.4.9f1)
*   **Scripting Language:** C# 12 / .NET Standard 2.1 호환 API
*   **Unit Test:** Unity Test Framework (NUnit)
*   **Architecture Pattern:** Service-Manager Pattern (POCO Service + MonoBehaviour Wrapper)

### 4.2 Build & Test Commands
프로젝트의 C# 코드 컴파일 유효성 및 테스트 검증은 다음 명령을 사용합니다:
```bash
"/Applications/Unity/Hub/Editor/6000.4.9f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -runTests \
  -projectPath . \
  -testPlatform editmode \
  -testResults Logs/editmode-results.xml \
  -logFile -
```

---

## 🏗 5. Architecture & Code Conventions

### 5.1 POCO & MonoBehaviour Separation (순수 C#과 엔진 분리)
*   **POCO (Plain Old C# Object) Service**: 게임 로직, 상태 전환 계산, 비용 소모 로직은 `MonoBehaviour`를 상속하지 않는 순수 C# 클래스(`Service`)로 작성합니다. 이를 통해 Unity 씬 로딩 없이 초고속 EditMode 테스트가 가능해야 합니다.
*   **MonoBehaviour Manager**: 씬 진입점 및 영속 싱글턴 역할을 하며, 외부 세이브/로드 시점에 POCO Service를 초기화하고 씬 내부의 UI/시각 연출 컴포넌트가 참조할 수 있도록 브릿지 역할을 수행합니다.
*   **Event-Driven Decoupling**: 로직 상태 변화(레벨업, 재화 변화 등)는 C# `event`를 사용하여 발행(Publish)하고, UI 및 사운드/이펙트 컴포넌트는 이를 구독(Subscribe)하여 결합도를 낮춥니다.

### 5.2 Data & Configuration (설정과 상태 분리)
*   **ScriptableObject (Config)**: 밸런싱 데이터(레벨업 비용, 템플릿 정보)는 기획자가 인스펙터에서 수정할 수 있는 `ScriptableObject`에 선언합니다. 런타임에는 이 데이터를 **읽기 전용(Read-Only)**으로 취급합니다.
*   **Plain C# Data Class (State)**: 저장 및 복구가 필요한 동적 런타임 데이터는 `[Serializable]` 어트리뷰트가 적용된 순수 C# 클래스로 분리합니다.

### 5.3 Memory & GC Alloc Optimization (가비지 컬렉션 최적화)
*   **Update 루프 내 할당 금지**: `Update()` 또는 자주 호출되는 메서드 내에서의 `new` 연산자, `LINQ` 사용, 박싱(Boxing) 유발 코드를 금지합니다.
*   **구조체(Struct) 활용**: 임시 벡터 연산, 간단한 데이터 팩 등은 스택 영역을 사용하는 `struct` 또는 `readonly struct`로 설계하여 힙 할당을 방지합니다.
*   **Null 비교 최적화**: Unity `UnityEngine.Object`를 상속한 객체들의 Null 비교(`obj == null`)는 C# 순수 객체와 달리 네이티브 브릿지를 거쳐 무겁습니다. 가급적 핵심 로직 내부(POCO)에서는 Unity Object와 격리된 순수 C# 객체를 설계하십시오.

### 5.4 Async & Threading (비동기 프로그래밍)
*   Unity의 싱글 스레드 특성을 고려하여 코루틴(Coroutine)이나 `UniTask`/`Task`를 사용할 때 메인 스레드 컨텍스트를 침범하지 않도록 주의하고, 파괴(Destroy) 시점에 비동기 작업이 중단되도록 `CancellationToken`을 적절히 주입합니다.

### 5.5 Test-Driven Development (테스트 중심 개발)
*   새로운 핵심 비즈니스 로직(Track A)을 작성할 때 반드시 이에 매칭되는 NUnit EditMode 단위 테스트 코드를 `Assets/Tests/EditMode/` 하위에 작성하고, CLI 상에서 검증을 마칩니다.

---

## 🤖 6. AI Agent Ecosystem & Collaboration (에이전트 생태계 및 협업)

이 프로젝트는 관심사가 분리된 멀티 에이전트 아키텍처로 개발됩니다. 코드를 작성하거나 수정할 때, 작업 성격에 맞춰 반드시 `agents/prompts/` 내의 전용 지시어 문서를 최우선으로 읽고 해당 규칙에 바인딩(Binding)되어야 합니다.

*   **POCO Logic (순수 로직 및 테스트 개발):** [01_poco_logic.md](file:///Users/broyears/Documents/GitHub/MeowStar/agents/prompts/01_poco_logic.md)를 참조하여 Unity 의존성이 없는 순수 C# 서비스 및 단위 테스트 작성.
*   **Unity Bridge (MonoBehaviour 및 씬 연결):** [02_unity_bridge.md](file:///Users/broyears/Documents/GitHub/MeowStar/agents/prompts/02_unity_bridge.md)를 참조하여 매니저 라이프사이클 관리 및 컴포넌트 간 이벤트 발행/구독 연계 구현.
*   **Config Scaffolding (기획 설정 및 데이터 모델):** [03_config_scaffolding.md](file:///Users/broyears/Documents/GitHub/MeowStar/agents/prompts/03_config_scaffolding.md)를 참조하여 ScriptableObject 설계 및 Serializable 데이터 구조 생성.

*주의: 각 에이전트의 역할 범위를 침범하는 코드(예: POCO Logic 에이전트가 MonoBehaviour를 상속하는 행위 등)를 작성해서는 안 됩니다.*

### 6.1 하위 에이전트(Subagent) 활용 지침
Antigravity는 대규모 조사나 작업 분할이 필요한 경우 하위 에이전트(Subagent)를 활용할 수 있습니다.
*   **`research` subagent**: 대용량 코드 검색, 웹 문서 읽기 등 병렬 검색이 필요한 경우 백그라운드 위임.
*   **`self` subagent**: 현재 컨텍스트와 동일한 능력을 가진 별도 에이전트에게 샌드박스 독립 개발 위임.
*   하위 에이전트 실행 시 주기적인 폴링(Polling)을 하지 않고, 시스템 알림 메시지를 수신하여 비동기 처리합니다.
