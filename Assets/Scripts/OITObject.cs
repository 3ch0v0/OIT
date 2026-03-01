using UnityEngine;

[ExecuteAlways] // 保证在 Editor 编辑模式下也能正常注册，方便你实时看效果
public class OITObject : MonoBehaviour
{
    [Tooltip("Target Algorithm")]
    public OITAlgorithm targetAlgorithm = OITAlgorithm.WBOIT;


    private OITAlgorithm m_RegisteredAlgorithm;

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

#if UNITY_EDITOR
    
    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        
        if (targetAlgorithm != m_RegisteredAlgorithm)
        {
            Unregister();
            Register();
        }
    }
#endif

    private void Register()
    {
        OITRegistry.Objects[targetAlgorithm].Add(this);
        m_RegisteredAlgorithm = targetAlgorithm;
    }

    private void Unregister()
    {
        if (OITRegistry.Objects.ContainsKey(m_RegisteredAlgorithm))
        {
            OITRegistry.Objects[m_RegisteredAlgorithm].Remove(this);
        }
    }
}