using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryData// json파일 세이브, 로드를 위해 생성. 기존에는 Newtonsoft 네임스페이스에서 dynamic을 사용한 메서드를 썼는데, c# 버전에 따라 사용이 불가할 수 있기에 jsonutility로 대체
{
    public float[] MemoryUsage;
    public string[] AllocationSources;
}
