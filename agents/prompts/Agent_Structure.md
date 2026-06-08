# MeowStar - AI Agent Harnessing Structure

이 문서는 프로젝트 내 `agents/` 폴더의 구조와 각 에이전트의 역할을 정의합니다. 클로드 CLI(Claude Code) 등을 사용할 때 이 구조를 기반으로 명령을 내리세요.

## 📂 Directory Structure

```text
MeowStar/ (Workspace Root)
│
├── 🤖 agents/                      <-- [AI 에이전트 전용 작업 공간]
│   ├── 📝 prompts/                 <-- 에이전트별 '시스템 프롬프트' 및 규칙 보관
│   │   ├── Agent_Structure.md      <-- [이 문서] 에이전트 구조 정의서
│   │   ├── 01_poco_logic.md        <-- Track A: 순수 C# 비즈니스 로직 및 테스트 지시어
│   │   ├── 02_unity_bridge.md      <-- Track B: MonoBehaviour 매니저 및 씬 연동 지시어
│   │   └── 03_config_scaffolding.md <-- Track C: ScriptableObject 및 데이터 모델 지시어
│   │
│   ├── ⚙️ scripts/                 <-- 에이전트 구동용 CLI 스크립트 (.sh)
│   │   ├── generate-poco.sh
│   │   ├── generate-bridge.sh
│   │   └── generate-config.sh
│   │
│   └── 📦 output/                  <-- AI가 생성한 코드 임시 저장소 (Sandbox)
```

---

## 🤖 Agent Roles

### 1. Track A: POCO Logic (C# Domain Model & Tests)
- **목적**: Unity 엔진 API(`MonoBehaviour` 등)에 의존하지 않는 순수한 C# 비즈니스 로직(POCO), 서비스 클래스, 인터페이스, 그리고 NUnit 단위 테스트 생성.
- **제약**: `UnityEngine` 네임스페이스 수입 금지, 씬 오브젝트 직접 조작 금지. 오직 비즈니스 상태 변화 계산과 이를 완벽히 커버하는 단위 테스트 작성에 집중.

### 2. Track B: Unity Bridge (MonoBehaviour & Event Routing)
- **목적**: Track A에서 작성된 순수 C# 서비스를 감싸는 `MonoBehaviour` 매니저 클래스 및 씬 연동 코드 작성.
- **제약**: 핵심 로직 연산(예: 비용 차감 공식, 레벨업 규칙 등)을 직접 연산하지 않고 Track A 서비스에 위임. UI 및 시각/사운드 연출과의 디커플링을 위해 C# `event`를 설계하고 포워딩하는 역할에 집중.

### 3. Track C: Config & Data Scaffolding (ScriptableObject & Data)
- **목적**: 기획 데이터 조정을 위한 `ScriptableObject`(Config) 클래스, 그리고 세이브/로드 및 데이터 전송을 위한 직렬화 구조체/클래스(`[Serializable]`) 생성.
- **제약**: 가변 상태와 정적 기획 설정을 명확히 구분하여 설계.

---

## 🚀 Workflow

1. `agents/prompts/`에 있는 규칙과 지시어를 클로드 또는 AI 개발 환경에 컨텍스트로 제공합니다.
2. AI가 제안한 코드를 `agents/output/` 디렉토리에 생성하여 1차적으로 파일 생성 상태와 논리적 구조를 확인합니다.
3. 검증된 코드를 `Assets/Scripts/` 및 `Assets/Tests/` 아래의 올바른 네임스페이스 폴더로 복사/머지합니다.
4. `CLAUDE.md`의 `Unity EditMode Tests` 명령어를 실행하여 단위 테스트가 정상 통과하는지 CLI 터미널에서 2차 검증을 수행합니다.
