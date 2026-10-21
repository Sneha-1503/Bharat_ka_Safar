using UnityEngine;

public class SpriteScroller : MonoBehaviour
{
    public float speed = 0.5f;
    private float tileWidth;
    private Vector3 startPos;

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        Camera cam = Camera.main;
        float camWidth = 2f * cam.orthographicSize * cam.aspect;

        Vector2 s = sr.sprite.bounds.size;
        tileWidth = s.x * transform.localScale.x;
        int copies = Mathf.CeilToInt(camWidth / tileWidth) + 2;

        sr.size = new Vector2(s.x * copies, s.y);
        startPos = transform.position;
    }

    void Update()
    {
        float x = Mathf.Repeat(Time.time * speed, tileWidth);
        transform.position = startPos + Vector3.left * x;
    }
}