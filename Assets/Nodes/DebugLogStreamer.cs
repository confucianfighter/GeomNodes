using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DebugLogStreamer : MonoBehaviour
{
    private Queue<string> logQueue = new Queue<string>();
    [SerializeField]
    TMPro.TMP_Text textMesh;
    private const int maxQueueSize = 100; // Maximum number of logs to keep in the queue

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        StartCoroutine(LogHelloWorldCoroutine());
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logQueue.Count >= maxQueueSize)
        {
            logQueue.Dequeue(); // Remove the oldest log if the queue is full
        }
        logQueue.Enqueue(logString);
    }

    public string[] GetLogs()
    {
        return logQueue.ToArray();
    }
    void Update()
    {

        if (logQueue.Count > 0)
        {
            var text = string.Join("\n", logQueue.ToArray());
            logQueue.Clear();
            textMesh.SetText(textMesh.text + "\n" + text);
        }
    }

    IEnumerator LogHelloWorldCoroutine()
    {
        while (true)
        {
            Debug.Log("Hello world");
            yield return new WaitForSeconds(1);
        }
    }
}
