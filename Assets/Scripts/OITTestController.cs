using System.Collections.Generic;
using UnityEngine;
using TMPro; 

public class OITTestController : MonoBehaviour
{
   
    [System.Serializable]
    public struct TestGroup
    {
        public string OptionName;       
        public GameObject TargetObject; 
    }

    [Header("UI 引用")]
    [Tooltip("拖入你 Canvas 里的 TMP_Dropdown 组件")]
    public TMP_Dropdown algorithmDropdown;

    [Header("测试组配置")]
    public List<TestGroup> testGroups = new List<TestGroup>();

    private void Start()
    {
        if (algorithmDropdown == null)
        {
            Debug.LogError("OITTestController: 请在 Inspector 面板中绑定 Dropdown 组件！");
            return;
        }

        if (testGroups.Count == 0)
        {
            Debug.LogWarning("OITTestController: 测试组列表为空，请在面板中添加物体！");
            return;
        }

        
        algorithmDropdown.ClearOptions();

       
        List<string> options = new List<string>();
        foreach (var group in testGroups)
        {
            options.Add(group.OptionName);
        }
        algorithmDropdown.AddOptions(options);

       
        algorithmDropdown.onValueChanged.AddListener(OnAlgorithmChanged);

        
        OnAlgorithmChanged(algorithmDropdown.value);
    }

   
    private void OnAlgorithmChanged(int index)
    {
        for (int i = 0; i < testGroups.Count; i++)
        {
            if (testGroups[i].TargetObject != null)
            {
               
                bool shouldBeActive = (i == index);
                testGroups[i].TargetObject.SetActive(shouldBeActive);
            }
        }
    }

    private void OnDestroy()
    {
        
        if (algorithmDropdown != null)
        {
            algorithmDropdown.onValueChanged.RemoveListener(OnAlgorithmChanged);
        }
    }
}