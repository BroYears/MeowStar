#!/bin/bash
# agents/scripts/generate-config.sh

CLASS_NAME=$1
DESCRIPTION=$2

if [ -z "$CLASS_NAME" ]; then
  echo "Usage: ./generate-config.sh [ClassName] '[Description]'"
  exit 1
fi

claude "주어진 C# 클래스명(${CLASS_NAME})과 설정을 바탕으로 ScriptableObject 또는 Serializable 데이터 모델 클래스를 생성해줘. 상세설명: ${DESCRIPTION}" \
  --file agents/prompts/Agent_Structure.md \
  --file agents/prompts/03_config_scaffolding.md
