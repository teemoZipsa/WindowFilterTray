# 불쑥창닫개

갑자기 뜨는 창을 사용자가 정한 방식대로 조용히 정리하는 Windows 유틸리티입니다.

## Features

- Windows 10/11용 C#/.NET 8 WPF 트레이 앱
- Windows Forms NotifyIcon 기반 시스템 트레이 메뉴
- `Ctrl+Alt+X` 현재 커서 아래 창 캡처
- `Ctrl+Alt+P` 잠시 멈춤 토글
- 최근 30분간 열린 최상위 창 목록
- 피커 모드로 창 클릭 후 정리 항목 생성
- 전역 `정리 강도` 슬라이더
  - 구경만: 아무 창도 닫지 않고 기록만 남깁니다
  - 조심: 확실히 같은 창일 때만 정리합니다
  - 적당: 권장. 대부분의 경우에 알맞게 정리합니다
  - 적극: 비슷한 창도 더 빠르게 정리합니다
- 사용자 설정 기반 `작게 내리기`, `숨기기`, `닫기`, `기록만`
- `작게 내리기`와 `숨기기` 후 되돌리기 알림
- 일반 사용자 권한 실행
- 설정과 기록 저장 위치: `%AppData%/WindowFilterTray`

## Safety Model

- 중요한 시스템 창은 항상 그대로 둡니다.
- 중요한 Windows 구성 요소는 사용자가 만든 설정으로도 처리하지 않습니다.
- 기본 안전 키워드: `업데이트`, `보안`, `로그인`, `인증`, `결제`, `암호`, `Windows`, `Microsoft`
- DLL 인젝션, 글로벌 마우스 훅, 네트워크/방화벽/hosts 변경은 사용하지 않습니다.
- 관리자 권한이 필요 없습니다.

## Build

이 저장소는 .NET SDK가 설치된 Windows 환경에서 빌드합니다.

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

현재 구현은 `net8.0-windows`, WPF, Windows Forms NotifyIcon을 대상으로 합니다.

## Notes

- 모든 정리 항목은 사용자가 캡처하고 확인한 창에서 직접 생성됩니다.
- 본창 내부에 렌더링되는 콘텐츠, WebView2/CEF 내부 요소, 네트워크 수준 필터링은 v1 범위가 아닙니다.
- 자동 시작은 사용자 옵션이며 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`만 사용합니다.
