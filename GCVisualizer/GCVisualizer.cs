using System;
using System.IO;//C#�� ���� ����� ���ӽ����̽�
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GCVisualizer : EditorWindow
{
    // GC Visualizer : ����Ƽ �� �������÷��� ȣ�� ���� �ð�ȭ ��� ����
    // ��� 1 : �޸� ��뷮�� �׷����� ǥ��. gcȣ��� ����Ǵ� ������ ���������� ǥ����
    // ��� 2 : ���� �޸� ��뷮 �� gc�߻� �� ��뷮�� ǥ����
    // ��� 3 : �޸𸮿� � ������Ʈ�� ���ҽ��� �Ҵ�Ǵ��� �����Ͽ� gc �߻� ������ �м��� �� ����. �̸� ���� �޸� ��뷮�� �ֱ������� ���ø��ؼ� ū ��ȭ�� ���� �� �ڵ����� �Ҵ��� ������ �� �ִ� �޸� ��ȭ ���� ����� ����
    // ��� 4 : �޸� �����丮 �׷��� ���� �� �� ����. Ư�� ������ �����͸� csv�Ǵ� json���� ��ȯ�Ͽ� ������ �� �ְ�, �����͸� �ҷ��ͼ� ����ȭ ��-�� ������ �� ��ȭ�� ���� Ȯ�� ����

    //---- ��� 1 ----
    private List<float> MemoryUsageData = new List<float>(); //�޸� ��뷮�� ����� List
    private float GraphWidth = 500.0f;//�׷��� ǥ�� �ʺ�
    private float GraphHeight = 200.0f;//�׷��� ǥ�� ����
    private float CurrentMemoryUsage = 0f;//���� �޸� ��뷮
    private List<float> GcTriggeredMemoryUsage = new List<float>();// �������÷��� ȣ�� �� �޸� ��뷮�� ����� List
    //---- ��� 2 ----
    private Rect rtArea = new Rect(10, 300, 500, 300);//������ ��ġ ǥ��â ��Ʈ������ 1
    private Rect rtArea2 = new Rect(510, 300, 500, 300);//������ ��ġ ǥ��â ��Ʈ������ 2
    private Vector2 ScrollPosition;//������ ��ġ ǥ��â ��ũ�Ѻ� 1
    private Vector2 ScrollPosition2;//������ ��ġ ǥ��â ��ũ�Ѻ� 2

    private float LastSampleTime = 0f;// ��Ȯ�� ������ ������ ����, Ÿ�̸� ��� ���ٹ���� ����غ���.
    //---- ��� 3-----
    private List<string> AllocationSources = new List<string>();//�޸� �Ҵ� �ҽ��� ����ϴ� ����Ʈ
    private List<string> GcTriggeredSources = new List<string>();// gc Ʈ���� �� � ���ҽ��� ���õǴ��� ����ϴ� ����Ʈ
    private Dictionary<int, string> ObjectAllocationMap = new Dictionary<int, string>(); //������Ʈ �ؽð��� �ҽ� ����
    
    // ---- ��� 4 ----
    private string FilePathCSV = "Asset/MemoryUsageData.csv";//csv���� ���� ���
    private string FilePathJSON = "Asset/MemoryUsageData.json";//json���� ���� ���
    private bool DataComparisonEnabled = false;
    private List<float> PreviouseMemoryUsageData = new List<float>();// �������� �ε�� �����͸� �����ϱ� ���� ����Ʈ
    private Rect rtArea3 = new Rect(1010, 300, 500, 300);//������Ʈ �� ���ҽ� �Ҵ� ��� â ��Ʈ������

    [SerializeField]
    private float SampleInterval = 1.0f; //���ø� ����(��)

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
        EditorApplication.update += UpdateMemoryUsage;// �̺�Ʈ �ڵ鷯 ���
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateMemoryUsage;//�̺�Ʈ �ڵ鷯 ����
    }

    private void UpdateMemoryUsage()// �޸� ��뷮 ������Ʈ �޼���. �޸� ��뷮�� �����ϰ� �ǽð����� �����͸� �����Ͽ� �׷����� ������Ʈ�� ��.
    {
        if (Time.realtimeSinceStartup - LastSampleTime >= SampleInterval)// ������ ���ø� ���� : �������� realtimeStartUp�� ���ø� ���͹��� ������ ������ ���� 0.1f�� ���ߴµ�, Ÿ�̸� ��� ���ٹ����� ������
        {
            LastSampleTime = Time.realtimeSinceStartup;

            CurrentMemoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);// ���ø����̼��� �Ҵ��� �� �޸� ��뷮��  1MB�� ������ MB������ ��ȯ. �̶� GetTotalMemory�� false�� �����Ͽ�, gc�� ������ �������� �ʴ´�.
            MemoryUsageData.Add(CurrentMemoryUsage);//�޸� ��뷮�� ������(mb����) ���

            if (MemoryUsageData.Count > 1 && MemoryUsageData[MemoryUsageData.Count - 2] - CurrentMemoryUsage > 10.0f)//���� �޸� ��뷮�� ���� �޸� ��뷮�� ���� 10mb �̻��� ��. �� �������÷��� ȣ�� �������� ���ֵ� ��
            {
                GcTriggeredMemoryUsage.Add(CurrentMemoryUsage);//�������÷��� ȣ�� �� �޸� ��뷮�� ���
                GcTriggeredSources.Add(AllocationSources.Count > 0 ? AllocationSources[AllocationSources.Count - 1] : "Unknown");// gcȣ�� ���� �Ҵ�� ���ҽ��� Ȯ��. ���ҽ� ���� �����ϸ� ����Ʈ�� �ε��� ���� �̿��� string���� ������ �� �ְ�, ����Ʈ�� ������� ��� �⺻���� unknown�� ���.       
            }

            if (MemoryUsageData.Count > GraphWidth) // �޸� �����Ͱ� �ʹ� �������� �׷����ʺ�(�ִ� ���� ��)�� �Ѿ��, ������ �����ʹ� �����Ѵ�.
                MemoryUsageData.RemoveAt(0);

            Repaint();//������ �ٽñ׸���
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

        Rect GraphRect = GUILayoutUtility.GetRect(GraphWidth, GraphHeight); // �׷����� �׸� ���簢�� ����(���μ���) / ������ : GraphRect.x / ���� : GraphRect.yMax     
        if (MemoryUsageData.Count > 1)
        {
            Handles.BeginGUI();//Handles�� �׸��� �׸��� �Լ��� ����� Ŭ����.
            {
                DrawGraph(GraphRect);
            }
            Handles.EndGUI();//���� �׷����� �ð�ȭ.
        }
        GUILayout.Space(10);
        //������ ���� �� �ε� ��ư �߰�
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
        for (int i = 1; i < MemoryUsageData.Count; i++)//��ϵ� �޸� ��뷮 �����͸�ŭ �ݺ�
        {
            // �׷��� ��ü Width ���̸� ����� �޸� ��뷮 ������ ������ ���� -> �� ������ ����Ʈ ������ ���� ������ ���Ѵ�. ������ ����Ʈ�� ���� �������� �󸶳� ������ �ִ��� ��Ÿ�� �� ����(���� �� �Ÿ� ����)
            float PrevX = GraphRect.x + (i - 1) * (GraphRect.width / MemoryUsageData.Count);//���� ����������Ʈ x��ǥ
            float CurrX = GraphRect.x + i * (GraphRect.width / MemoryUsageData.Count);//���� ����������Ʈ x��ǥ

            //�޸� ��뷮 �����͸� ���� ������ ����ȭ(100mb�� ��������). ��뷮 ��� �� �׷��� Height�� ���߱� ���� ���� ���� / yMax������ �޸� ��뷮�� ����� ����( (MemoryUsageData[i] / 100f) * GraphRect.height)�� ���� �޸� ��뷮�� Ŀ�� ���� y��ǥ�� ���� �ö� ��.
            float PrevY = GraphRect.yMax - (MemoryUsageData[i - 1] / 100.0f) * GraphRect.height;//���� ������ ����Ʈy��ǥ
            float CurrY = GraphRect.yMax - (MemoryUsageData[i] / 100.0f) * GraphRect.height;//���� ������ ����Ʈ y��ǥ

            // �޸� ���ҷ��� ũ�� GC ȣ��� �����ϰ� ���������� ǥ���Ѵ�.
            Handles.color = MemoryUsageData[i - 1] - MemoryUsageData[i] > 10.0f ? Color.red : Color.green;// ���� ��뷮 - ���� ��뷮�� ���� 10MB �̻��̸� GC ȣ��� ������.
            Handles.DrawLine(new Vector3(PrevX, PrevY), new Vector3(CurrX, CurrY));
        }
    }


    private void DisplayUsageData()// �׷��� �м� ��Ȳ�� ǥ���ϴ� �޼���
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
                    GUILayout.Label($"Current Memory Usage : " + CurrentMemoryUsage.ToString("F2") + " MB", CurrentLabel);// ���� �޸� ��뷮 ǥ��

                    GUILayout.Space(10);

                    EditorGUILayout.LabelField("Memory Usage History : ");
                    for (int i = 0; i < MemoryUsageData.Count; i++)
                    {
                        //�޸� ��� �̷� + � �ҽ��� �޸𸮸� �Ҵ��ߴ��� ǥ��. gc�� ȣ��� ������ � �޸� �Ҵ��� ������ �Ǿ����� ǥ��. unknown�� �����̳� �÷����ο� ���� �ڵ����� �޸𸮸� �Ҵ��ϴ� �����. �� ���� ������ ���� �޸� �Ҵ��� �ǹ�.
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
                        {//gc�� ȣ��� ������ ��ϵ� �޸� ��뷮�� � ���ҽ��� ������Ʈ�� �޸𸮸� �Ҵ��Ͽ� gc�� Ʈ���ŵǾ����� Ȯ��
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

    private void TrackAllocation(string allocationSource)//�޸� �Ҵ� ���� ������ ���� �޼���
    {
        //��ü ���� �Ǵ� �� �ε� �� ��, �޸� �Ҵ��� �߻��ϴ� �޼��� (Instantiate, Load ��)�� �����Ͽ� ���� ��� ���� ����
        AllocationSources.Add(allocationSource);
    }

    private void SaveMemoryUsageDataToCSV()//�����͸� csv�� �����ϴ� �޼���
    {
        using (StreamWriter writer = new StreamWriter(FilePathCSV))
        {
            writer.WriteLine("MemoryUsage, AllocationSource");
            for (int i = 0; i < MemoryUsageData.Count; i++)//��ϵ� �޸� ��뷮 ��ŭ �ݺ��ϸ�
            {
                string Source = i < AllocationSources.Count ? AllocationSources[i] : "Unknown";
                writer.WriteLine($"{MemoryUsageData[i]:F2}, {Source}");//�޸� ��뷮�� ��� �ҽ��� ���.
            }
        }
        EditorUtility.DisplayDialog("Save Memory Usage Data", "Save Memory Usage Data To CSV.", "OK");
        return;
    }

    private void SaveMemoryUsageDataToJSON()//�����͸� json���� �����ϴ� �޼���
    {
        //var MemoryData = new { MemoryUsage = MemoryUsageData, AllocationSources = AllocationSources };
        //string JsonData = JsonConvert.SerializeObject(MemoryData, Formatting.Indented);//�޸� ��뷮 �����͸� json���Ϸ� ����Ʈ
        //File.WriteAllText(FilePathJSON, JsonData);
        //EditorUtility.DisplayDialog("Save Memory Usage Data", "Save Memory Usage Data To JSON.", "OK");
        //return;
        MemoryData memoryData = new MemoryData { MemoryUsage = MemoryUsageData.ToArray(), AllocationSources = AllocationSources.ToArray() };
        string Json = JsonUtility.ToJson(memoryData, true);
        File.WriteAllText(FilePathJSON, Json);
        EditorUtility.DisplayDialog("Save Memory Usage Data", "Save Memory Usage Data To JSON.", "OK");
        return;

    }

    private void LoadMemoryUsageDataFromCSV()//csv������ �ε��ϴ� �޼���
    {
        if (File.Exists(FilePathCSV))//csv���ϰ�ο� ������ ������ ������ ���
        {
            PreviouseMemoryUsageData.Clear();//���� �޸𸮻�뷮 ������ ����Ʈ�� Ŭ����
            using (StreamReader reader = new StreamReader(FilePathCSV))//��Ʈ�� ������ ����Ͽ� ������ �д´�.
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

    private void LoadMemoryUsageDataFromJSON()//JSON������ �ε��ϴ� �޼���
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

    private void CompareMemoryUsageData()//����� �����Ϳ� ���� �����͸� ���ϴ� â�� ���
    {
        for(int i=0; i<Mathf.Min(MemoryUsageData.Count, PreviouseMemoryUsageData.Count);i++)//���� �����Ϳ� ���� ������ �� �� �� ���� �� ����ŭ �ݺ�
        {
            string Comparison = MemoryUsageData[i] > PreviouseMemoryUsageData[i] ? "Increased" : "Decreased";
            GUILayout.Label($"Time {i} : {MemoryUsageData[i]:F2}MB (Previous : {PreviouseMemoryUsageData[i]:F2}MB, {Comparison})");
        }
    }


}
