using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

public static class VBBridgeCommands
{
    // ----- Handler Registry -----

    private static readonly Dictionary<string, Func<Dictionary<string, object>, Dictionary<string, object>>>
        HANDLERS = new Dictionary<string, Func<Dictionary<string, object>, Dictionary<string, object>>>()
    {
        ["ping"] = HandlePing,
        ["recompile"] = HandleRecompile,
        ["execute_menu_item"] = HandleExecuteMenuItem,
        ["enter_play_mode"] = HandleEnterPlayMode,
        ["exit_play_mode"] = HandleExitPlayMode,
        ["screenshot"] = HandleScreenshot,
        ["console_logs"] = HandleConsoleLogs,
        ["read_result"] = HandleReadResult,
        ["get_game_objects"] = HandleGetGameObjects,
        ["check_compile_status"] = HandleCheckCompileStatus,
    };

    // ----- Dispatch -----

    public static string Dispatch(string requestJson)
    {
        Dictionary<string, object> request = MiniJSON.Deserialize(requestJson) as Dictionary<string, object>;
        if (request == null)
            return SerializeResponse("error", null, "Failed to parse request JSON");

        string commandType = request.ContainsKey("type") ? request["type"].ToString() : "unknown";
        Dictionary<string, object> parameters = null;
        if (request.ContainsKey("params") && request["params"] is Dictionary<string, object>)
            parameters = (Dictionary<string, object>)request["params"];
        else
            parameters = new Dictionary<string, object>();

        Func<Dictionary<string, object>, Dictionary<string, object>> handler;
        if (HANDLERS.TryGetValue(commandType, out handler))
        {
            try
            {
                Dictionary<string, object> result = handler(parameters);
                return SerializeResponse("success", result, null);
            }
            catch (Exception e)
            {
                return SerializeResponse("error", null, e.Message);
            }
        }
        return SerializeResponse("error", null, "Unknown command: " + commandType);
    }

    // ----- Handlers -----

    static Dictionary<string, object> HandlePing(Dictionary<string, object> p)
    {
        return new Dictionary<string, object> { ["status"] = "success", ["result"] = "pong" };
    }

