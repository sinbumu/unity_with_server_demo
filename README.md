# 유니티-파이썬 연동 테스트

유니티 클라이언트가 로컬 백엔드(`dummy_backend.exe`)와 HTTP/WebSocket으로 통신하는 실험용 프로젝트입니다.  
유니티 실행 시 백엔드를 자동 실행하고, 간단한 상태 체크/채팅 호출 및 WS 메시지 송수신을 확인할 수 있습니다.

## 핵심 구성

- `Assets/Scripts/TestAppManager.cs`
  - 앱 시작 시 `StreamingAssets`의 `dummy_backend.exe`를 자동 실행
  - HTTP GET: `/health`, `/configview`
  - HTTP POST: `/chat` (JSON 바디 전송)
  - WebSocket: `ws://127.0.0.1:23333/ws`
- `Assets/StreamingAssets/`
  - `dummy_backend.exe`: 로컬 테스트용 백엔드 실행 파일
  - `server_config.json`: 모델/프롬프트 등 백엔드 설정 파일

## 실행 흐름 요약

1. 유니티 플레이 시작
2. `dummy_backend.exe`가 `--port 23333`으로 자동 실행
3. UI 버튼(또는 호출 함수)로:
   - `/health`, `/configview` 호출
   - `/chat` 호출 (입력 텍스트가 없으면 기본값 사용)
   - `/ws` WebSocket 연결 후 메시지 송수신

## 개발 참고

- 기본 포트는 `23333` 고정입니다.
- WebSocket 연결이 이미 열려 있으면 재연결하지 않고 메시지만 전송합니다.
- 응답/로그는 UI 텍스트와 콘솔 로그로 출력됩니다.
