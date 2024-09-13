# Garbage-Collector-Visualizer
Unity Engine에서 가비지 컬렉터 호출 지점을 그래프로 보여주고, 메모리 히스토리를 파일로 I/O하여 비교할 수 있는 커스텀 에디터 윈도우

# 패키지 다운로드 링크
  https://drive.google.com/drive/folders/1zsrdGLh9p7opG4H9u1sYycETL3ILjBT_?usp=sharing

# 개발 환경
- Unity Engine 2023.2.16f1
- Visual Studio Community 2022
- C#

# 기술 스택
- Unity Engine의 커스텀 에디터 기능
- EditorWindow
- EditorGUILayer / GUILayer
- System.IO
- C#의 메모리 프로파일링

# 개요
유니티는 가비지 컬렉터(GC) 호출 시점을 추적할 수 없으나, 메모리 사용량 변화를 추적하여 GC호출을 유추할 수 있음. (GC 호출 후 메모리 사용량이 급격히 떨어짐)

# 구현
- 특정 시간 간격으로 메모리 사용량을 수집하고, 메모리 사용량이 10MB 이상 떨어질 때 GC가 호출되었다고 가정하여 이를 그래프로 시각화
- 메모리 사용량 추적 : System.GC.GetTotalMemory를 사용하여 현재 메모리 사용량을 주기적으로 측정
- GC 호출 추정 : 이전 메모리 사용량과 현재 메모리 사용량을 비교하여, 10MB이상 변화 감지 시 GC호출로 가정
- 그래프 시각화 : 메모리 사용량 변화를 시간 축에 따라 그래프로 표현하고, GC호출로 판단되는 지점을 빨간색으로 표시

# 기능
1. 현재 할당된 메모리 및 사용량을 그래프와 수치로 표시
2. 메모리에 어떤 오브젝트나 리소스가 할당되는지 추적하여 GC 발생 원인 분석 가능. 이를 위해 메모리 사용량을 주기적으로 샘플링해서 큰 변화가 있을 때 자동으로 할당을 추적.
3. 메모리 히스토리 그래프 저장 및 비교 가능. 특정 시점의 데이터를 JSON, CSV로 변환하여 내보낼 수 있고, 데이터를 불러와서 최적화 전-후 데이터 간 변화를 쉽게 확인 가능

