# 불쑥창닫개

갑자기 뜨는 광고창, 안내창, 팝업창을 사용자가 만든 규칙에 따라 조용히 정리하는 Windows 트레이 유틸리티입니다.

## 주요 기능

- Windows 10/11용 C#/.NET 8 WPF 트레이 앱
- 최근 감지 창 목록에서 바로 규칙 만들기
- 피커 오버레이로 화면의 창을 직접 선택해 규칙 만들기
- 규칙 조건: 창 제목, 앱 프로세스명, 창 클래스, 크기, 위치
- 처리 방식: `작게 내리기`, `숨기기`, `닫기`, `기록만`
- 규칙 활성/비활성 토글
- 규칙/처리 기록 검색
- 정리 강도: `구경만`, `조심`, `적당`, `적극`
- 처리 완료 토스트와 되돌리기
- Windows 시작 시 자동 실행 옵션

## 실행

배포 폴더가 있는 경우 아래 파일을 실행합니다.

```powershell
.\artifacts\publish\win-x64\불쑥창닫개.exe
```

처음 실행하면 메인 창과 트레이 아이콘이 표시됩니다. 창을 닫아도 앱은 트레이에 남아 계속 감시합니다. 트레이 아이콘은 좌클릭과 우클릭 모두 앱 전용 팝오버를 열며, Windows 표준 우클릭 메뉴와 더블클릭 액션은 따로 제공하지 않습니다. 완전히 종료하려면 트레이 팝오버에서 `종료`를 선택합니다.

## 단축키

- `Ctrl+Alt+X`: 현재 커서 아래 창 캡처 후 규칙 만들기
- `Ctrl+Alt+P`: 잠시 멈춤 토글

## 설정과 데이터

설정, 규칙, 처리 기록은 사용자 프로필 아래에 저장됩니다.

```text
%AppData%\WindowFilterTray
```

자동 시작은 사용자 권한 레지스트리만 사용합니다.

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

## 안전 정책

- 중요한 시스템 창은 항상 제외합니다.
- 기본 안전 키워드가 포함된 창은 처리하지 않습니다: `업데이트`, `보안`, `로그인`, `인증`, `결제`, `암호`, `Windows`, `Microsoft`
- 일반 사용자 권한으로 실행합니다.
- DLL 인젝션, 글로벌 마우스 훅, 네트워크/방화벽/hosts 변경은 사용하지 않습니다.
- `닫기`는 되돌릴 수 없으므로 처음에는 `작게 내리기` 또는 `숨기기`를 권장합니다.

## 알려진 제약

- 관리자 권한으로 실행된 다른 앱의 창은 일반 사용자 권한에서 조작이 제한될 수 있습니다.
- 앱 내부 WebView/브라우저 콘텐츠 요소 단위 차단은 지원하지 않습니다.
- 네트워크 수준 광고 차단은 지원하지 않습니다.
- MSI는 아직 코드 서명되지 않았으므로 Windows SmartScreen 또는 게시자 경고가 표시될 수 있습니다. 차단 화면이 뜨면 파일 출처를 확인한 뒤 `추가 정보` → `실행`으로 진행해야 합니다.
- 코드 서명 인증서 적용과 GitHub Release 자동화는 아직 제공하지 않습니다.

## 문의

- 제작자: 임선규
- 이메일: `seonkyu@gmail.com`
- Instagram DM: [@seon_7yu](https://www.instagram.com/seon_7yu/)

## 개발

이 저장소는 .NET SDK가 설치된 Windows 환경에서 빌드합니다.

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

로컬 번들 SDK를 사용하는 경우:

```powershell
.\.dotnet\dotnet.exe restore
.\.dotnet\dotnet.exe build -c Release
.\.dotnet\dotnet.exe run
```

## 배포 빌드

`win-x64` self-contained single-file 배포 폴더를 만들려면:

```powershell
.\scripts\Publish.ps1
```

산출물은 아래 경로에 생성됩니다.

```text
artifacts\publish\win-x64
```

휴대용 zip 패키지는 아래 명령으로 만듭니다.

```powershell
.\scripts\Package-Zip.ps1
```

zip 파일은 `artifacts\release\WindowFilterTray-win-x64-v1.0.1.zip` 형식으로 생성됩니다. 스크립트는 저장소의 `.dotnet\dotnet.exe`가 있으면 우선 사용하고, 없으면 시스템 `dotnet`을 사용합니다. WPF 네이티브 DLL은 실행 파일 옆에 함께 배치됩니다.

MSI 패키지는 아래 명령으로 만듭니다.

```powershell
.\scripts\Package-Msi.ps1
```

MSI 빌드는 `WixToolset.Sdk/7.0.0`과 WiX 7 EULA 수락(`AcceptEula=wix7`)을 사용합니다. 설치 UI는 기본 설치 위치 선택과 라이선스 안내 화면을 포함합니다. MSI는 아직 코드 서명되지 않았으므로 SmartScreen 또는 게시자 경고가 표시될 수 있습니다. portable zip과 MSI는 같은 `%AppData%\WindowFilterTray` 데이터를 공유하므로 둘 중 하나의 배포 방식만 사용하는 것을 권장합니다.

## 디자인 자료

`docs\design` 폴더는 Claude 목업과 디자인 참고 산출물입니다. `artifacts` 배포 패키지에는 포함하지 않는 개발 참고 자료입니다.
