# Unity 씬 구성 가이드 — 5단계 처리 시설 줌인/줌아웃

> Unity 6 (6000.0.62f1) / URP 기준

---

## 1. 완성 후 Hierarchy 구조

```
SampleScene
├── SensorDataReceiver          ← 빈 오브젝트 (싱글톤)
├── Main Camera                 ← CameraController 스크립트 연결
├── Directional Light
├── Plane                       ← 바닥 (기존 유지)
│
├── Stage_1_PrimarySettling     ← StageView (index=0)
│   ├── Tank                    ←  외곽 Cube (불투명)
│   └── Water                  ←  내부 Cube (반투명, TankController 대상)
├── Stage_2_Aeration            ← StageView (index=1)
│   ├── Tank
│   └── Water
├── Stage_3_SecondarySettling   ← StageView (index=2)
│   ├── Tank
│   └── Water
├── Stage_4_Nitrification       ← StageView (index=3)
│   ├── Tank
│   └── Water
├── Stage_5_Disinfection        ← StageView (index=4)
│   ├── Tank
│   └── Water
│
├── Pipes                       ← 시설 간 연결 파이프 (Cylinder)
│   ├── Pipe_1_to_2
│   ├── Pipe_2_to_3
│   ├── Pipe_3_to_4
│   └── Pipe_4_to_5
│
└── UI (Canvas, Screen Space - Overlay)
    ├── SidebarPanel            ← SidebarUI 스크립트 연결
    │   ├── StageButton_0       ← Button
    │   │   ├── StatusDot       ←   Image (원형, 상태 색상)
    │   │   └── Label           ←   TextMeshProUGUI "1차 침전"
    │   ├── StageButton_1
    │   ├── StageButton_2
    │   ├── StageButton_3
    │   └── StageButton_4
    └── DetailPanel             ← DetailUI 스크립트 연결 (초기: 비활성)
        ├── BackButton          ← Button "← 전체보기"
        ├── StageName           ← TextMeshProUGUI
        ├── HRTText             ← TextMeshProUGUI
        ├── StatusBadge         ← Image (배경색으로 상태 표시)
        ├── StatusText          ← TextMeshProUGUI
        ├── InfluentSection
        │   ├── Label           ← "유입수"
        │   ├── InBOD           ← TextMeshProUGUI
        │   ├── InTSS           ← TextMeshProUGUI
        │   ├── InCOD           ← TextMeshProUGUI
        │   └── InNH3           ← TextMeshProUGUI
        ├── EffluentSection
        │   ├── Label           ← "유출수"
        │   ├── OutBOD          ← TextMeshProUGUI
        │   ├── OutTSS          ← TextMeshProUGUI
        │   ├── OutCOD          ← TextMeshProUGUI
        │   └── OutNH3          ← TextMeshProUGUI
        └── RemovalSection
            ├── RemovalBOD      ← TextMeshProUGUI (제거율 %)
            └── RemovalTSS      ← TextMeshProUGUI
```

---

## 2. 탱크 5개 배치 (3D 씬)

5개 탱크를 일렬 또는 지그재그로 배치합니다.

### 추천 위치 (일렬 배치)

| 이름 | Position |
|------|----------|
| Stage_1 | (-8, 0, 0) |
| Stage_2 | (-4, 0, 0) |
| Stage_3 | ( 0, 0, 0) |
| Stage_4 | ( 4, 0, 0) |
| Stage_5 | ( 8, 0, 0) |

### 각 탱크 오브젝트 만드는 방법

1. Hierarchy → **Create Empty** → 이름: `Stage_1_PrimarySettling`
2. 위 오브젝트 안에 **3D Object → Cube** → 이름: `Tank`
   - Scale: (2, 2.5, 2)
   - Material: 불투명 (기본 URP/Lit)
3. 같은 위치에 **3D Object → Cube** → 이름: `Water`
   - Scale: (1.8, 1.0, 1.8) — Tank 안에 딱 맞게
   - Position: (0, -0.5, 0) — 탱크 하단에서 시작
   - Material: **반투명 URP/Lit** (Rendering Mode: Transparent, Alpha ~0.8)
   - 색상: 파란색 (0.1, 0.72, 0.95)
4. `Stage_1_PrimarySettling`에 **StageView** 스크립트 추가

---

## 3. 스크립트 연결 방법

### 3-1. SensorDataReceiver (싱글톤)

1. Hierarchy → Create Empty → 이름: `SensorDataReceiver`
2. Add Component → `SensorDataReceiver`
3. **이것이 반드시 먼저 씬에 있어야 합니다** (다른 스크립트가 Instance에 의존)

---

### 3-2. StageView (탱크 5개 각각)

각 `Stage_N_...` 오브젝트 선택 후:

| Inspector 항목 | 설정값 |
|---------------|--------|
| Stage Index | 0 / 1 / 2 / 3 / 4 |
| Stage Name Ko | "1차 침전" / "폭기조" / "2차 침전" / "질산화" / "소독" |
| Water Transform | 자식 `Water` 오브젝트 드래그 |
| Water Renderer | 자식 `Water` 오브젝트 드래그 |

---

### 3-3. CameraController (Main Camera)

Main Camera 선택 후:

| Inspector 항목 | 설정값 |
|---------------|--------|
| Overview Position | (0, 15, -22) |
| Overview Look Target | (0, 0, 0) |
| Zoom Offset | (0, 6, -9) |
| Transition Duration | 0.9 |

---

### 3-4. SidebarUI (Canvas > SidebarPanel)

SidebarPanel 오브젝트 선택 후 Add Component → `SidebarUI`:

| Inspector 항목 | 설정값 |
|---------------|--------|
| Stage Views [0~4] | 각 Stage_N 오브젝트 드래그 |
| Stage Buttons [0~4] | 각 StageButton_N 드래그 |
| Status Dots [0~4] | 각 StageButton 안의 StatusDot Image 드래그 |
| Stage Labels [0~4] | 각 StageButton 안의 Label TMP 드래그 |
| Sidebar Root | SidebarPanel 오브젝트 자기 자신 |

---

### 3-5. DetailUI (Canvas > DetailPanel)

DetailPanel 오브젝트 선택 후 Add Component → `DetailUI`:

| Inspector 항목 | 연결 대상 |
|---------------|----------|
| Detail Panel | DetailPanel 오브젝트 자기 자신 |
| Back Button | BackButton |
| Stage Name Text | StageName TMP |
| Hrt Text | HRTText TMP |
| Status Text | StatusText TMP |
| Status Badge | StatusBadge Image |
| In BOD Text | InBOD TMP |
| In TSS Text | InTSS TMP |
| In COD Text | InCOD TMP |
| In NH3 Text | InNH3 TMP |
| Out BOD Text | OutBOD TMP |
| Out TSS Text | OutTSS TMP |
| Out COD Text | OutCOD TMP |
| Out NH3 Text | OutNH3 TMP |
| Removal BOD Text | RemovalBOD TMP |
| Removal TSS Text | RemovalTSS TMP |

> ⚠️ DetailPanel은 Inspector에서 **비활성(SetActive false)**로 시작해야 합니다.

---

## 4. Canvas UI 레이아웃 권장 크기

```
Canvas (1920×1080 기준)

SidebarPanel
  Anchor: Left
  Width: 200px
  Height: 400px
  Position: (-760, 0)

  각 StageButton: Width 190, Height 68, 간격 10px
    StatusDot: 16×16px, 왼쪽 끝
    Label: TextMeshPro, 폰트 14, 볼드

DetailPanel
  Anchor: Right Center
  Width: 320px
  Height: 480px
  Position: (680, 0)
  Background: 반투명 검정 (0, 0, 0, 0.85)

  BackButton: 상단, Width 120, Height 40
  StageName: 폰트 22, 볼드
  HRTText: 폰트 13, 회색
  StatusBadge: 수평 바 (전체폭, 높이 32)
  StatusText: 폰트 14, 가운데 정렬

  유입수/유출수 각 섹션:
    라벨: 폰트 11, 흐린 색
    값: 폰트 13
```

---

## 5. 파이프 연결 오브젝트

탱크 사이를 Cylinder로 연결:

1. Create Empty → `Pipes`
2. 안에 **3D Object → Cylinder** (Pipe_1_to_2 등)
   - 두 탱크 중간 위치에 배치
   - Scale: (0.15, 탱크간 거리/2, 0.15)
   - Rotation: Z축 90도 (가로 파이프)

---

## 6. WebGL 빌드 설정

```
File → Build Settings
  Platform: WebGL

Player Settings:
  Resolution: 900 × 600 (WebGL)
  Run In Background: ✓ 체크

Output 경로:
  ..\frontend\public\unity\AquaView3D\
  (예: C:\Users\<이름>\aquaview\frontend\public\unity\AquaView3D)
```

---

## 7. 작업 우선순위

```
✅ 필수 (기본 동작)
  1. SensorDataReceiver 빈 오브젝트 추가
  2. 탱크 5개 배치 + StageView 연결
  3. Main Camera에 CameraController 연결
  4. Canvas에 SidebarPanel + 5개 버튼 배치 → SidebarUI 연결
  5. Canvas에 DetailPanel 배치 → DetailUI 연결
  6. WebGL 빌드

⭐ 권장 (완성도)
  7. 파이프 Cylinder 연결
  8. 각 탱크 위에 공정명 3D Text (TextMeshPro 3D) 추가
  9. 탱크 테두리 경고 상태 시 발광 (Emission) 효과

💡 선택 (어필 요소)
  10. PumpAnimator를 Stage_2 폭기조에 추가 (임펠러 회전)
  11. WaterFlowParticle을 파이프에 추가
```