    static Dictionary<string, object> HandleRecompile(Dictionary<string, object> p)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        return new Dictionary<string, object> { ["refreshed"] = true };
    }

    static Dictionary<string, object> HandleExecuteMenuItem(Dictionary<string, object> p)
    {
        string menuPath = p.ContainsKey("menu_path") ? p["menu_path"].ToString() : "";
        bool ok = EditorApplication.ExecuteMenuItem(menuPath);
        return new Dictionary<string, object> { ["executed"] = ok, ["menu_path"] = menuPath };
    }

    static Dictionary<string, object> HandleEnterPlayMode(Dictionary<string, object> p)
    {
        EditorApplication.EnterPlaymode();
        return new Dictionary<string, object> { ["is_playing"] = true };
    }

    static Dictionary<string, object> HandleExitPlayMode(Dictionary<string, object> p)
    {
        EditorApplication.ExitPlaymode();
        return new Dictionary<string, object> { ["is_playing"] = false };
    }

    // ----- Deferred Screenshot State -----
    private static bool _pendingScreenshot = false;
    private static string _pendingScreenshotPath = null;
    private static int _screenshotWaitFrames = 0;
    private static readonly int MAX_SCREENSHOT_FRAMES = 120;

    static Dictionary<string, object> HandleScreenshot(Dictionary<string, object> p)
    {
        string path = p.ContainsKey("path") ? p["path"].ToString() : "Screenshots/vb_bridge_capture.png";
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(path);
        string fullPath = Path.GetFullPath(path);
        // Defer polling to EditorApplication.update so we don't block the main thread.
        _pendingScreenshot = true;
        _pendingScreenshotPath = fullPath;
        _screenshotWaitFrames = 0;
        EditorApplication.update += PollScreenshotFile;
        return new Dictionary<string, object> { ["path"] = fullPath, ["captured"] = "pending" };
    }

    static void PollScreenshotFile()
    {
        _screenshotWaitFrames++;
        if (File.Exists(_pendingScreenshotPath) || _screenshotWaitFrames >= MAX_SCREENSHOT_FRAMES)
        {
            EditorApplication.update -= PollScreenshotFile;
            bool success = File.Exists(_pendingScreenshotPath);
            string resultJson = "{\"path\":\"" + _pendingScreenshotPath.Replace("\\", "\\\\") + "\",\"captured\":" + (success ? "true" : "false") + "}";
            File.WriteAllText("Temp/vb_screenshot_result.json", resultJson);
            _pendingScreenshot = false;
        }
    }

    static Dictionary<string, object> HandleConsoleLogs(Dictionary<string, object> p)
    {
        int count = p.ContainsKey("count") ? Convert.ToInt32(p["count"]) : 50;
        string filter = p.ContainsKey("filter") ? p["filter"].ToString().ToLower() : "all";
        List<Dictionary<string, object>> logs = new List<Dictionary<string, object>>();

        // Collect via LogEntries reflection (internal Unity API)
        Type logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
        if (logEntriesType != null)
        {
            var getCount = logEntriesType.GetMethod("GetCount",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var startGetting = logEntriesType.GetMethod("StartGettingEntries",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var endGetting = logEntriesType.GetMethod("EndGettingEntries",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var getEntry = logEntriesType.GetMethod("GetEntryInternal",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            Type logEntryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");

            if (getCount != null)
            {
                int total = (int)getCount.Invoke(null, null);
                int start = Math.Max(0, total - count);

                if (startGetting != null) startGetting.Invoke(null, null);
                try
                {
                    for (int i = start; i < total; i++)
                    {
                        string message = "LogEntry_" + i;
                        string stackTrace = "";
                        string logType = "Log";

                        if (getEntry != null && logEntryType != null)
                        {
                            object entry = System.Activator.CreateInstance(logEntryType);
                            getEntry.Invoke(null, new object[] { i, entry });
                            var msgField = logEntryType.GetField("message");
                            if (msgField != null) message = msgField.GetValue(entry)?.ToString() ?? "";
                            var modeField = logEntryType.GetField("mode");
                            if (modeField != null)
                            {
                                int mode = (int)modeField.GetValue(entry);
                                if ((mode & (1 << 0)) != 0) logType = "Error";
                                else if ((mode & (1 << 1)) != 0) logType = "Assert";
                                else if ((mode & (1 << 9)) != 0) logType = "Warning";
                                else if ((mode & (1 << 21)) != 0) logType = "Exception";
                                else logType = "Log";
                            }
                        }

                        // Apply filter
                        if (filter != "all")
                        {
                            if (!logType.Equals(filter, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        logs.Add(new Dictionary<string, object>
                        {
                            ["message"] = message,
                            ["type"] = logType,
                            ["stackTrace"] = stackTrace
                        });
                    }
                }
                catch (Exception) { /* reflection may fail on some Unity versions */ }
                finally
                {
                    if (endGetting != null) endGetting.Invoke(null, null);
                }
            }
        }
        return new Dictionary<string, object> { ["logs"] = logs };
    }

    static Dictionary<string, object> HandleReadResult(Dictionary<string, object> p)
    {
        string resultPath = p.ContainsKey("path") ? p["path"].ToString() : "Temp/vb_result.json";
        if (!File.Exists(resultPath))
            return new Dictionary<string, object> { ["exists"] = false, ["content"] = null };
        string content = File.ReadAllText(resultPath);
        object parsed = MiniJSON.Deserialize(content);
        return new Dictionary<string, object> { ["exists"] = true, ["content"] = parsed };
    }

    static Dictionary<string, object> HandleGetGameObjects(Dictionary<string, object> p)
    {
        List<Dictionary<string, object>> roots = new List<Dictionary<string, object>>();
        foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            roots.Add(SerializeGameObject(go));
        }
        return new Dictionary<string, object> { ["game_objects"] = roots };
    }

    static Dictionary<string, object> HandleCheckCompileStatus(Dictionary<string, object> p)
    {
        bool isCompiling = EditorApplication.isCompiling;
        bool hasErrors = false;
        int errorCount = 0;
        List<string> errorMessages = new List<string>();

        // Check console logs for compile errors
        Type logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
        if (logEntriesType != null)
        {
            var getCount = logEntriesType.GetMethod("GetCount",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var startGetting = logEntriesType.GetMethod("StartGettingEntries",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var endGetting = logEntriesType.GetMethod("EndGettingEntries",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var getEntry = logEntriesType.GetMethod("GetEntryInternal",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            Type logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");

            if (getCount != null)
            {
                int total = (int)getCount.Invoke(null, null);
                if (startGetting != null) startGetting.Invoke(null, null);
                try
                {
                    for (int i = 0; i < total; i++)
                    {
                        if (getEntry != null && logEntryType != null)
                        {
                            object entry = System.Activator.CreateInstance(logEntryType);
                            getEntry.Invoke(null, new object[] { i, entry });
                            var modeField = logEntryType.GetField("mode");
                            if (modeField != null)
                            {
                                int mode = (int)modeField.GetValue(entry);
                                // Bit 0 = Error, bit 21 = Exception
                                if ((mode & (1 << 0)) != 0 || (mode & (1 << 21)) != 0)
                                {
                                    var msgField = logEntryType.GetField("message");
                                    string msg = msgField != null ? msgField.GetValue(entry)?.ToString() ?? "" : "";
                                    // Filter to compile errors (CS prefix or 'error' keyword)
                                    if (msg.Contains("CS") || msg.Contains("error") || msg.Contains("Error"))
                                    {
                                        hasErrors = true;
                                        errorCount++;
                                        if (errorMessages.Count < 20)
                                            errorMessages.Add(msg.Length > 200 ? msg.Substring(0, 200) : msg);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception) { /* reflection may fail */ }
                finally
                {
                    if (endGetting != null) endGetting.Invoke(null, null);
                }
            }
        }

        var result = new Dictionary<string, object>
        {
            ["is_compiling"] = isCompiling,
            ["has_errors"] = hasErrors,
            ["error_count"] = errorCount,
            ["errors"] = errorMessages
        };
        return result;
    }

    // ----- Helpers -----

    static Dictionary<string, object> SerializeGameObject(GameObject go)
    {
        var result = new Dictionary<string, object>
        {
            ["name"] = go.name,
            ["active"] = go.activeSelf
        };

        // Components
        var comps = new List<string>();
        foreach (Component c in go.GetComponents<Component>())
        {
            if (c != null) comps.Add(c.GetType().Name);
        }
        result["components"] = comps;

        // Children (recursive)
        var children = new List<Dictionary<string, object>>();
        for (int i = 0; i < go.transform.childCount; i++)
        {
            children.Add(SerializeGameObject(go.transform.GetChild(i).gameObject));
        }
        result["children"] = children;

        return result;
    }

    static string SerializeResponse(string status, Dictionary<string, object> result, string message)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{\"status\":\"");
        sb.Append(EscapeJsonValue(status));
        sb.Append("\"");

        if (result != null)
        {
            sb.Append(",\"result\":");
            sb.Append(MiniJSON.Serialize(result));
        }

        if (message != null)
        {
            sb.Append(",\"message\":\"");
            sb.Append(EscapeJsonValue(message));
            sb.Append("\"");
        }

        sb.Append("}");
        return sb.ToString();
    }

    static string EscapeJsonValue(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    // =================================================================
    // MiniJSON -- Lightweight JSON parser (MIT License)
    // Embedded because JsonUtility cannot handle Dictionary<string,object>
    // =================================================================

    public static class MiniJSON
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return Parser.Parse(json);
        }

        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        sealed class Parser : IDisposable
        {
            StringReader _reader;

            Parser(string jsonString) { _reader = new StringReader(jsonString); }
            public void Dispose() { _reader.Dispose(); }

            public static object Parse(string jsonString)
            {
                using (var p = new Parser(jsonString)) { return p.ParseValue(); }
            }

            object ParseValue()
            {
                EatWhitespace();
                int peek = _reader.Peek();
                if (peek == -1) return null;
                char c = (char)peek;
                if (c == '{') return ParseObject();
                if (c == '[') return ParseArray();
                if (c == '"') return ParseString();
                if (c == '-' || char.IsDigit(c)) return ParseNumber();
                string word = ParseWord();
                if (word == "true") return true;
                if (word == "false") return false;
                if (word == "null") return null;
                return word;
            }

            Dictionary<string, object> ParseObject()
            {
                _reader.Read(); // consume opening brace
                var dict = new Dictionary<string, object>();
                while (true)
                {
                    EatWhitespace();
                    int peek = _reader.Peek();
                    if (peek == -1) break;
                    if ((char)peek == '}') { _reader.Read(); break; }
                    if ((char)peek == ',') { _reader.Read(); continue; }
                    string key = ParseString();
                    EatWhitespace();
                    _reader.Read(); // :
                    dict[key] = ParseValue();
                }
                return dict;
            }

            List<object> ParseArray()
            {
                _reader.Read(); // [
                var list = new List<object>();
                while (true)
                {
                    EatWhitespace();
                    int peek = _reader.Peek();
                    if (peek == -1) break;
                    if ((char)peek == ']') { _reader.Read(); break; }
                    if ((char)peek == ',') { _reader.Read(); continue; }
                    list.Add(ParseValue());
                }
                return list;
            }

            string ParseString()
            {
                _reader.Read(); // opening quote
                var sb = new StringBuilder();
                while (true)
                {
                    int c = _reader.Read();
                    if (c == -1 || c == '"') break;
                    if (c == '\\')
                    {
                        int next = _reader.Read();
                        switch ((char)next)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            default: sb.Append((char)next); break;
                        }
                    }
                    else sb.Append((char)c);
                }
                return sb.ToString();
            }

            object ParseNumber()
            {
                var sb = new StringBuilder();
                bool isFloat = false;
                while (true)
                {
                    int peek = _reader.Peek();
                    if (peek == -1) break;
                    char c = (char)peek;
                    if (c == '.' || c == 'e' || c == 'E') isFloat = true;
                    if (char.IsDigit(c) || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E')
                    { sb.Append(c); _reader.Read(); }
                    else break;
                }
                string numStr = sb.ToString();
                if (isFloat) { double d; double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d); return d; }
                else { long l; long.TryParse(numStr, out l); return l; }
            }

            string ParseWord()
            {
                var sb = new StringBuilder();
                while (true)
                {
                    int peek = _reader.Peek();
                    if (peek == -1) break;
                    char c = (char)peek;
                    if (char.IsLetterOrDigit(c)) { sb.Append(c); _reader.Read(); }
                    else break;
                }
                return sb.ToString();
            }

            void EatWhitespace()
            {
                while (true)
                {
                    int peek = _reader.Peek();
                    if (peek == -1) break;
                    if (!char.IsWhiteSpace((char)peek)) break;
                    _reader.Read();
                }
            }

            class StringReader : IDisposable
            {
                string _s; int _pos;
                public StringReader(string s) { _s = s ?? ""; _pos = 0; }
                public int Peek() { return _pos < _s.Length ? _s[_pos] : -1; }
                public int Read() { return _pos < _s.Length ? _s[_pos++] : -1; }
                public void Dispose() { }
            }
        }

        sealed class Serializer
        {
            StringBuilder _sb = new StringBuilder();

            public static string Serialize(object obj)
            {
                var s = new Serializer();
                s.SerializeValue(obj);
                return s._sb.ToString();
            }

            void SerializeValue(object val)
            {
                if (val == null) { _sb.Append("null"); return; }
                if (val is string s) { SerializeString(s); return; }
                if (val is bool b) { _sb.Append(b ? "true" : "false"); return; }
                if (val is IDictionary<string, object> dict) { SerializeDict(dict); return; }
                if (val is IList<object> list) { SerializeList(list); return; }
                if (val is IList<string> slist) { SerializeStringList(slist); return; }
                if (val is IList<Dictionary<string, object>> dlist) { SerializeDictList(dlist); return; }
                _sb.Append(Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture));
            }

            void SerializeString(string s)
            {
                _sb.Append('"');
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '"': _sb.Append("\\\""); break;
                        case '\\': _sb.Append("\\\\"); break;
                        case '\n': _sb.Append("\\n"); break;
                        case '\r': _sb.Append("\\r"); break;
                        case '\t': _sb.Append("\\t"); break;
                        default: _sb.Append(c); break;
                    }
                }
                _sb.Append('"');
            }

            void SerializeDict(IDictionary<string, object> dict)
            {
                _sb.Append('{');
                bool first = true;
                foreach (var kv in dict)
                {
                    if (!first) _sb.Append(',');
                    SerializeString(kv.Key);
                    _sb.Append(':');
                    SerializeValue(kv.Value);
                    first = false;
                }
                _sb.Append('}');
            }

            void SerializeList(IList<object> list)
            {
                _sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) _sb.Append(',');
                    SerializeValue(list[i]);
                }
                _sb.Append(']');
            }

            void SerializeStringList(IList<string> list)
            {
                _sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) _sb.Append(',');
                    SerializeString(list[i]);
                }
                _sb.Append(']');
            }

            void SerializeDictList(IList<Dictionary<string, object>> list)
            {
                _sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) _sb.Append(',');
                    SerializeDict(list[i]);
                }
                _sb.Append(']');
            }
        }
    }
}

