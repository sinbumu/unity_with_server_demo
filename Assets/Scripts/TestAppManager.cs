using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System;
using System.Net.WebSockets; // 웹소켓용
using System.Threading;
using System.Text;

public class TestAppManager : MonoBehaviour
{
    [Header("UI References")]
    public Text logDisplay;
    public InputField chatInput;

    private Process serverProcess;
    private ClientWebSocket webSocket;
    private string baseUrl = "http://127.0.0.1:23333"; // README 명시 포트

    void Start()
    {
        Application.targetFrameRate = 60;
        StartServer();
    }

    void OnApplicationQuit()
    {
        if (serverProcess != null && !serverProcess.HasExited)
            serverProcess.Kill();

        webSocket?.Dispose();
    }

    // --- [1] 서버 자동 실행 ---
    void StartServer()
    {
        string exePath = Path.Combine(Application.streamingAssetsPath, "dummy_backend.exe");
        try
        {
            serverProcess = new Process();
            serverProcess.StartInfo.FileName = exePath;
            serverProcess.StartInfo.Arguments = "--port 23333";
            serverProcess.StartInfo.WorkingDirectory = Application.streamingAssetsPath;

            // --- 백그라운드 실행을 위한 핵심 설정 ---
            serverProcess.StartInfo.UseShellExecute = false;      // 셸을 사용하지 않음
            serverProcess.StartInfo.CreateNoWindow = true;       // 창을 생성하지 않음
            serverProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // 창 스타일 숨김
                                                                             // ---------------------------------------

            serverProcess.Start();
            AddLog("System: 서버가 백그라운드에서 시작되었습니다.");
        }
        catch (Exception e)
        {
            AddLog($"Error: 서버 실행 실패 - {e.Message}");
        }
    }

    // --- [2] HTTP GET: Health & Config ---
    public void CallHealth() => StartCoroutine(GetRequest("/health"));
    public void CallConfig() => StartCoroutine(GetRequest("/configview"));

    IEnumerator GetRequest(string endpoint)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(baseUrl + endpoint))
        {
            yield return webRequest.SendWebRequest();
            HandleResponse(webRequest, endpoint);
        }
    }

    // --- [3] HTTP POST: Chat ---
    public void CallChat()
    {
        string message = string.IsNullOrEmpty(chatInput.text) ? "hello" : chatInput.text;
        string json = $"{{\"session_id\":\"s1\", \"text\":\"{message}\"}}";
        StartCoroutine(PostRequest("/chat", json));
    }

    IEnumerator PostRequest(string endpoint, string json)
    {
        var request = new UnityWebRequest(baseUrl + endpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        HandleResponse(request, endpoint);
    }

    // --- [4] WebSocket: /ws ---

    // 1. 연결 및 초기 메시지 전송
    public async void ConnectWebSocket()
    {
        // 이미 연결되어 있다면 새로 연결하지 않음
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            AddLog("WS: 이미 연결되어 있습니다. 메시지를 전송합니다.");
            SendWSMessage();
            return;
        }

        try
        {
            webSocket = new ClientWebSocket();
            Uri serverUri = new Uri("ws://127.0.0.1:23333/ws"); //
            AddLog("WS: 연결 시도 중...");

            await webSocket.ConnectAsync(serverUri, CancellationToken.None);
            AddLog("WS: 연결 성공!");

            // 연결 성공 직후, 인풋 필드의 텍스트를 바로 던짐
            SendWSMessage();

            // 수신 대기 루프 시작
            _ = ReceiveWSMessages();
        }
        catch (Exception e)
        {
            AddLog($"WS Error: {e.Message}");
        }
    }

    // 2. 메시지 전송 전용 함수
    public async void SendWSMessage()
    {
        if (webSocket == null || webSocket.State != WebSocketState.Open)
        {
            AddLog("WS: 연결이 되어 있지 않아 전송할 수 없습니다.");
            return;
        }

        string message = string.IsNullOrEmpty(chatInput.text) ? "WS Hello" : chatInput.text;
        byte[] buffer = Encoding.UTF8.GetBytes(message);

        try
        {
            // 서버로 메시지 전송
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            AddLog($"WS Sent: {message}");
        }
        catch (Exception e)
        {
            AddLog($"WS Send Error: {e.Message}");
        }
    }

    private async System.Threading.Tasks.Task ReceiveWSMessages()
    {
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            AddLog($"WS Received: {message}");
        }
    }

    // --- 공통 유틸리티 ---
    void HandleResponse(UnityWebRequest request, string endpoint)
    {
        if (request.result == UnityWebRequest.Result.Success)
            AddLog($"[{endpoint}] Success: {request.downloadHandler.text}");
        else
            AddLog($"[{endpoint}] Error: {request.error}");
    }

    void AddLog(string msg)
    {
        logDisplay.text += $"\n[{DateTime.Now:HH:mm:ss}] {msg}";
        UnityEngine.Debug.Log(msg); // 명시적으로 유니티의 Debug를 사용
    }
}