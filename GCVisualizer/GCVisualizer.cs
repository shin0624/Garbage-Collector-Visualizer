using System;
using System.IO;//C#의 파일 입출력 네임스페이스
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GCVisualizer : EditorWindow
{
    // GC Visualizer : 유니티 내 가비지컬렉터 호출 시점 시각화 기능 구현
    // 기능 1 : 메모리 사용량을 그래프로 표시. gc호출로 예상되는 지점을 빨간색으로 표시함
    // 기능 2 : 현재 메모리 사용량 및 gc발생 시 사용량을 표시함
    // 기능 3 : 메모리에 어떤 오브젝트나 리소스가 할당되는지 추적하여 gc 발생 원인을 분석할 수 있음. 이를 위해 메모리 사용량을 주기적으로 샘플링해서 큰 변화가 있을 때 자동으로 할당을 추적할 수 있는 메모리 변화 감지 기능을 구현
    // 기능 4 : 메모리 히스토리 그래프 저장 및 비교 가능. 특정 시점의 데이터를 csv또는 json으로 변환하여 내보낼 수 있고, 데이터를 불러와서 최적화 전-후 데이터 간 변화를 쉽게 확인 가능

    //---- 기능 1 ----
    private List<float> MemoryUsageData = new List<float>(); //메모리 사용량을 기록할 List
    private float GraphWidth = 500.0f;//그래프 표시 너비
    private float GraphHeight = 200.0f;//그래프 표시 높이
    private float CurrentMemoryUsage = 0f;//현재 메모리 사용량
    private List<float> GcTriggeredMemoryUsage = new List<float>();// 가비지컬렉터 호출 시 메모리 사용량을 기록할 List
    //---- 기능 2 ----
    private Rect rtArea = new Rect(10, 300, 500, 300);//데이터 수치 표시창 렉트에리어 1
    private Rect rtArea2 = new Rect(510, 300, 500, 300);//데이터 수치 표시창 렉트에리어 2
    private Vector2 ScrollPosition;//데이터 수치 표시창 스크롤뷰 1
    private Vector2 ScrollPosition2;//데이터 수치 표시창 스크롤뷰 2

    private float LastSampleTime = 0f;// 정확한 데이터 수집을 위해, 타이머 기반 접근방법을 사용해본다.
    //---- 기능 3-----
    private List<string> AllocationSources = new List<string>();//메모리 할당 소스를 기록하는 리스트
    private List<string> GcTriggeredSources = new List<string>();// gc 트리거 시 어떤 리소스가 관련되는지 기록하는 리스트
    private Dictionary<int, string> ObjectAllocationMap = new Dictionary<int, string>(); //오브젝트 해시값과 소스 매핑
    
    // ---- 기능 4 ----
    private string FilePathCSV = "Asset/MemoryUsageData.csv";//csv파일 저장 경로
    private string FilePathJSON = "Asset/MemoryUsageData.json";//json파일 저장 경로
    private bool DataComparisonEnabled = false;
    private List<float> PreviouseMemoryUsageData = new List<float>();// 이전까지 로드된 데이터를 저장하기 위한 리스트
    private Rect rtArea3 = new Rect(1010, 300, 500, 300);//오브젝트 및 리소스 할당 목록 창 렉트에리어

    [SerializeField]
    private float SampleInterval = 1.0f; //샘플링 간격(초)

    [MenuItem("Tools/GC Visualizer")]
    public static void ShowWindow()
    {
        // GetWindow<GCVisualizer>("Garbage Collector Visualizer");
        GCVisualizer window = (GCVisualizer)GetWindow(typeof(GCVisualizer), false, "Garbage Collector Visualizer");
        window.minSize = new Vector2(800, 400);
        window.maxSize = new Vector2(1700, 800);
    }

    private void OnEnable()
    {
        EditorApplication.update += UpdateMemoryUsage;// 이벤트 핸들러 등록
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateMemoryUsage;//이벤트 핸들러 해제
    }

    private void UpdateMemoryUsage()// 메모리 사용량 업데이트 메서드. 메모리 사용량을 추적하고 실시간으로 데이터를 수집하여 그래프를 업데이트할 것.
    {
        if (Time.realtimeSinceStartup - LastSampleTime >= SampleInterval)// 데이터 샘플링 개선 : 기존에는 realtimeStartUp과 샘플링 인터벌을 나머지 연산한 값과 0.1f를 비교했는데, 타이머 기반 접근법으로 변경함
        {
            LastSampleTime = Time.realtimeSinceStartup;

            CurrentMemoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);// 애플리케이션이 할당한 총 메모리 사용량을  1MB로 나누어 MB단위로 변환. 이때 GetTotalMemory를 false로 설정하여, gc를 강제로 실행하지 않는다.
            MemoryUsageData.Add(CurrentMemoryUsage);//메모리 사용량에 데이터(mb단위) 기록

            if (MemoryUsageData.Count > 1 && MemoryUsageData[MemoryUsageData.Count - 2] - CurrentMemoryUsage > 10.0f)//현재 메모리 사용량과 이전 메모리 사용량의 차가 10mb 이상일 때. 즉 가비지컬렉터 호출 시점으로 간주될 때
            {
                GcTriggeredMemoryUsage.Add(CurrentMemoryUsage);//가비지컬렉터 호출 시 메모리 사용량을 기록
                GcTriggeredSources.Add(AllocationSources.Count > 0 ? AllocationSources[AllocationSources.Count - 1] : "Unknown");// gc호출 전에 할당된 리소스를 확인. 리소스 명이 존재하면 리스트의 인덱스 값을 이용해 string값을 가져올 수 있고, 리스트가 비어있을 경우 기본값인 unknown을 출력.       
            }

            if (MemoryUsageData.Count > GraphWidth) // 메모리 데이터가 너무 많아져서 그래프너비(최대 샘플 수)를 넘어가면, 오래된 데이터는 제거한다.
                MemoryUsageData.RemoveAt(0);

            Repaint();//윈도우 다시그리기
        }
    }

    private void OnGUI()
    {
        GUIStyle MainLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };

        GUILayout.Space(10);
        GUILayout.Label("Garbage Collector Visualizer", MainLabel);

        Rect GraphRect = GUILayoutUtility.GetRect(GraphWidth, GraphHeight); // 그래프를 그릴 직사각형 영역(가로세로) / 시작점 : GraphRect.x / 끝점 : GraphRect.yMax     
        if (MemoryUsageData.Count > 1)
        {
            Handles.BeginGUI();//Handles는 그림을 그리는 함수가 내장된 클래스.
            {
                DrawGraph(GraphRect);
            }
            Handles.EndGUI();//파형 그래프로 시각화.
        }
        GUILayout.Space(10);
        //데이터 저장 및 로드 버튼 추가
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Memory Usage Data Options", MainLabel);
            if (GUILayout.Button("Save Memory Usage Data (CSV)"))
            {
                SaveMemoryUsageDataToCSV();
            }
            if (GUILayout.Button("Save Memory Usage Data (JSON)"))
            {
                SaveMemoryUsageDataToJSON();
            }
            if (GUILayout.Button("Load Previous Memory Usage Data (CSV)"))
            {
                LoadMemoryUsageDataFromCSV();
            }
            if (GUILayout.Button("Load Previous Memory Usage Data (JSON)"))
            {
                LoadMemoryUsageDataFromJSON();
            }
        }
        GUILayout.EndHorizontal();
        DisplayUsageData();
    }

    private void DrawGraph(Rect GraphRect)
    {
        for (int i = 1; i < MemoryUsageData.Count; i++)//기록된 메모리 사용량 데이터만큼 반복
        {
            // 그래프 전체 Width 길이를 저장된 메모리 사용량 데이터 개수로 나눔 -> 각 데이터 포인트 사이의 가로 간격을 구한다. 데이터 포인트가 가로 방향으로 얼마나 떨어져 있는지 나타낼 수 있음(샘플 간 거리 일정)
            float PrevX = GraphRect.x + (i - 1) * (GraphRect.width / MemoryUsageData.Count);//이전 데이터포인트 x좌표
            float CurrX = GraphRect.x + i * (GraphRect.width / MemoryUsageData.Count);//현재 데이터포인트 x좌표

            //메모리 사용량 데이터를 일정 비율로 정규화(100mb를 기준으로). 사용량 계산 후 그래프 Height에 맞추기 위해 비율 적용 / yMax값에서 메모리 사용량에 비례한 높이( (MemoryUsageData[i] / 100f) * GraphRect.height)를 빼면 메모리 사용량이 커질 수록 y좌표가 위로 올라갈 것.
            float PrevY = GraphRect.yMax - (MemoryUsageData[i - 1] / 100.0f) * GraphRect.height;//이전 데이터 포인트y좌표
            float CurrY = GraphRect.yMax - (MemoryUsageData[i] / 100.0f) * GraphRect.height;//현재 데이터 포인트 y좌표

            // 메모리 감소량이 크면 GC 호출로 간주하고 빨간색으로 표시한다.
            Handles.color = MemoryUsageData[i - 1] - MemoryUsageData[i] > 10.0f ? Color.red : Color.green;// 이전 사용량 - 이후 사용량의 차가 10MB 이상이면 GC 호출로 간주함.
            Handles.DrawLine(new Vector3(PrevX, PrevY), new Vector3(CurrX, CurrY));
        }
    }


    private void DisplayUsageData()// 그래프 분석 현황을 표시하는 메서드
    {
        GUIStyle CurrentLabel = new GUIStyle(GUI.skin.label);
        CurrentLabel.fontSize = 12;
        CurrentLabel.fontStyle = FontStyle.Bold;

        GUILayout.BeginHorizontal();
        {
            GUILayout.BeginArea(rtArea, "Usage Data", GUI.skin.window);
            {
                GUILayout.Space(20);
                ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition, GUILayout.Width(rtArea.width - 10), GUILayout.Height(rtArea.height - 30));
                {
                    GUILayout.Label($"Current Memory Usage : " + CurrentMemoryUsage.ToString("F2") + " MB", CurrentLabel);// 현재 메모리 사용량 표시

                    GUILayout.Space(10);

                    EditorGUILayout.LabelField("Memory Usage History : ");
                    for (int i = 0; i < MemoryUsageData.Count; i++)
                    {
                        //메모리 사용 이력 + 어떤 소스가 메모리를 할당했는지 표시. gc가 호출된 시점에 어떤 메모리 할당이 원인이 되었는지 표시. unknown은 엔진이나 플러그인에 의해 자동으로 메모리를 할당하는 경우임. 즉 추적 정보가 없는 메모리 할당을 의미.
                        string source = i < AllocationSources.Count ? AllocationSources[i] : "Unknown";
                        EditorGUILayout.LabelField($" - {MemoryUsageData[i]:F2} MB (Allocated by: {source})", CurrentLabel);
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(rtArea2, "Garbage Collector Triggered Point", GUI.skin.window);
            {
                GUILayout.Space(20);
                ScrollPosition2 = EditorGUILayout.BeginScrollView(ScrollPosition2, GUILayout.Width(rtArea2.width - 10), GUILayout.Height(rtArea2.height - 30));
                {
                    EditorGUILayout.LabelField("GC Triggered Points : ", CurrentLabel);
                    if (GcTriggeredMemoryUsage.Count > 0)
                    {
                        for (int i = 0; i < GcTriggeredMemoryUsage.Count; i++)
                        {//gc가 호출된 시점에 기록된 메모리 사용량과 어떤 리소스나 오브젝트가 메모리를 할당하여 gc가 트리거되었는지 확인
                            string gcSource = i < GcTriggeredSources.Count ? GcTriggeredSources[i] : "Unknown";
                            EditorGUILayout.LabelField($" - {GcTriggeredMemoryUsage[i]:F2} MB (GC triggered by: {gcSource})", CurrentLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No GC Triggered Points Yet.");
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(rtArea3, "Comparison", GUI.skin.window);
            {
                GUILayout.Space(20);
                GUILayout.Label("Comparison of Current and Previous Memory Usage");
                CompareMemoryUsageData();
            }
            GUILayout.EndArea();
        }
        GUILayout.EndHorizontal();
    }

    private void TrackAllocation(string allocationSource)//메모리 할당 시점 추적을 위한 메서드
    {
        //객체 생성 또는 씬 로드 시 등, 메모리 할당이 발생하는 메서드 (Instantiate, Load 등)에 래핑하여 추적 기능 구현 가능
        AllocationSources.Add(allocationSource);
    }

    private void SaveMemoryUsageDataToCSV()//데이터를 csv로 저장하는 메서드
    {
        using (StreamWriter writer = new StreamWriter(FilePathCSV))
        {
            writer.WriteLine("MemoryUsage, AllocationSource");
            for (int i = 0; i < MemoryUsageData.Count; i++)//기록된 메모리 사용량 만큼 반복하며
            {
                string Source = i < AllocationSources.Count ? AllocationSources[i] : "Unknown";
                writer.WriteLine($"{MemoryUsageData[i]:F2}, {Source}");//메모리 사용량과 사용 소스를 기록.
            }
        }
        EditorUtility.DisplayDialog("Save Memory Usage Data", "Save Memory Usage Data To CSV.", "OK");
        return;
    }

    private void SaveMemoryUsageDataToJSON()//데이터를 json으로 저장하는 메서드
    {
        //var MemoryData = new { MemoryUsage = MemoryUsageData, AllocationSources = AllocationSources };
        //string JsonData = JsonConvert.SerializeObject(MemoryData, Formatting.Indented);//메모리 사용량 데이터를 json파일로 컨버트
        //File.WriteAllText(FilePathJSON, JsonData);
        //EditorUtility.DisplayDialog("Save Memory Usage Data", "Save Memory Usage Data To JSON.", "OK");
        //return;
        MemoryData memoryData = new MemoryData { MemoryUsage = MemoryUsageData.ToArray(), AllocationSources = AllocationSources.ToArray() };
        string Json = JsonUtility.ToJson(memoryData, true);
        File.WriteAllText(FilePathJSON, Json);
        EditorUtility.DisplayDialog("Save Memory Usage Data", "Save Memory Usage Data To JSON.", "OK");
        return;

    }

    private void LoadMemoryUsageDataFromCSV()//csv파일을 로드하는 메서드
    {
        if (File.Exists(FilePathCSV))//csv파일경로에 데이터 파일이 존재할 경우
        {
            PreviouseMemoryUsageData.Clear();//이전 메모리사용량 데이터 리스트를 클리어
            using (StreamReader reader = new StreamReader(FilePathCSV))//스트림 리더를 사용하여 파일을 읽는다.
            {
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    string[] line = reader.ReadLine().Split(',');
                    PreviouseMemoryUsageData.Add(float.Parse(line[0]));
                }
            }
            DataComparisonEnabled = true;
            Debug.Log("Previous memory usage data loaded from CSV.");
            EditorUtility.DisplayDialog("Previous Memory Usage Data Loaded", "Previous Memory Usage Data Loaded from CSV.", "OK");
            return;
        }
        else
        {
            Debug.Log("CSV file not found.");
            EditorUtility.DisplayDialog("Previous Memory Usage Data NOT Found", "CSV File Not Found.", "OK");
            return;
        }
    }

    private void LoadMemoryUsageDataFromJSON()//JSON파일을 로드하는 메서드
    {
        if(File.Exists(FilePathJSON))
        {
            PreviouseMemoryUsageData.Clear();
            string json = File.ReadAllText(FilePathJSON);
            MemoryData memoryData = JsonUtility.FromJson<MemoryData>(json);

            PreviouseMemoryUsageData.AddRange(memoryData.MemoryUsage);
            //var MemoryData = JsonConvert.DeserializeObject<dynamic>(JsonData);

            //foreach(var MemoryValue in MemoryData.MemoryUsage)
            //{
            //    PreviouseMemoryUsageData.Add((float)MemoryValue);
            //}

            DataComparisonEnabled = true;
            Debug.Log("Previous memory usage data loaded from JSON.");
            EditorUtility.DisplayDialog("Previous Memory Usage Data Loaded", "Previous Memory Usage Data Loaded from Json.", "OK");
            return;
        }
        else
        {
            Debug.Log("Json file not found.");
            EditorUtility.DisplayDialog("Previous Memory Usage Data NOT Found", "Json File Not Found.", "OK");
            return;
        }
    }

    private void CompareMemoryUsageData()//저장된 데이터와 현재 데이터를 비교하는 창을 출력
    {
        for(int i=0; i<Mathf.Min(MemoryUsageData.Count, PreviouseMemoryUsageData.Count);i++)//이전 데이터와 이후 데이터 수 중 더 적은 쪽 수만큼 반복
        {
            string Comparison = MemoryUsageData[i] > PreviouseMemoryUsageData[i] ? "Increased" : "Decreased";
            GUILayout.Label($"Time {i} : {MemoryUsageData[i]:F2}MB (Previous : {PreviouseMemoryUsageData[i]:F2}MB, {Comparison})");
        }
    }


}
