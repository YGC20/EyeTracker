# LookAtCat

웹캠으로 얼굴 위치를 추적해서, 캐릭터의 눈이 그 방향을 따라 움직이는 WPF 인터랙션 프로젝트입니다.
컴퓨터 비전 파트는 검증된 라이브러리(OpenCV, dlib)를 활용하고, 스레드 아키텍처·상태 관리·캐릭터 반응 로직 구현에 집중했습니다.

## 진행 배경

노트북 웹캠만으로 사람의 얼굴을 실시간 검출하고, 그 위치에 반응해 캐릭터가 시선을 옮기는 인터랙션을 목표로 시작한 프로젝트입니다. 웹캠 캡처 → 얼굴 검출 → 눈 랜드마크 → 동공 검출까지 비전 파이프라인을 단계적으로 구현한 뒤, 실제 실행 검증을 거치며 "동공 위치 기반 시선 추적"보다 "얼굴 위치 기반 추적"이 체감상 더 안정적이고 자연스럽다고 판단해 최종적으로 얼굴 위치 기반으로 캐릭터를 움직이는 구조로 정리했습니다. 그 위에 얼굴을 놓쳤을 때의 자연스러운 대기 동작(마지막 위치 유지 → 랜덤워크)까지 상태머신으로 구현했습니다.

## 주요 기능

- **웹캠 실시간 캡처 + 저조도 감마 보정** — 백그라운드 스레드에서 프레임을 지속적으로 읽어오며, LUT(룩업테이블) 기반 감마 보정으로 어두운 환경에서도 화면과 검출 입력이 함께 밝아지도록 처리합니다.
- **다중 얼굴 중 추적 대상 선택** — 여러 명이 잡혀도 처음에는 가장 큰(가까운) 얼굴을 추적 대상으로 잠그고, 이후에는 이전 위치와 가장 가까운 얼굴을 계속 같은 사람으로 간주해 타겟이 바뀌지 않게 합니다.
- **68점 랜드마크 기반 눈 영역 추출 + 동공 검출** — dlib 랜드마크로 눈 영역을 특정하고, Otsu 이진화 + 컨투어 무게중심 방식으로 동공 위치를 계산합니다 (현재 캐릭터 시선 방향 계산에는 사용하지 않지만, 검출 결과는 디버그 오버레이로 확인 가능합니다).
- **좌표 스무딩 (EMA)** — 프레임 간 검출 좌표의 미세한 떨림을 지수이동평균으로 완화합니다.
- **상태 기반(FSM) 캐릭터 동작** — 얼굴을 추적하는 동안은 그 방향을, 잠깐 놓치면 마지막 방향을 유지하다가, 일정 시간 넘게 안 잡히면 랜덤워크(목표 지점을 주기적으로 바꿔가며 서서히 이동)로 전환합니다. 재감지 시에도 순간 이동이 아니라 부드럽게 이어집니다.

### 상태 전이

| 상태 | 조건 | 동작 |
|---|---|---|
| Tracking | 얼굴이 검출됨 | 캐릭터 눈이 얼굴 위치 방향으로 이동 |
| HoldLast | 얼굴을 놓친 지 유예시간(기본 1.5초) 이내 | 마지막 방향 유지 |
| RandomWalk | 유예시간 초과 | 일정 간격(기본 1.5초)마다 새 목표 지점을 향해 서서히 이동 |

## 빌드 & 실행