# 사용 예시
  ## Tool -> GC Visualizer 클릭
  ![툴바](https://github.com/user-attachments/assets/dfa8db6b-63da-4218-8478-8e0844d26f20)
  ## GC Visualzer 메인 화면
  ![VER1메인화면](https://github.com/user-attachments/assets/d320cec8-d2ef-47a2-afc6-d46a2635f7a2)
  
  ## 메모리 사용량 그래프
  ![메모리사용량그래프](https://github.com/user-attachments/assets/0f353cc3-917c-471c-94ba-0560fd9bb89f)
  - System.GC.GetTotalMemory를 사용하여 애플리케이션이 할당한 총 메모리 사용량을  1MB로 나누어 MB단위로 변환. 이때 GetTotalMemory를 false로 설정하여, gc를 강제로 실행하지 않는다.
  - 현재 메모리 사용량과 이전 메모리 사용량의 차가 10mb 이상일 때. 즉 가비지컬렉터 호출 시점으로 간주될 때 메모리 사용량을 기록.
  - 메모리 데이터가 너무 많아져서 그래프너비(최대 샘플 수)를 넘어가면, 오래된 데이터는 제거한다.

  ## 유니티 내 GC 호출 시 그래프 변화
  ![VER1 GC호출구간](https://github.com/user-attachments/assets/0883b61b-36b2-493a-a4bc-4b115b13192c)
  
  ## Usage Data
  - 현재 메모리 사용량을 소수점 둘째 자리까지 표시.

  ## Memory Usage History
  - 메모리 사용 이력 + 어떤 소스가 메모리를 할당했는지 표시.
  - GC가 호출된 시점에 어떤 메모리 할당이 원인이 되었는지 표시.
  - unknown은 엔진이나 플러그인에 의해 자동으로 메모리를 할당하는 경우임. 즉 추적 정보가 없는 메모리 할당을 의미.

  ## Garbage Collector Triggered Point
  - GC가 호출된 시점에 기록된 메모리 사용량과 어떤 리소스나 오브젝트가 메모리를 할당하여 GC가 트리거되었는지 확인

  ## Comparison
  - 저장된 데이터와 현재 데이터를 비교하는 창을 출력.
  - JSON, CSV로 내보내고 불러온 이전-이후 시간대 별 메모리 사용량을 비교할 수 있음.
  1. csv, json 중 원하는 포맷을 선택한 후 SAVE![버튼](https://github.com/user-attachments/assets/cb20e7f2-9502-4b3f-84f3-5b250872c50d)![SAVECOLPETE](https://github.com/user-attachments/assets/e8f1a3c5-b834-406b-90ec-934a97432cd4)
  2. Asset 폴더 내 DataFiles폴더에 저장됨![SAVEDIRECTORY](https://github.com/user-attachments/assets/11eed4b0-8f47-412e-8bf5-8c2c08493b57)
  3. CSV파일 저장 시![CSVFILE](https://github.com/user-attachments/assets/fb3ffca0-b499-416d-990e-61b90cf736b0)
  4. JSON파일 저장 시![JSONFILE](https://github.com/user-attachments/assets/964cfa89-8d49-4f0c-bc26-c8004ba39705)
  5. 포맷에 맞는 Load를 클릭하면 Comparison 윈도우에 현재 메모리에 할당된 사용량, 이전 메모리 사용량과 증가/감소 여부가 출력됨![CSV파일 로드 후 비교](https://github.com/user-attachments/assets/499a5c7b-f0ea-4ad0-a6c5-881902ebf0fb)

  ## private void TrackAllocation(string allocationSource)
  ![TrackAllocation](https://github.com/user-attachments/assets/be64aac4-5b27-44ab-8f8e-17584b8414af)
  - 메모리 할당 시점 추적을 위한 메서드.
  - 메모리 할당 소스를 기록하는 리스트인 AllocationSources에 GC를 트리거시킨 소스의 이름을 삽입
  - 객체 생성 또는 씬 로드 시 등, 메모리 할당이 발생하는 메서드 (Instantiate, Load 등)에 래핑하여 추적 기능 구현 가능

  ## Unity의 Profiler와의 차별점
  - 가비지 컬렉션에 초점을 맞춘 직관적인 시각화
  - 자동화된 분석 및 최적화 제안 : 데이터 분석을 자동화하여 메모리 최적화 팁이나 메모리 사용 패턴의 문제점을 바로 피드백 가능.
  - GC 발생원인 추적 : 어떤 스크립트나 오브젝트가 메모리 할당을 유발했는지를 기록하여 원인을 명확히 추적하는 기능
  - 메모리 히스토리 관리 및 비교 기능 : 이전 최적화 시점과 이후 성능차이를 쉽게 파악
  - 커스텀 리포트 기능 : 메모리 사용과 gc 호출 데이터를 csv, json으로 내보내고, 분석 리포트를 자동으로 생성하는 기능. 프로젝트 최적화 과정의 문서화에 도움

  ## 현재 오류(해결)
  ![현재오류](https://github.com/user-attachments/assets/ce91515b-ba5f-41d9-a29a-dc0bf83eb665)
  - UI 문제 : 파일 로드/세이브 메서드에서 오류가 발생하는 듯 하여 try-catch로 예외처리를 추가하고 다이얼로그디스플레이 출력을 후순위로 분할
  ![파일경로수정2](https://github.com/user-attachments/assets/602e9aba-339f-484c-a023-24371f5294a7)
  
  - 파일 I/O 불가 문제 : 파일경로를 Application.dataPath를 사용하여 상대경로로 지정
  ![파일경로수정1](https://github.com/user-attachments/assets/08219c18-1724-4024-b865-0d644601bd37)

  ## 패키지 구성
  ![패키지목록](https://github.com/user-attachments/assets/058438a7-e828-4e35-bc77-f3a1b90da7b9)
  - MemoryData : json파일 세이브, 로드를 위해 생성. 기존에는 Newtonsoft 네임스페이스에서 dynamic을 사용한 메서드를 썼는데, c# 버전에 따라 사용이 불가할 수 있기에 jsonutility로 대체
  - GCVisualizer : 에디터 및 메인 소스

  ## 버전 목록
  - 2024.09.12 Ver.1 : 오류로 인해 미배포
  - 2024.09.13 Ver.2 : https://drive.google.com/drive/folders/1zsrdGLh9p7opG4H9u1sYycETL3ILjBT_?usp=sharing

  ## 업데이트 노트
  - Ver.1 : 그래프 시각화, 메모리 및 GC트리거 내역 수치화 / UI 오류 및 파일 LOAD/SAVE 불가 오류
  - Ver.2 : SAVE, LOAD 수행 시 EditorApplication.delayCall을 사용하여 DispalyDialog를 후순위로 호출하여 UI 오류 해결,
            파일 경로를 Application.dataPath를 사용하여 상대경로로 지정하고 디렉터리 널체크 추가
