#!/bin/bash
# agents/scripts/generate-poco.sh

CLASS_NAME=$1
DESCRIPTION=$2

if [ -z "$CLASS_NAME" ]; then
  echo "Usage: ./generate-poco.sh [ClassName] '[Description]'"
  exit 1
fi

claude "주어진 C# 클래스명(${CLASS_NAME})과 설정을 바탕으로 순수 C# 로직(POCO) 서비스 클래스 및 NUnit EditMode 단위 테스트 코드를 생성해줘. 상세설명: ${DESCRIPTION}" \
  --file agents/prompts/Agent_Structure.md \
  --file agents/prompts/01_poco_logic.md
