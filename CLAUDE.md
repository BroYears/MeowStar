# CLAUDE.md

This file provides comprehensive guidance to Claude Code (claude.ai/code) when working with code in this repository. It combines core LLM behavior principles with project-specific rules to ensure accurate, high-quality, and non-destructive development.

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

## 🤖 2. Core Behavioral Guidelines (기본 AI 행동 원칙)

> **Tradeoff:** 아래 지침은 속도보다 **신중함과 정확성**에 무게를 둡니다.

### 2.1 Think Before Coding (코딩 전 생각하기)
*   **가정하지 말고 혼란을 숨기지 마십시오.** 불확실한 부분이 있다면 구현 전에 명시적으로 질문하십시오.
*   다양한 해석이 가능하다면 독단적으로 선택하지 말고 가능성들을 제시하십시오.
*   더 단순한 접근법이 있다면 적극적으로 제안하고, 타당한 근거가 있다면 사용자의 의견에 피드백(Push back)을 제공하십시오.

### 2.2 Simplicity First (단순함 최우선)
*   요청받은 기능을 해결하는 **최소한의 코드**만 작성하십시오. 추측성 기능 추가는 금지합니다.
*   단일 사용 코드에 대한 과도한 추상화나 요청되지 않은 설정 유연성을 부여하지 마십시오.
*   불가능한 시나리오에 대한 과도한 예외 처리는 지양하며, 코드의 비대화를 방지하십시오.

### 2.3 Surgical Changes (외과 수술식 변경)
*   **요청받은 수정 범위만 정확히 터치하십시오.** 주변의 정상적인 코드, 주석, 포맷팅을 임의로 개선하려 하지 마십시오.
*   망가지지 않은 코드를 임의로 리팩토링하지 마십시오. 본인의 선호 스타일이 있더라도 기존 프로젝트 컨벤션을 엄격히 준수하십시오.
*   본인의 수정으로 인해 발생하는 데드 코드만 제거하고, 기존의 무관한 데드 코드는 언급만 하되 직접 삭제하지 마십시오.

### 2.4 Goal-Driven Execution (목표 중심 수행)
*   작업을 항상 검증 가능한 목표(예: 실패하는 테스트 작성 후 성공시키기)로 변환하여 접근하십시오.
*   다단계 작업 시에는 명확한 단계별 계획과 검증 체크리스트를 선제적으로 수립하십시오.

---

## 🎯 3. Project Overview

**MeowStar**는 세계수의 성장, 지역 해금 및 이동, 재화 수집 등을 다루는 Unity 6 기반의 타이쿤/시뮬레이션 형태의 게임 프로젝트입니다.
비즈니스 로직과 Unity 엔진 컴포넌트 간의 결합도를 낮추고 테스트 가능성을 극대화하기 위해, 핵심 로직은 순수 C# (POCO) 서비스로 설계하고 Unity MonoBehaviour 및 UI는 이를 래핑/구독하는 구조를 가집니다.

---

## 🚀 4. Build & Test Commands

프로젝트의 C# 코드 컴파일 유효성 및 테스트 검증은 다음 명령을 사용합니다:

```bash
# ── Unity EditMode Unit Tests 실행 (NUnit 검증) ──────────────────────────────
"/Applications/Unity/Hub/Editor/6000.4.9f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -runTests \
  -projectPath . \
  -testPlatform editmode \
  -testResults Logs/editmode-results.xml \
  -logFile -
```

---

## 🛠 5. Tech Stack

*   **Game Engine:** Unity 6 (6000.4.9f1)
*   **Scripting Language:** C# 12 / .NET Standard 2.1 호환 API
*   **Unit Test:** Unity Test Framework (NUnit)
*   **Architecture Pattern:** Service-Manager Pattern (POCO Service + MonoBehaviour Wrapper)

---

## 🏗 6. Architecture & Code Conventions

### 6.1 POCO & MonoBehaviour Separation (순수 C#과 엔진 분리)
*   **POCO (Plain Old C# Object) Service**: 게임 로직, 상태 전환 계산, 비용 소모 로직은 `MonoBehaviour`를 상속하지 않는 순수 C# 클래스(`Service`)로 작성합니다. 이를 통해 Unity 씬 로딩 없이 초고속 EditMode 테스트가 가능해야 합니다.
*   **MonoBehaviour Manager**: 씬 진입점 및 영속 싱글턴 역할을 하며, 외부 세이브/로드 시점에 POCO Service를 초기화하고 씬 내부의 UI/시각 연출 컴포넌트가 참조할 수 있도록 브릿지 역할을 수행합니다.
*   **Event-Driven Decoupling**: 로직 상태 변화(레벨업, 재화 변화 등)는 C# `event`를 사용하여 발행(Publish)하고, UI 및 사운드/이펙트 컴포넌트는 이를 구독(Subscribe)하여 결합도를 낮춥니다.

### 6.2 Data & Configuration (설정과 상태 분리)
*   **ScriptableObject (Config)**: 밸런싱 데이터(레벨업 비용, 템플릿 정보)는 기획자가 인스펙터에서 수정할 수 있는 `ScriptableObject`에 선언합니다. 런타임에는 이 데이터를 **읽기 전용(Read-Only)**으로 취급합니다.
*   **Plain C# Data Class (State)**: 저장 및 복구가 필요한 동적 런타임 데이터는 `[Serializable]` 어트리뷰트가 적용된 순수 C# 클래스로 분리합니다.

---

## 🤖 7. AI Agent Ecosystem (에이전트 라우팅 맵)

이 프로젝트는 관심사가 분리된 멀티 에이전트 아키텍처로 개발됩니다. 코드를 작성하거나 수정할 때, 작업 성격에 맞춰 반드시 `agents/prompts/` 내의 전용 지시어 문서를 최우선으로 읽고 해당 규칙에 바인딩(Binding)되어야 합니다.

*   **POCO Logic (순수 로직 및 테스트 개발):** [01_poco_logic.md](file:///Users/broyears/Documents/GitHub/MeowStar/agents/prompts/01_poco_logic.md)를 참조하여 Unity 의존성이 없는 순수 C# 서비스 및 단위 테스트 작성.
*   **Unity Bridge (MonoBehaviour 및 씬 연결):** [02_unity_bridge.md](file:///Users/broyears/Documents/GitHub/MeowStar/agents/prompts/02_unity_bridge.md)를 참조하여 매니저 라이프사이클 관리 및 컴포넌트 간 이벤트 발행/구독 연계 구현.
*   **Config Scaffolding (기획 설정 및 데이터 모델):** [03_config_scaffolding.md](file:///Users/broyears/Documents/GitHub/MeowStar/agents/prompts/03_config_scaffolding.md)를 참조하여 ScriptableObject 설계 및 Serializable 데이터 구조 생성.

*주의: 각 에이전트의 역할 범위를 침범하는 코드(예: POCO Logic 에이전트가 MonoBehaviour를 상속하는 행위 등)를 작성해서는 안 됩니다.*
