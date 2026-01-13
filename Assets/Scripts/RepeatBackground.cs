using UnityEngine;
public class RepeatBackground : MonoBehaviour
{
    private Vector3 startPos;
    private float repeatWidth;
    void Start()
    {
        // «апоминаем начальную позицию и ширину спрайта
        startPos = transform.position;
        repeatWidth = GetComponent<BoxCollider>().size.x / 2;
    }
    void Update()
    {
        // ѕровер€ем: если фон ушЄл слишком далеко влево Ч возвращаем его на старт
    if (transform.position.x < startPos.x - repeatWidth)
        {
            transform.position = startPos;
        }
    }
}
