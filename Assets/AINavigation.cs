using UnityEngine;
using UnityEngine.AI;

public class AINavigation : MonoBehaviour
{
    public Transform target; // ?i?m ?�ch
    private NavMeshAgent agent; // ??i t??ng NavMeshAgent ?? ?i?u khi?n di chuy?n

    void Start()
    {
        agent = GetComponent<NavMeshAgent>(); // L?y component NavMeshAgent c?a ??i t??ng
        if (agent == null)
        {
            Debug.LogError("No NavMeshAgent component found on " + gameObject.name);
        }
        else
        {
            SetDestination(); // G?i h�m ?? thi?t l?p ?i?m ?�ch ban ??u
        }
    }

    void SetDestination()
    {
        if (target != null)
        {
            agent.SetDestination(target.position); // ??t ?i?m ?�ch cho NavMeshAgent
        }
    }

    void Update()
    {
        // Ki?m tra n?u ??i t??ng ?� ??n g?n ?i?m ?�ch, v� ??t l?i ?i?m ?�ch m?i
        if (agent.remainingDistance < 0.5f)
        {
            SetDestination(); // ??t l?i ?i?m ?�ch m?i
        }
    }
}