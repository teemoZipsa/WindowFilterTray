# Window Filter Tray

사용자가 직접 지정한 Windows 창을 감지하고 처리하는 트레이 기반 자동화 유틸리티입니다.

## Features

- Windows 10/11용 C#/.NET 10 WPF 트레이 앱
- H.NotifyIcon 기반 시스템 트레이 메뉴
- `Ctrl+Alt+X` 현재 커서 아래 창 캡처
- `Ctrl+Alt+P` 전체 차단 ON/OFF 토글
- 최근 30분간 감지된 최상위 창 목록
- 피커 모드로 창 클릭 후 규칙 생성
- 전역 `필터링 모드` 슬라이더
  - 꺼짐: 차단하지 않고 감지만 합니다
  - 약함: 확실한 경우에만 차단합니다
  - 최적: 권장. 균형 잡힌 차단
  - 강함: 의심되는 창을 적극적으로 차단합니다
- 사용자 규칙 기반 `CloseWindow`, `HideWindow`, `Minimize`, `Ignore`
- 일반 사용자 권한 실행
- 설정/규칙/통계 저장 위치: `%AppData%/WindowFilterTray`

## Safety Model

- 시스템 및 보안 관련 창은 모든 단계에서 자동으로 제외됩니다.
- 강제 예외 프로세스와 셸 창 클래스는 사용자가 만든 규칙으로도 처리하지 않습니다.
- 기본 안전 키워드: `업데이트`, `보안`, `로그인`, `인증`, `결제`, `암호`, `Windows`, `Microsoft`
- DLL 인젝션, 글로벌 마우스 훅, 네트워크/방화벽/hosts 변경은 사용하지 않습니다.

## Build

이 저장소는 .NET SDK가 설치된 Windows 환경에서 빌드합니다.

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

현재 구현은 `net10.0-windows`, WPF, H.NotifyIcon.Wpf를 대상으로 합니다.

## Notes

- 모든 규칙은 사용자가 캡처하고 확인한 창에서 직접 생성됩니다.
- 본창 내부에 렌더링되는 콘텐츠, WebView2/CEF 내부 요소, 네트워크 수준 필터링은 v1 범위가 아닙니다.
- 자동 시작은 사용자 옵션이며 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`만 사용합니다.