- **요구 사항**: Windows, Visual Studio(.NET 10 SDK 워크로드 포함), 웹캠
- **랜드마크 모델 파일 별도 다운로드 필요** (용량 문제로 저장소에는 미포함, `.gitignore` 처리됨):
  1. [shape_predictor_68_face_landmarks.dat.bz2](http://dlib.net/files/shape_predictor_68_face_landmarks.dat.bz2) 다운로드 후 압축 해제
  2. `LookAtCat/Models/shape_predictor_68_face_landmarks.dat` 경로에 저장
- `LookAtCat.slnx` 솔루션을 Visual Studio로 열고 `LookAtCat`을 실행합니다 (NuGet 패키지는 자동 복원됩니다).

## 프로젝트 구조

```
LookAtCat/                              # 저장소 루트
├─ LookAtCat/                           # WPF 프로젝트
│  ├─ Vision/
│  │  ├─ FrameCaptureService.cs      # 백그라운드 스레드에서 웹캠 프레임 캡처 + 감마 보정
│  │  ├─ FaceDetectService.cs        # 별도 스레드: 얼굴 검출 → 랜드마크 → 동공 검출 파이프라인
│  │  ├─ FaceTracker.cs              # 다중 얼굴 중 추적 대상 선택 (최초: 최대 면적, 이후: 최근접)
│  │  ├─ FaceTrackingResult.cs       # 한 프레임의 검출 결과(얼굴/눈 랜드마크/동공) 묶음
│  │  ├─ PupilDetector.cs            # 눈 영역 크롭 → Otsu 이진화 → 컨투어 → 동공 중심 계산
│  │  ├─ PositionSmoother.cs         # 좌표 EMA 스무딩
│  │  ├─ GazeDirection.cs            # 얼굴 위치 기반 시선 방향(-1~1 정규화) 계산
│  │  └─ CharacterStateMachine.cs    # Tracking/HoldLast/RandomWalk 상태머신 + 방향 스무딩
│  ├─ Models/
│  │  └─ shape_predictor_68_face_landmarks.dat  # dlib 68점 랜드마크 모델 (별도 다운로드)
│  ├─ MainWindow.xaml(.cs)           # 웹캠 디버그 오버레이 + 캐릭터 눈 렌더링
│  └─ LookAtCat.csproj
└─ LookAtCat.slnx
```

## 아키텍처

캡처, 검출, 렌더링을 서로 다른 스레드로 분리하고, 스레드 간 데이터는 큐가 아니라 **"최신 값 하나만 lock으로 보호해 덮어쓰는"** 방식으로 전달합니다 (처리 지연이 누적되는 것을 방지).

```
[캡처 스레드] FrameCaptureService
        │ 최신 프레임(Mat/BitmapSource) 공유 변수로 전달
        ▼
[검출 스레드] FaceDetectService
        │  dlib 얼굴 검출 → FaceTracker(대상 선택) → ShapePredictor(랜드마크)
        │  → PupilDetector(동공) → PositionSmoother(스무딩)
        │  결과를 FaceTrackingResult 하나로 묶어 공유 변수로 전달
        ▼
[UI 스레드] MainWindow (CompositionTarget.Rendering)
        │  최신 결과를 매 틱마다 폴링
        │  → GazeDirection(방향 계산) → CharacterStateMachine(상태 전이 + 스무딩)
        ▼
     캐릭터 눈 렌더링 (+ 웹캠 디버그 오버레이)
```

| 구성 | 기술 | 역할 |
|---|---|---|
| 캡처/영상 처리 | OpenCvSharp4 | 웹캠 캡처, 감마 보정, 동공 검출(임계값·컨투어) |
| 얼굴/랜드마크 검출 | DlibDotNet | HOG 기반 얼굴 검출, 68점 랜드마크 |
| UI | WPF (.NET 10) | 캐릭터 렌더링, 디버그 오버레이 |

### Vision 파이프라인 클래스 설계

검출 스레드는 비전 처리(누가 어디 있는지 계산)만 담당하고, UI 스레드는 그 결과를 받아 상태 판단과 렌더링만 담당하도록 책임을 분리했습니다.

```
FaceDetectService ──uses──▶ FrameCaptureService (최신 프레임 획득)
FaceDetectService ──uses──▶ FaceTracker            (얼굴 선택, 상태 보유)
FaceDetectService ──uses──▶ PupilDetector           (동공 검출, 순수 정적 계산)
FaceDetectService ──uses──▶ PositionSmoother × 2    (좌/우 눈 각각 소유, 상태 보유)
FaceDetectService ──produces──▶ FaceTrackingResult  (얼굴+눈 랜드마크+동공 스냅샷)

MainWindow ──uses──▶ GazeDirection        (방향 계산, 순수 정적 계산)
MainWindow ──uses──▶ CharacterStateMachine (상태 전이 + 스무딩, 상태 보유)
```

- **`FaceTracker`/`PositionSmoother`/`CharacterStateMachine`**처럼 프레임 간 기억이 필요한 클래스는 상태를 인스턴스 필드로 보유합니다.
- **`PupilDetector`/`GazeDirection`**처럼 이번 프레임 입력만으로 계산이 끝나는 클래스는 상태 없이 정적 메서드로 구성했습니다.
