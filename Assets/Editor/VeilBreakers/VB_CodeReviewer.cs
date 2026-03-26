using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace VeilBreakers.Editor.CodeReview
{
    // =========================================================================
    //  Data Structures
    // =========================================================================

    public enum Severity { CRITICAL, HIGH, MEDIUM, LOW }
    public enum Category { Bug, Performance, Security, Unity, Quality, Optimization }
    public enum Language { CSharp, Python }
    public enum RuleScope { HotPath, AnyMethod, ClassLevel, FileLevel }
    public enum FileFilter { Runtime, EditorOnly, All }
    public enum FindingType { Error, Bug, Optimization, Strengthening }

    // Pre-classified line context -- computed ONCE per file, O(n) pass
    public enum LineContext { Cold, HotPath, Comment, StringLiteral, EditorBlock, Attribute }

    [Serializable]
    public sealed class ReviewRule
    {
        public string Id;
        public Severity Severity;
        public Category Category;
        public Language Lang;
        public RuleScope Scope;
        public FileFilter Filter;
        public FindingType Type;           // Error/Bug/Optimization/Strengthening
        public string Description;
        public string Fix;
        public int Confidence;             // 0-100: how certain this finding is real
        public int Priority;               // 0-100: 0=cosmetic, 100=critical/crash
        public Regex Pattern;
        public Regex[] AntiPatterns;       // suppress match if ANY anti-pattern matches nearby
        public int AntiPatternRadius;      // how many lines around match to check anti-patterns (default 3)
        public Func<string, string[], int, LineContext[], bool> ContextGuard; // optional extra guard
        public string Reasoning; // explains thought process for low-confidence findings
        public string FastCheck; // literal substring for fast pre-filtering before regex
        public Regex InsidePattern;    // only fire if inside a method whose signature matches this
        public Regex NotInsidePattern; // suppress if inside a method whose signature matches this

        public ReviewRule(string id, Severity sev, Category cat, Language lang,
                          RuleScope scope, FileFilter filter,
                          string desc, string fix, string pattern,
                          RegexOptions opts = RegexOptions.None,
                          string[] antiPatterns = null, int antiRadius = 3,
                          Func<string, string[], int, LineContext[], bool> guard = null,
                          FindingType type = FindingType.Bug,
                          int confidence = -1, int priority = -1,
                          string reasoning = null,
                          string insidePattern = null, string notInsidePattern = null)
        {
            Id = id; Severity = sev; Category = cat; Lang = lang;
            Scope = scope; Filter = filter;
            Description = desc; Fix = fix;
            Type = type;
            // Auto-derive confidence + priority from severity when not specified
            Confidence = confidence >= 0 ? confidence
                : sev == Severity.CRITICAL ? 95 : sev == Severity.HIGH ? 85 : sev == Severity.MEDIUM ? 75 : 70;
            Priority = priority >= 0 ? priority
                : sev == Severity.CRITICAL ? 95 : sev == Severity.HIGH ? 75 : sev == Severity.MEDIUM ? 50 : 20;
            // Auto-derive type from category when default
            if (type == FindingType.Bug && cat == Category.Performance) Type = FindingType.Optimization;
            if (type == FindingType.Bug && cat == Category.Quality) Type = FindingType.Strengthening;
            if (cat == Category.Security) Type = FindingType.Error;
            Reasoning = reasoning;
            if (insidePattern != null) InsidePattern = new Regex(insidePattern, RegexOptions.Compiled);
            if (notInsidePattern != null) NotInsidePattern = new Regex(notInsidePattern, RegexOptions.Compiled);
            Pattern = new Regex(pattern, opts | RegexOptions.Compiled);
            AntiPatternRadius = antiRadius;
            ContextGuard = guard;
            if (antiPatterns != null && antiPatterns.Length > 0)
            {
                AntiPatterns = new Regex[antiPatterns.Length];
                for (int i = 0; i < antiPatterns.Length; i++)
                    AntiPatterns[i] = new Regex(antiPatterns[i], RegexOptions.Compiled);
            }
            // Auto-derive FastCheck: extract longest literal word (4+ chars) from pattern
            // Skip if pattern has alternation (A|B) -- would only match first branch
            // Strip regex metachar escapes first so \bDictionary doesn't become "bDictionary"
            // Skip FastCheck entirely for negative lookahead patterns (?!) -- extracted word inverts logic
            if (FastCheck == null && !pattern.Contains("(?!"))
            {
                bool hasAlternation = pattern.Contains("|");
                if (!hasAlternation)
                {
                    string cleaned = System.Text.RegularExpressions.Regex.Replace(
                        pattern, @"\\[bBdDsSwW.*+?^$|{}()\[\]]", " ");
                    var candidates = System.Text.RegularExpressions.Regex.Matches(
                        cleaned, @"[A-Za-z_][A-Za-z0-9_]{3,}");
                    string best = null;
                    for (int ci = 0; ci < candidates.Count; ci++)
                        if (best == null || candidates[ci].Value.Length > best.Length)
                            best = candidates[ci].Value;
                    if (best != null) FastCheck = best;
                }
                // For alternation patterns, only use FastCheck if there's a common prefix/suffix
                // outside the alternation group (e.g. "GetComponent" before "<(A|B)>")
                else
                {
                    // Extract text before first ( or | as potential common prefix
                    int firstGroup = pattern.IndexOfAny(new[] { '(', '|' });
                    if (firstGroup > 4)
                    {
                        string prefix = pattern.Substring(0, firstGroup);
                        string cleaned = System.Text.RegularExpressions.Regex.Replace(
                            prefix, @"\\[bBdDsSwW.*+?^$|{}()\[\]]", " ");
                        var m = System.Text.RegularExpressions.Regex.Match(
                            cleaned, @"[A-Za-z_][A-Za-z0-9_]{3,}");
                        if (m.Success) FastCheck = m.Value;
                    }
                }
            }
        }
    }

    [Serializable]
    public sealed class ReviewIssue
    {
        public string RuleId;
        public Severity Severity;
        public Category Category;
        public Language Lang;
        public FindingType Type;           // Error/Bug/Optimization/Strengthening
        public int Confidence;             // 0-100: certainty this is real (95=CERTAIN, 75=HIGH, 50=LIKELY, <50=POSSIBLE)
        public int Priority;               // 0-100: 0=cosmetic, 100=critical/crash
        public string FilePath;
        public int Line;
        public string Description;
        public string Fix;                 // Best-practice fix suggestion
        public string MatchedText;
        public string Reasoning;           // Explains thought process when Confidence < 70%
        public string CodeContext;          // Surrounding code lines (3 before + 3 after)

        // Human-readable labels
        public string ConfidenceLabel => Confidence >= 90 ? "CERTAIN" : Confidence >= 75 ? "HIGH" : Confidence >= 50 ? "LIKELY" : "POSSIBLE";
        public string PriorityLabel => Priority >= 90 ? "P0-CRITICAL" : Priority >= 70 ? "P1-HIGH" : Priority >= 40 ? "P2-MEDIUM" : Priority >= 15 ? "P3-LOW" : "P4-COSMETIC";
        public string TypeLabel => Type == FindingType.Error ? "ERROR/BUG" : Type == FindingType.Bug ? "BUG" : Type == FindingType.Optimization ? "OPTIMIZE" : "STRENGTHEN";
    }

    // =========================================================================
    //  Pre-Scan: Classify every line's context ONCE per file
    // =========================================================================

    public static class LineClassifier
    {
        static readonly Regex HotMethodSig = new Regex(
            @"^\s*(private\s+|protected\s+|public\s+|internal\s+)?(override\s+)?void\s+(Update|LateUpdate|FixedUpdate|OnGUI|OnAnimatorMove|OnAnimatorIK)\s*\(",
            RegexOptions.Compiled);
        static readonly Regex AnyMethodSig = new Regex(
            @"^\s*(private|protected|public|internal|static|override|virtual|abstract|async|void|int|float|bool|string|Task|IEnumerator|\w+)\s+\w+\s*\(",
            RegexOptions.Compiled);
        static readonly Regex AttrLine = new Regex(@"^\s*\[", RegexOptions.Compiled);
        static readonly Regex EditorIfStart = new Regex(@"#if\s+UNITY_EDITOR", RegexOptions.Compiled);
        static readonly Regex EndIf = new Regex(@"#endif", RegexOptions.Compiled);

        public static LineContext[] Classify(string[] lines)
        {
            var ctx = new LineContext[lines.Length];
            bool inHotMethod = false;
            int hotBraceDepth = 0;
            bool inEditorBlock = false;
            bool inBlockComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();

                // Block comment tracking
                if (inBlockComment)
                {
                    ctx[i] = LineContext.Comment;
                    if (trimmed.Contains("*/")) inBlockComment = false;
                    continue;
                }
                if (trimmed.StartsWith("/*"))
                {
                    inBlockComment = !trimmed.Contains("*/");
                    ctx[i] = LineContext.Comment;
                    continue;
                }

                // Single-line comment (string-aware)
                if (IsLineComment(line) || trimmed.StartsWith("*"))
                {
                    ctx[i] = LineContext.Comment;
                    continue;
                }

                // Attribute lines
                if (AttrLine.IsMatch(trimmed) && !trimmed.Contains("="))
                {
                    ctx[i] = LineContext.Attribute;
                    continue;
                }

                // #if UNITY_EDITOR blocks
                if (EditorIfStart.IsMatch(trimmed)) { inEditorBlock = true; }
                if (EndIf.IsMatch(trimmed) && inEditorBlock) { inEditorBlock = false; ctx[i] = LineContext.EditorBlock; continue; }
                if (inEditorBlock) { ctx[i] = LineContext.EditorBlock; continue; }

                // Hot method tracking
                if (HotMethodSig.IsMatch(trimmed))
                {
                    inHotMethod = true;
                    hotBraceDepth = 0;
                }
                else if (inHotMethod)
                {
                    // If we hit a different method signature, leave hot path
                    if (AnyMethodSig.IsMatch(trimmed) && !HotMethodSig.IsMatch(trimmed))
                    {
                        inHotMethod = false;
                        hotBraceDepth = 0;
                    }
                }

                if (inHotMethod)
                {
                    hotBraceDepth += CountChar(line, '{') - CountChar(line, '}');
                    if (hotBraceDepth <= 0 && i > 0 && line.Contains("}"))
                    {
                        inHotMethod = false;
                        hotBraceDepth = 0;
                    }
                    ctx[i] = LineContext.HotPath;
                }
                else
                {
                    ctx[i] = LineContext.Cold;
                }
            }

            // Phase 2: Transitive hot-path propagation (Rider-inspired)
            // If Update() calls DoMovement(), and DoMovement() calls CheckCollisions(),
            // then CheckCollisions() lines should also be marked HotPath.
            var methods = new Dictionary<string, (int start, int end, bool isHot)>();
            string currentMethod = null;
            int methodStart = -1, methodDepth = 0;
            bool methodIsHot = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (ctx[i] == LineContext.Comment) continue;
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//")) continue;
                var methodMatch = Regex.Match(trimmed,
                    @"^(?:(?:private|protected|public|internal|static|override|virtual|abstract|async|sealed|new|partial)\s+)*(void|Task|int|float|bool|string|IEnumerator|\w+)\s+(\w+)\s*\(");
                if (methodMatch.Success)
                {
                    string mName = methodMatch.Groups[2].Value;
                    if (currentMethod != null && methodStart >= 0)
                        methods[currentMethod] = (methodStart, i - 1, methodIsHot);
                    currentMethod = mName;
                    methodStart = i;
                    methodIsHot = ctx[i] == LineContext.HotPath;
                    methodDepth = 0;
                }
                if (currentMethod != null)
                {
                    methodDepth += CountChar(lines[i], '{') - CountChar(lines[i], '}');
                    if (methodDepth <= 0 && i > methodStart && lines[i].Contains("}"))
                    {
                        methods[currentMethod] = (methodStart, i, methodIsHot);
                        currentMethod = null;
                    }
                }
            }
            if (currentMethod != null && methodStart >= 0)
                methods[currentMethod] = (methodStart, lines.Length - 1, methodIsHot);

            // Build call graph: method -> set of called methods (intra-file only)
            var callGraph = new Dictionary<string, HashSet<string>>();
            foreach (var kvp in methods)
            {
                var calls = new HashSet<string>();
                for (int i = kvp.Value.start; i <= kvp.Value.end && i < lines.Length; i++)
                {
                    if (ctx[i] == LineContext.Comment) continue;
                    foreach (Match cm in Regex.Matches(lines[i], @"\b(\w+)\s*\("))
                    {
                        if (IsMatchInString(lines[i], cm.Index)) continue;
                        string callee = cm.Groups[1].Value;
                        if (methods.ContainsKey(callee) && callee != kvp.Key)
                            calls.Add(callee);
                    }
                }
                callGraph[kvp.Key] = calls;
            }

            // BFS from known hot methods to find transitively-reachable methods
            var hotMethods = new HashSet<string>(
                methods.Where(m => m.Value.isHot).Select(m => m.Key));
            var queue = new Queue<string>(hotMethods);
            while (queue.Count > 0)
            {
                string method = queue.Dequeue();
                if (!callGraph.ContainsKey(method)) continue;
                foreach (string callee in callGraph[method])
                {
                    if (!hotMethods.Contains(callee))
                    {
                        hotMethods.Add(callee);
                        queue.Enqueue(callee);
                    }
                }
            }

            // Re-classify transitively hot methods
            foreach (string hotMethod in hotMethods)
            {
                if (!methods.ContainsKey(hotMethod)) continue;
                var (start, end, wasHot) = methods[hotMethod];
                if (wasHot) continue; // Already classified
                for (int i = start; i <= end && i < ctx.Length; i++)
                {
                    if (ctx[i] == LineContext.Cold)
                        ctx[i] = LineContext.HotPath;
                }
            }

            return ctx;
        }

        // Check if the apparent '//' is actually a line comment (not inside a string)
        // Handles both regular strings ("...") and verbatim strings (@"...")
        static bool IsLineComment(string line)
        {
            bool inStr = false; bool inVerbatim = false; bool escaped = false;
            for (int c = 0; c < line.Length - 1; c++)
            {
                char ch = line[c];
                if (inVerbatim)
                {
                    if (ch == '"') { if (c + 1 < line.Length && line[c + 1] == '"') c++; else inVerbatim = false; }
                    continue;
                }
                if (escaped) { escaped = false; continue; }
                if (ch == '\\' && inStr) { escaped = true; continue; }
                if (ch == '@' && c + 1 < line.Length && line[c + 1] == '"' && !inStr) { inVerbatim = true; c++; continue; }
                if (ch == '"') inStr = !inStr;
                if (!inStr && ch == '/' && line[c + 1] == '/') return true;
            }
            return false;
        }

        // Count occurrences of char, skipping string literals (regular + verbatim)
        public static int CountChar(string s, char c)
        {
            int n = 0;
            bool inStr = false; bool inVerbatim = false; bool escaped = false;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (inVerbatim)
                {
                    if (ch == '"') { if (i + 1 < s.Length && s[i + 1] == '"') i++; else inVerbatim = false; }
                    continue;
                }
                if (escaped) { escaped = false; continue; }
                if (ch == '\\' && inStr) { escaped = true; continue; }
                if (ch == '@' && i + 1 < s.Length && s[i + 1] == '"' && !inStr) { inVerbatim = true; i++; continue; }
                if (ch == '"') { inStr = !inStr; continue; }
                if (!inStr && ch == c) n++;
            }
            return n;
        }

        // Check if a regex match position falls inside a quoted string on the line
        public static bool IsMatchInString(string line, int matchPos)
        {
            bool inSingle = false, inDouble = false;
            bool escaped = false;
            for (int k = 0; k < line.Length && k <= matchPos; k++)
            {
                if (escaped) { escaped = false; continue; }
                char c = line[k];
                if (c == '\\') { escaped = true; continue; }
                if (c == '\'' && !inDouble) inSingle = !inSingle;
                else if (c == '"' && !inSingle) inDouble = !inDouble;
                if (k == matchPos) return inSingle || inDouble;
            }
            return false;
        }
    }

    // =========================================================================
    //  Rule Definitions -- 115 C# + 33 Python
    // =========================================================================

    public static class ReviewRules
    {
        // Shorthand helpers for guards
        public static bool MatchesScope(LineContext ctx, RuleScope scope)
        {
            if (scope == RuleScope.HotPath) return ctx == LineContext.HotPath;
            if (scope == RuleScope.AnyMethod) return ctx != LineContext.Comment && ctx != LineContext.Attribute;
            return ctx != LineContext.Comment;
        }


        // ---- FixedUpdate helper for BUG-17 ----
        static bool InFixedUpdate(string line, string[] all, int idx, LineContext[] ctx)
        {
            for (int i = idx; i >= 0; i--)
            {
                if (Regex.IsMatch(all[i], @"void\s+FixedUpdate\s*\(")) return true;
                if (i < idx && LineClassifier.CountChar(all[i], '{') > 0 && i < idx - 1) break;
            }
            return false;
        }

        // Count brace-delimited body length from a method/class declaration
        static int BodyLength(string[] all, int start)
        {
            int depth = 0;
            for (int j = start; j < all.Length; j++)
            {
                depth += LineClassifier.CountChar(all[j], '{') - LineClassifier.CountChar(all[j], '}');
                if (depth <= 0 && j > start) return j - start;
            }
            return 0;
        }

        // =====================================================================
        //  C# RULES (105 rules: BUG 1-33, PERF 1-22, SEC 1-10,
        //            UNITY 1-17, QUAL 1-23, DEEP 1-6)
        // =====================================================================

        public static readonly ReviewRule[] CSharpRules = new ReviewRule[]
        {
            // ---- BUG DETECTION (1-33) ----

            new ReviewRule("BUG-01", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "GetComponent<T>() in Update/LateUpdate/FixedUpdate -- cache in Awake/Start",
                "Cache the component reference in a field during Awake() or Start().",
                @"GetComponent\s*<",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"(Awake|Start|OnEnable)\s*\(", @"private\s+\w+\s+_\w+\s*=\s*GetComponent", @"/Editor/" }),

            new ReviewRule("BUG-02", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Camera.main in Update -- calls FindGameObjectWithTag internally",
                "Cache Camera.main in a field during Start().",
                @"Camera\.main",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"_\w*(cam|camera)\w*\s*=\s*Camera\.main" }),

            new ReviewRule("BUG-03", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "FindObjectOfType in Update -- O(n) scene scan every frame",
                "Cache the result in Start() or use a singleton/service locator pattern.",
                @"FindObjectOfType\s*[<(]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("BUG-04", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Heap allocation (new List/Dictionary/HashSet) inside Update",
                "Pre-allocate collections as fields and Clear() them in Update instead.",
                @"new\s+(List|Dictionary|HashSet|Queue|Stack|LinkedList)\s*<",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\.Clear\s*\(\s*\)" }),

            // BUG-05 FP fix: Only flag string concat with + if BOTH sides are string expressions
            new ReviewRule("BUG-05", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "String concatenation with + in Update -- allocates new string each frame",
                "Use StringBuilder, string.Format, or interpolation cached outside the loop.",
                @"(?:""[^""]*""\s*\+\s*(?:""|\w+\.ToString|\w+\s*\+\s*""))|(?:\w+\.ToString\s*\(\s*\)\s*\+\s*"")",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"const\s+string", @"StringBuilder" }),

            new ReviewRule("BUG-06", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "GameObject.Find() in Update -- string-based scene search every frame",
                "Cache the reference in Start() or Awake().",
                @"GameObject\.Find\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("BUG-07", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "transform.position/.rotation accessed multiple times -- crosses native boundary",
                "Cache transform.position/rotation in a local variable.",
                @"transform\.(position|rotation)\s*[;=\.\[].*transform\.(position|rotation)",
                RegexOptions.Singleline,
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"var\s+\w+\s*=\s*transform\.(position|rotation)" }),

            // BUG-08: forward guard -- only flag if code continues after Destroy
            new ReviewRule("BUG-08", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Accessing member after Destroy(gameObject) -- object is destroyed",
                "Add 'return;' after Destroy(gameObject) or ensure no member access follows.",
                @"Destroy\s*\(\s*gameObject\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    for (int j = i + 1; j < Math.Min(i + 4, all.Length); j++) {
                        string next = all[j].Trim();
                        if (next == "" || next.StartsWith("//")) continue;
                        if (next.StartsWith("return") || next == "}" || next.StartsWith("break")) return false;
                        return true;
                    }
                    return false;
                }),

            new ReviewRule("BUG-09", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Missing null check after GetComponent -- may return null",
                "Always null-check the result of GetComponent before use.",
                @"=\s*GetComponent\s*<[^>]+>\s*\(\s*\)\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"if\s*\(\s*\w+\s*[!=]=\s*null", @"Debug\.(Log|Assert)", @"\?\." }),

            new ReviewRule("BUG-10", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "'is null' on UnityEngine.Object -- Unity overloads == for destroyed object check",
                "Use '== null' instead of 'is null' for Unity objects.",
                @"\bis\s+null\b",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"System\.", @"struct\s",
                                     @"string\s", @"List<", @"Dictionary<", @"I[A-Z]\w+\s" },
                confidence: 55,
                reasoning: "'is null' is correct for plain C# objects, strings, interfaces, and generics. Only wrong when used on UnityEngine.Object subclasses where == null checks for destroyed objects."),

            // BUG-11: already excludes Start/Awake/OnXxx event handlers
            new ReviewRule("BUG-11", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "async void method -- exceptions are silently swallowed",
                "Use async Task/UniTask instead; only async void for event handlers.",
                @"async\s+void\s+(?!On[A-Z]|Start|Awake|Handle|Button_|Btn_)\w+\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"ICommand", @"EventHandler" }),

            // BUG-12 FP fix: Only flag if result NOT stored AND name suggests infinite loop
            new ReviewRule("BUG-12", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Coroutine started but never stopped -- potential memory leak",
                "Store the Coroutine reference and StopCoroutine in OnDisable/OnDestroy.",
                @"StartCoroutine\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"=\s*StartCoroutine", @"StopCoroutine", @"StopAllCoroutines" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Only flag if name suggests infinite: Loop, Repeat, Continuous, Forever
                    return Regex.IsMatch(line, @"StartCoroutine\s*\(\s*\w*(Loop|Repeat|Continuous|Forever|Tick|Poll|Spawn)", RegexOptions.IgnoreCase);
                }),

            new ReviewRule("BUG-13", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "new WaitForSeconds() allocated every yield -- cache as a field",
                "Declare a WaitForSeconds field and reuse it.",
                @"yield\s+return\s+new\s+WaitForSeconds\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            // BUG-14 removed: duplicate of UNITY-18 (SendMessage/BroadcastMessage)

            new ReviewRule("BUG-15", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "OnTriggerEnter/OnCollisionEnter -- ensure at least one object has Rigidbody",
                "At least one colliding object must have a Rigidbody.",
                @"void\s+(OnTriggerEnter|OnCollisionEnter|OnTriggerEnter2D|OnCollisionEnter2D)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("BUG-16", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Physics.Raycast without LayerMask -- scans all layers",
                "Add a LayerMask parameter to limit which layers are hit.",
                @"Physics\d*\.Raycast\s*\([^)]*\)\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"(LayerMask|layerMask|layer)" }),

            new ReviewRule("BUG-17", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Time.deltaTime in FixedUpdate -- use Time.fixedDeltaTime",
                "Replace with Time.fixedDeltaTime or omit (already fixed step).",
                @"Time\.deltaTime",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => InFixedUpdate(line, all, i, ctx)),

            new ReviewRule("BUG-18", Severity.LOW, Category.Bug, Language.CSharp,
                RuleScope.FileLevel, FileFilter.Runtime,
                "Empty Unity lifecycle method -- still called, wasting CPU",
                "Remove empty lifecycle methods entirely.",
                @"void\s+(Update|Start|Awake|LateUpdate|FixedUpdate|OnGUI|OnAnimatorMove)\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => ctx[i] != LineContext.Comment && BodyLength(all, i) <= 2),

            new ReviewRule("BUG-19", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "foreach in hot path -- may allocate enumerator on older Mono",
                "Use for loop with index instead.",
                @"foreach\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Span<", @"ReadOnlySpan<" },
                confidence: 50,
                reasoning: "Modern Unity (2021+) with .NET Standard 2.1 does not allocate enumerators for List<T> and arrays in foreach. Only a concern on older Mono backend or custom IEnumerable types."),

            new ReviewRule("BUG-20", Severity.LOW, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Debug.Log in production code -- wrap in #if UNITY_EDITOR or [Conditional]",
                "Use #if UNITY_EDITOR or [Conditional(\"UNITY_EDITOR\")] wrapper.",
                @"Debug\.(Log|LogWarning|LogError|LogException)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"#if\s+UNITY_EDITOR", @"\[Conditional" }),

            new ReviewRule("BUG-21", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Resources.Load at runtime without caching -- disk I/O each call",
                "Cache the loaded resource or use Addressables.",
                @"Resources\.Load\s*[<(]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"_\w+\s*=\s*Resources\.Load" }),

            new ReviewRule("BUG-22", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Instantiate without parent transform -- world-space recalculation",
                "Pass a parent transform as the second argument.",
                @"Instantiate\s*\(\s*[^,)]+\s*\)\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\.SetParent", @"\.transform\.parent" }),

            new ReviewRule("BUG-23", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "AddComponent in Update loop -- creates components every frame",
                "Move AddComponent to initialization or one-time event.",
                @"\.AddComponent\s*[<(]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("BUG-24", Severity.LOW, Category.Bug, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Private field missing [SerializeField] but preceded by [Tooltip]/[Header]",
                "Add [SerializeField] to private fields visible in Inspector.",
                @"private\s+\w+\s+\w+\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\[SerializeField\]", @"\[HideInInspector\]", @"static|const|readonly" },
                guard: (line, all, i, ctx) => {
                    return i > 0 && (all[i-1].Contains("[Tooltip") || all[i-1].Contains("[Header"));
                }),

            // BUG-25 FP fix: Skip ScriptableObject classes (public fields are intended pattern)
            new ReviewRule("BUG-25", Severity.LOW, Category.Bug, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Public field should be [SerializeField] private for encapsulation",
                "Use [SerializeField] private instead of public for Inspector fields.",
                @"^\s+public\s+(?!static|const|readonly|override|virtual|abstract|event|delegate|class|struct|enum|interface)\w+\s+\w+\s*[;=]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @":\s*ScriptableObject", @":\s*SOBase", @"\[System\.Serializable\]" },
                guard: (line, all, i, ctx) => {
                    for (int j = i; j >= 0; j--)
                    {
                        if (all[j].Contains(": MonoBehaviour"))
                            return ctx[i] != LineContext.Comment;
                        if (all[j].Contains(": ScriptableObject")) return false;
                        if (Regex.IsMatch(all[j], @"^\s*class\s+")) return false;
                    }
                    return false;
                }),

            new ReviewRule("BUG-26", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Comparing tag with == instead of CompareTag() -- allocates string",
                "Use gameObject.CompareTag(\"tag\") instead.",
                @"\.tag\s*==\s*""",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("BUG-27", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Vector3.Distance in loop -- use sqrMagnitude to avoid sqrt",
                "Use (a - b).sqrMagnitude < threshold * threshold.",
                @"Vector\d\.Distance\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"sqrMagnitude" }),

            new ReviewRule("BUG-28", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "LINQ in Update -- allocates iterators, closures, temp collections",
                "Replace LINQ with manual loops in hot paths.",
                @"\.\s*(Where|Select|OrderBy|GroupBy|Any|All|First|Last|Count|Sum|Min|Max|ToList|ToArray|ToDictionary)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("BUG-29", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Animator.StringToHash not cached -- recalculates hash every call",
                "Declare static readonly int fields for animator hashes.",
                @"Animator\.StringToHash\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"static\s+readonly\s+int" }),

            new ReviewRule("BUG-30", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "material property creating runtime instance -- use sharedMaterial or MPB",
                "Use renderer.sharedMaterial or MaterialPropertyBlock.",
                @"\.\s*material\s*[\.=](?!s)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"sharedMaterial", @"MaterialPropertyBlock" }),

            new ReviewRule("BUG-31", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Null-conditional ?. or ?? on UnityEngine.Object bypasses destroyed check",
                "Use explicit == null check: Unity overloads == to detect destroyed objects.",
                @"\b\w+\s*(\?\.|(\?\?))",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"System\.\w+", @"string\?", @"int\?" },
                guard: (line, all, i, ctx) => {
                    return ctx[i] != LineContext.Comment && Regex.IsMatch(line, @"(Component|GameObject|Transform|Renderer|Collider|Rigidbody|Camera|Light|MonoBehaviour)\s");
                }),

            new ReviewRule("BUG-32", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "GetComponentInChildren/InParent in Update -- cache it",
                "Cache in Awake/Start. Traverses entire hierarchy.",
                @"GetComponent(InChildren|InParent)\s*[<(]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"_\w+\s*=\s*GetComponent" }),

            new ReviewRule("BUG-33", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "FindWithTag/FindGameObjectsWithTag in Update -- O(n) scene scan",
                "Cache in Start() or use a registry pattern.",
                @"(FindWithTag|FindGameObjectsWithTag)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            // BUG-34: Dictionary serialization (Unity can't serialize Dictionary)
            new ReviewRule("BUG-34", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Dictionary<K,V> in [Serializable] class -- Unity cannot serialize dictionaries",
                "Use a List<SerializableKeyValue> wrapper or ISerializationCallbackReceiver.",
                @"\bDictionary\s*<",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"ISerializationCallbackReceiver", @"SerializationCallback", @"JsonConvert" },
                guard: (line, all, i, ctx) => {
                    // Only flag if inside a [Serializable] or [SerializeField] context
                    for (int j = Math.Max(0, i - 10); j < i; j++)
                        if (all[j].Contains("[Serializable]") || all[j].Contains("[SerializeField]"))
                            return true;
                    return false;
                }),

            // BUG-35: yield return 0 instead of null (causes boxing)
            new ReviewRule("BUG-35", Severity.LOW, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "yield return 0 -- boxes int to object; use yield return null",
                "Replace 'yield return 0' with 'yield return null' to avoid boxing allocation.",
                @"yield\s+return\s+0\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            // BUG-36: Input polling in FixedUpdate (misses input frames)
            new ReviewRule("BUG-36", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Input.GetKey/GetButton in FixedUpdate -- misses input on frames without physics step",
                "Read input in Update(), store in a field, apply in FixedUpdate().",
                @"Input\.(GetKey|GetKeyDown|GetKeyUp|GetButton|GetButtonDown|GetButtonUp|GetMouseButton|GetMouseButtonDown)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"void\s+Update" },
                guard: (line, all, i, ctx) => InFixedUpdate(line, all, i, ctx)),

            // BUG-37: ConfigureAwait(false) in Unity (breaks SynchronizationContext)
            new ReviewRule("BUG-37", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "ConfigureAwait(false) in Unity -- Unity has single-threaded sync context",
                "Remove .ConfigureAwait(false); Unity automatically returns to main thread.",
                @"\.ConfigureAwait\s*\(\s*false\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"#if\s+UNITY_EDITOR", @"/Editor/" }),

            // BUG-38: Texture2D created without Destroy (native memory leak)
            new ReviewRule("BUG-38", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "new Texture2D() without Destroy -- leaks native GPU memory",
                "Call Destroy(texture) when no longer needed, or use a texture pool.",
                @"new\s+Texture2D\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Destroy\s*\(", @"DestroyImmediate", @"Object\.Destroy",
                                     @"_texture\s*=\s*new\s+Texture2D" },
                antiRadius: 15),

            // BUG-39: RenderTexture not released (native memory leak)
            new ReviewRule("BUG-39", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "new RenderTexture() without Release -- leaks native GPU memory",
                "Call rt.Release() and Destroy(rt) in cleanup, or use RenderTexture.GetTemporary/ReleaseTemporary.",
                @"new\s+RenderTexture\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\.Release\s*\(\)", @"ReleaseTemporary", @"Destroy\s*\(" },
                antiRadius: 20),

            // BUG-40: DontDestroyOnLoad(this) instead of DontDestroyOnLoad(gameObject)
            new ReviewRule("BUG-40", Severity.LOW, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "DontDestroyOnLoad(this) -- should use DontDestroyOnLoad(gameObject)",
                "Pass gameObject instead of this to ensure the entire GameObject persists.",
                @"DontDestroyOnLoad\s*\(\s*this\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                confidence: 90),

            // ---- PERFORMANCE (1-22) ----

            new ReviewRule("PERF-01", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Boxing value type to object -- causes GC allocation",
                "Use generics or overloaded methods to avoid boxing.",
                @"\(\s*object\s*\)\s*\w+",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("PERF-02", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Closure allocation in lambda/delegate in hot path",
                "Capture in a struct or pass via static method + state parameter.",
                @"=>\s*\{?[^}]*\b(this|[a-z_]\w*)\b",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"static\s+(void|bool|int)",
                                     @"static\s*\(", @"static\s*\w+\s*=>" },
                confidence: 50,
                reasoning: "Most lambdas in Update don't allocate closures in modern Unity/.NET. Only a concern with captured locals that change per-frame."),

            new ReviewRule("PERF-03", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Large struct passed by value -- consider in/ref parameter",
                "Use 'in' for readonly pass or 'ref' for mutable pass.",
                @"\(\s*(Matrix4x4|Bounds|RaycastHit|ContactPoint|NavMeshHit)\s+\w+\s*[,)]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\bin\b", @"\bref\b" }),

            new ReviewRule("PERF-04", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Unbounded List.Add without Capacity pre-allocation",
                "Set list.Capacity or use new List<T>(expectedSize).",
                @"new\s+List\s*<[^>]+>\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Capacity\s*=" }),

            new ReviewRule("PERF-05", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "String.Format in hot path -- use cached StringBuilder",
                "Use StringBuilder.AppendFormat or pre-allocated string ops.",
                @"[Ss]tring\.Format\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("PERF-06", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Texture2D.GetPixel/SetPixel per-pixel -- use bulk API",
                "Use GetPixels32()/SetPixels32() for bulk pixel ops.",
                @"\.(GetPixel|SetPixel)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"GetPixels32|SetPixels32" }),

            new ReviewRule("PERF-07", Severity.HIGH, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Mesh property in loop -- each access copies entire array",
                "Cache mesh.vertices/normals/etc in a local array before the loop.",
                @"mesh\.(vertices|normals|uv|tangents|colors|triangles)\b",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"var\s+\w+\s*=\s*mesh\." }),

            new ReviewRule("PERF-08", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Physics cast without maxDistance -- scans to infinity",
                "Always specify a maxDistance parameter.",
                @"Physics\d*\.(Raycast|SphereCast|CapsuleCast|BoxCast)\s*\(\s*[^,]+\s*,\s*[^,]+\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("PERF-09", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Mathf.Pow(x, 2) -- use x * x for simple multiply",
                "Use x * x instead of Mathf.Pow(x, 2f).",
                @"Mathf\.Pow\s*\([^,]+,\s*2\.?0?f?\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("PERF-10", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Camera.main.ScreenToWorldPoint in Update without cache",
                "Cache Camera.main in Start().",
                @"Camera\.main\.Screen",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"_\w*(cam|camera)\s*=" }),

            new ReviewRule("PERF-11", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Nested for loops O(n^2) -- consider spatial hashing or early exit",
                "Use spatial partitioning, break/continue, or reduce inner loop.",
                @"for\s*\([^)]+\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"break\s*;" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] != LineContext.HotPath) return false;
                    // Check if there's another for loop within 8 lines after this one
                    for (int j = i + 1; j < Math.Min(i + 8, all.Length); j++)
                        if (Regex.IsMatch(all[j], @"for\s*\([^)]+\)")) return true;
                    return false;
                },
                confidence: 50,
                reasoning: "Nested for loops over small fixed-size collections (e.g. 4x4 matrix, 3D vector components) are fine. Only a concern when both loops iterate over dynamic-sized collections."),

            new ReviewRule("PERF-12", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "SetParent without worldPositionStays=false",
                "Pass false as second argument if world position preservation unneeded.",
                @"\.SetParent\s*\(\s*[^,)]+\s*\)\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                confidence: 45,
                reasoning: "worldPositionStays=true (the default) is often the intended behavior to preserve world position during reparenting. Passing false changes the object's world position, which may break gameplay. Only a perf concern in tight loops."),

            new ReviewRule("PERF-13", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "ParticleSystem collision on all layers -- use collision LayerMask",
                "Set the collision LayerMask to only needed layers.",
                @"collisionModule\.(enabled\s*=\s*true|collidesWith)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"LayerMask" }),

            new ReviewRule("PERF-14", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Light shadow casting on all objects -- use culling mask",
                "Set the light's culling mask to limit shadow-casting layers.",
                @"\.shadows\s*=\s*LightShadows\.(Soft|Hard)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"cullingMask" }),

            new ReviewRule("PERF-15", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "AudioSource spatialBlend 0 but using distance attenuation",
                "Set spatialBlend to 1 for 3D or remove rolloff settings.",
                @"spatialBlend\s*=\s*0",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                confidence: 45,
                reasoning: "spatialBlend=0 is correct for 2D audio (UI sounds, music). Only problematic if distance attenuation is also configured."),

            new ReviewRule("PERF-16", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "new NavMeshPath() in hot path -- allocates each call",
                "Reuse a NavMeshPath instance.",
                @"new\s+NavMeshPath\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"_\w+\s*=\s*new\s+NavMeshPath" }),

            new ReviewRule("PERF-17", Severity.HIGH, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "ForceUpdateCanvases() called -- very expensive",
                "Let Unity batch canvas updates naturally.",
                @"ForceUpdateCanvases\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("PERF-18", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "LayoutRebuilder.ForceRebuildLayoutImmediate every frame",
                "Only rebuild when content changes, then disable LayoutGroup.",
                @"LayoutRebuilder\.ForceRebuildLayoutImmediate\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("PERF-19", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "SetActive toggling in hot path -- consider CanvasGroup.alpha",
                "Use CanvasGroup.alpha or disable MeshRenderer for frequent toggles.",
                @"\.SetActive\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"CanvasGroup" }),

            new ReviewRule("PERF-20", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Multiple cameras rendering -- ensure proper culling/depth",
                "Reduce camera count or use stacking with optimized clear flags.",
                @"new\s+.*Camera\b.*enabled\s*=\s*true|Camera\.allCameras",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                confidence: 40,
                reasoning: "First branch 'new...Camera...enabled=true' is too loose and matches many unrelated patterns. Camera.allCameras is a valid query. Only a concern when multiple cameras render simultaneously without proper culling."),

            new ReviewRule("PERF-21", Severity.HIGH, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Use NonAlloc physics API to avoid array allocation every frame",
                "Replace RaycastAll with RaycastNonAlloc, OverlapSphere with OverlapSphereNonAlloc.",
                @"Physics\.(RaycastAll|SphereCastAll|CapsuleCastAll|BoxCastAll|OverlapSphere|OverlapBox|OverlapCapsule)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"NonAlloc" }),

            new ReviewRule("PERF-22", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Setting material properties in Update creates Material instances -- use MPB",
                "Use renderer.GetPropertyBlock()/SetPropertyBlock().",
                @"\.\s*material\s*\.\s*Set(Color|Float|Int|Vector|Texture|Matrix)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"MaterialPropertyBlock", @"sharedMaterial" }),

            // PERF-23: ToLower/ToUpper for string comparison
            new ReviewRule("PERF-23", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "ToLower()/ToUpper() for comparison -- allocates new string",
                "Use string.Equals(a, b, StringComparison.OrdinalIgnoreCase) instead.",
                @"\.(ToLower|ToUpper|ToLowerInvariant|ToUpperInvariant)\s*\(\s*\)\s*(==|!=|\.Equals|\.Contains|\.StartsWith)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"StringComparison\.(Ordinal|InvariantCulture)IgnoreCase" }),

            // (PERF-24 removed: duplicate of BUG-28)
            // (PERF-25 removed: duplicate of BUG-21)

            // ---- SECURITY (1-10) ----

            // SEC-01 FP fix: Skip Editor/ folder entirely
            new ReviewRule("SEC-01", Severity.CRITICAL, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "System.IO.File operation without path validation -- path traversal risk",
                "Validate and sanitize file paths; reject '..' and absolute paths from user input.",
                @"System\.IO\.(File|Directory)\.(Read|Write|Delete|Move|Copy|Create|Open|Append)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Application\.(dataPath|persistentDataPath|streamingAssetsPath)", @"/Editor/" }),

            new ReviewRule("SEC-02", Severity.CRITICAL, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Process.Start -- command injection risk",
                "Never pass user input to Process.Start; whitelist allowed commands.",
                @"Process\.Start\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"#if\s+UNITY_EDITOR" }),

            new ReviewRule("SEC-03", Severity.HIGH, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "JsonUtility.FromJson on untrusted input -- validate schema",
                "Validate deserialized object fields and reject unexpected values.",
                @"JsonUtility\.FromJson\s*[<(]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("SEC-04", Severity.HIGH, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "PlayerPrefs storing sensitive data -- plaintext storage",
                "Encrypt sensitive data or use a secure store.",
                @"PlayerPrefs\.(SetString|SetInt|SetFloat)\s*\(\s*""(password|token|key|secret|credential|auth)",
                RegexOptions.IgnoreCase,
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("SEC-05", Severity.MEDIUM, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "HTTP URL (non-HTTPS) -- data in plaintext",
                "Use HTTPS URLs for all network requests.",
                @"(""http://|UnityWebRequest\.Get\s*\(\s*""http://)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"localhost", @"127\.0\.0\.1" }),

            new ReviewRule("SEC-06", Severity.CRITICAL, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "CompileAssemblyFromSource -- arbitrary code execution",
                "Never compile user-provided code at runtime.",
                @"(CompileAssemblyFrom|CSharpCodeProvider|CodeDomProvider)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("SEC-07", Severity.CRITICAL, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "SQL query with string concat -- SQL injection risk",
                "Use parameterized queries.",
                @"(SELECT|INSERT|UPDATE|DELETE)\s+.*""\s*\+\s*\w+",
                RegexOptions.IgnoreCase,
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"@""", @"Parameters\.(Add|AddWithValue)" }),

            new ReviewRule("SEC-08", Severity.CRITICAL, Category.Security, Language.CSharp,
                RuleScope.FileLevel, FileFilter.All,
                "Hardcoded credential or API key in source",
                "Store in environment variables or secure vault.",
                @"(api[_-]?key|password|secret|token|credential)\s*=\s*""[^""]{8,}""",
                RegexOptions.IgnoreCase,
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"placeholder|example|test|dummy|TODO", @"\.env|config\." }),

            new ReviewRule("SEC-09", Severity.HIGH, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Resources.Load with user-provided path -- directory traversal",
                "Whitelist allowed resource paths.",
                @"Resources\.Load\s*\(\s*\w+\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"const\s+string", @"nameof\s*\(", @"Resources\.Load\s*\(\s*""" }),

            new ReviewRule("SEC-10", Severity.HIGH, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Application.OpenURL with dynamic URL -- URL injection",
                "Validate and whitelist URLs.",
                @"Application\.OpenURL\s*\(\s*[^"")\s]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            // ---- UNITY-SPECIFIC (1-17) ----

            new ReviewRule("UNITY-01", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "MonoBehaviour constructor -- use Awake()/Start() instead",
                "Unity manages MonoBehaviour lifecycle; use Awake/Start.",
                @"\bpublic\s+\w+\s*\(\s*\)\s*\{",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"ScriptableObject", @"struct\s" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    for (int j = i; j >= Math.Max(0, i - 50); j--)
                        if (Regex.IsMatch(all[j], @"class\s+\w+\s*:\s*\w*MonoBehaviour")) return true;
                    return false;
                }),

            new ReviewRule("UNITY-02", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "ScriptableObject constructor -- use OnEnable or CreateInstance",
                "Use ScriptableObject.CreateInstance<T>() and OnEnable.",
                @"\bpublic\s+\w+\s*\(\s*\)\s*\{",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"MonoBehaviour" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    for (int j = i; j >= Math.Max(0, i - 50); j--)
                        if (Regex.IsMatch(all[j], @"class\s+\w+\s*:\s*\w*ScriptableObject")) return true;
                    return false;
                }),

            new ReviewRule("UNITY-03", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Accessing .gameObject/.transform near Destroy(gameObject) -- potential use-after-destroy",
                "Add 'return;' after Destroy(gameObject) or null-check before accessing destroyed object's members.",
                @"\.(gameObject|transform)\b",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"if\s*\(\s*\w+\s*!=\s*null", @"return\s*;" },
                confidence: 50, priority: 55,
                reasoning: "Heuristic: flags .gameObject/.transform within 3 lines after Destroy(gameObject). Cannot verify the access targets the same object. Review manually.",
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    for (int j = Math.Max(0, i - 3); j < i; j++)
                        if (Regex.IsMatch(all[j], @"Destroy\s*\(\s*(gameObject|this\.gameObject)\s*\)")) return true;
                    return false;
                }),

            new ReviewRule("UNITY-04", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "DontDestroyOnLoad without singleton duplicate check",
                "Add: if (Instance != null) { Destroy(gameObject); return; }",
                @"DontDestroyOnLoad\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Instance\s*!=\s*null", @"Destroy\(gameObject\)" }),

            new ReviewRule("UNITY-05", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "GetComponent in Awake/Start without [RequireComponent]",
                "Add [RequireComponent(typeof(T))] to guarantee the component exists.",
                @"GetComponent\s*<(\w+)>\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\[RequireComponent", @"TryGetComponent" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    for (int j = i; j >= Math.Max(0, i - 20); j--)
                    {
                        if (Regex.IsMatch(all[j], @"void\s+(Awake|Start)\s*\(")) return true;
                        if (j < i && Regex.IsMatch(all[j], @"(void|private|public|protected)\s+\w+\s*\(")) return false;
                    }
                    return false;
                }),

            new ReviewRule("UNITY-06", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Invoke/InvokeRepeating with string method name",
                "Use Coroutines, async/await, or direct method references.",
                @"\.(Invoke|InvokeRepeating)\s*\(\s*""",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("UNITY-07", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Scene loaded without additive mode may leak DontDestroyOnLoad objects",
                "Use LoadSceneMode.Additive or clean up persistent objects.",
                @"SceneManager\.LoadScene\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Additive" },
                confidence: 40,
                reasoning: "Single-mode scene loading is the Unity default and correct in most cases. Only a concern with DontDestroyOnLoad objects."),

            new ReviewRule("UNITY-08", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Event += without matching -= -- memory leak if subscriber outlives publisher",
                "Add -= unsubscribe in OnDisable() or OnDestroy().",
                @"\w+\.\w+\s*\+=\s*\w+\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\+=\s*\d", @"\+=\s*""" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Must be member.Event += Handler pattern, not arithmetic
                    var m = Regex.Match(line, @"(\w+\.\w+)\s*\+=\s*(\w+)\s*;");
                    if (!m.Success) return false;
                    string handler = m.Groups[2].Value;
                    // Skip common arithmetic variable names
                    if (Regex.IsMatch(handler, @"^\d|^(count|score|total|sum|delta|offset|speed|health|mana|damage|bonus|amount|value|size|time|duration|timer|progress)$", RegexOptions.IgnoreCase))
                        return false;
                    for (int j = 0; j < all.Length; j++)
                        if (all[j].Contains("-= " + handler)) return false;
                    return true;
                }),

            new ReviewRule("UNITY-09", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Editor-only API outside #if UNITY_EDITOR block",
                "Wrap EditorApplication/AssetDatabase/etc. in #if UNITY_EDITOR.",
                @"(EditorApplication|AssetDatabase|EditorUtility|Selection|Undo|PrefabUtility|SerializedObject|SerializedProperty)\.",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"#if\s+UNITY_EDITOR" },
                guard: (line, all, i, ctx) => ctx[i] != LineContext.EditorBlock && ctx[i] != LineContext.Comment),

            new ReviewRule("UNITY-10", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Serializing interface or abstract type -- Unity serializer cannot handle",
                "Use concrete type or ISerializationCallbackReceiver.",
                @"\[SerializeField\]\s*(private|protected|public)?\s*(I[A-Z]\w+|abstract\s+\w+)\s+\w+",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"SerializeReference" }),

            new ReviewRule("UNITY-11", Severity.LOW, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Large array in ScriptableObject -- consider Addressables",
                "Use Addressables or split data into smaller chunks.",
                @"\[\]\s+\w+\s*=\s*new\s+\w+\[(?:[5-9]\d{2,}|\d{4,})\]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    for (int j = i; j >= Math.Max(0, i - 30); j--)
                        if (all[j].Contains(": ScriptableObject")) return true;
                    return false;
                }),

            new ReviewRule("UNITY-12", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Missing OnDisable/OnDestroy unsubscribe -- memory leak",
                "Always -= from events in OnDisable or OnDestroy.",
                @"\+=\s*\w+\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    bool inLifecycle = false;
                    for (int j = i; j >= Math.Max(0, i - 15); j--)
                    {
                        if (Regex.IsMatch(all[j], @"void\s+(OnEnable|Awake|Start)\s*\(")) { inLifecycle = true; break; }
                        if (j < i && Regex.IsMatch(all[j], @"(void|private|public)\s+\w+\s*\(")) break;
                    }
                    if (!inLifecycle) return false;
                    for (int j = 0; j < all.Length; j++)
                        if (Regex.IsMatch(all[j], @"void\s+(OnDisable|OnDestroy)\s*\(")) return false;
                    return true;
                }),

            new ReviewRule("UNITY-13", Severity.LOW, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Awake execution order dependency without [DefaultExecutionOrder]",
                "Add [DefaultExecutionOrder(N)] to control initialization order.",
                @"void\s+Awake\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\[DefaultExecutionOrder" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Check if Awake body references other objects (FindObjectOfType, Instance, GetComponent)
                    for (int j = i + 1; j < Math.Min(i + 20, all.Length); j++)
                    {
                        if (all[j].Contains("}") && j > i + 1) break;
                        if (Regex.IsMatch(all[j], @"(FindObjectOfType|\.Instance|GetComponent)")) return true;
                    }
                    return false;
                }),

            new ReviewRule("UNITY-14", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Static field in MonoBehaviour -- shared across instances",
                "Use instance fields or a dedicated static manager.",
                @"static\s+(?!readonly|void|bool|int|float|string|event|Action|Func|delegate)\w+\s+\w+",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Instance", @"Singleton", @"const\s" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    for (int j = i; j >= Math.Max(0, i - 50); j--)
                        if (Regex.IsMatch(all[j], @"class\s+\w+\s*:\s*\w*MonoBehaviour")) return true;
                    return false;
                }),

            new ReviewRule("UNITY-15", Severity.LOW, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Singleton MonoBehaviour missing [DisallowMultipleComponent]",
                "Add [DisallowMultipleComponent].",
                @"static\s+\w+\s+Instance\s*[{;=]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\[DisallowMultipleComponent" }),

            new ReviewRule("UNITY-16", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "GetComponent/Destroy in OnValidate -- fails during prefab import",
                "Wrap in #if UNITY_EDITOR and use EditorApplication.delayCall.",
                @"(GetComponent|Destroy|DestroyImmediate)\s*[<(]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"EditorApplication\.delayCall", @"#if\s+UNITY_EDITOR" },
                guard: (line, all, i, ctx) => {
                    for (int j = i; j >= 0; j--) {
                        if (Regex.IsMatch(all[j], @"void\s+OnValidate\s*\(")) return ctx[i] != LineContext.Comment;
                        if (j < i && Regex.IsMatch(all[j], @"(void|private|public)\s+\w+\s*\(")) return false;
                    }
                    return false;
                }),

            new ReviewRule("UNITY-17", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "OnGUI called every frame -- consider UI Toolkit",
                "For runtime UI prefer UI Toolkit or Canvas.",
                @"void\s+OnGUI\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"/Editor/" }),

            // UNITY-18: SendMessage -- slow reflection-based call
            new ReviewRule("UNITY-18", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "SendMessage/BroadcastMessage -- slow reflection, no compile-time safety",
                "Use direct method calls, C# events, or a message bus interface.",
                @"\b(SendMessage|BroadcastMessage|SendMessageUpwards)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"/Editor/" }),

            // UNITY-19: Shader.Find at runtime (fragile, fails in builds)
            new ReviewRule("UNITY-19", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Shader.Find() at runtime -- returns null if shader not in Always Included list",
                "Serialize shader references or load from Resources/Addressables.",
                @"Shader\.Find\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"/Editor/", @"#if\s+UNITY_EDITOR" }),

            // UNITY-20: Material leak from .material property access
            new ReviewRule("UNITY-20", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Accessing .material creates an instance -- must Destroy() it manually",
                "Use .sharedMaterial for read, or track/destroy instanced materials.",
                @"(?<!\bshared)\bmaterial\s*\.\s*(color|mainTexture|shader|renderQueue)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"sharedMaterial", @"MaterialPropertyBlock", @"Destroy\s*\(\s*\w*[Mm]at" },
                antiRadius: 10),

            // ---- CODE QUALITY (1-23) ----

            new ReviewRule("QUAL-01", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Method exceeds 50 lines -- consider extracting sub-methods",
                "Break long methods into smaller, well-named helpers.",
                @"(void|int|float|bool|string|var|Task|IEnumerator)\s+\w+\s*\([^)]*\)\s*\{",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => ctx[i] != LineContext.Comment && BodyLength(all, i) > 50),

            new ReviewRule("QUAL-02", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Excessive nesting depth (>4 levels) -- flatten with early returns",
                "Use guard clauses, early returns, or extract nested logic.",
                @"^\s{20,}(if|for|while|foreach|switch)\b",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("QUAL-03", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Magic number in code -- use a named constant",
                "Define a const or static readonly field.",
                @"[=<>+\-*/]\s*(?<![.0-9])((?:[2-9]\d{2,}|\d{4,})(?:\.\d+)?f?)\s*[;,)\]}]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"const\s", @"readonly\s", @"(Color|Vector|Rect|new\s+\w+\[)" }),

            // QUAL-04 FP fix: skip override methods, interface implementations
            new ReviewRule("QUAL-04", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.All,
                "Missing XML documentation on public method",
                "Add /// <summary> documentation to public API methods.",
                @"^\s+public\s+\S+\s+\w+\s*\([^)]*\)\s*\{?$",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"///", @"override\s+", @"^\s+public\s+\S+\s+\w+\s*\([^)]*\)\s*=>" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    if (line.Contains("override ") || line.Contains("abstract ")) return false;
                    // Skip interface implementations (check for explicit interface: IFoo.Method)
                    if (Regex.IsMatch(line, @"\bI[A-Z]\w+\.\w+\s*\(")) return false;
                    return i > 0 && !all[i-1].TrimStart().StartsWith("///");
                }),

            new ReviewRule("QUAL-05", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.All,
                "Inconsistent naming -- private fields should use _camelCase",
                "Follow Unity C# conventions: _camelCase for private.",
                @"private\s+\w+\s+([A-Z]\w+)\s*[;=]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"const\s", @"static\s", @"readonly\s", @"event\s" }),

            new ReviewRule("QUAL-06", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Empty catch block swallows exception silently",
                "At minimum log the exception.",
                @"catch\s*(\([^)]*\))?\s*\{\s*\}",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"// intentionally empty" }),

            new ReviewRule("QUAL-07", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.FileLevel, FileFilter.All,
                "TODO/FIXME/HACK comment -- track or resolve",
                "Create a task/issue and reference its ID.",
                @"//\s*(TODO|FIXME|HACK|XXX|TEMP|WORKAROUND)\b",
                RegexOptions.IgnoreCase,
                antiPatterns: new string[0]),

            new ReviewRule("QUAL-08", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.FileLevel, FileFilter.All,
                "Unused using directive",
                "Remove unused using statements.",
                @"^using\s+\w+(\.\w+)*\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    var m = Regex.Match(line, @"using\s+([\w.]+)\s*;");
                    if (!m.Success) return false;
                    string ns = m.Groups[1].Value;
                    string lastSeg = ns.Contains(".") ? ns.Substring(ns.LastIndexOf('.') + 1) : ns;
                    if (lastSeg == "System" || lastSeg == "Collections" || lastSeg == "Generic" ||
                        lastSeg == "UnityEngine" || lastSeg == "Linq") return false;
                    for (int j = 0; j < all.Length; j++)
                        if (j != i && all[j].Contains(lastSeg)) return false;
                    return true;
                }),

            new ReviewRule("QUAL-09", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Complex boolean condition (>3 operators) -- extract to named variable",
                "Extract complex conditions into a descriptive bool variable.",
                @"if\s*\(.*?(&&|\|\|).*?(&&|\|\|).*?(&&|\|\|)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("QUAL-10", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Switch statement missing default case",
                "Add a default case (even just throwing ArgumentOutOfRangeException).",
                @"switch\s*\([^)]+\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Scan forward to find if switch body has a default case
                    int depth = 0;
                    for (int j = i; j < Math.Min(i + 60, all.Length); j++)
                    {
                        depth += LineClassifier.CountChar(all[j], '{') - LineClassifier.CountChar(all[j], '}');
                        if (all[j].Contains("default:") || all[j].Contains("default :")) return false;
                        if (depth <= 0 && j > i) break;
                    }
                    return depth > 0 || i + 1 < all.Length;
                }),

            new ReviewRule("QUAL-11", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.All,
                "Non-sealed custom exception -- seal to prevent unintended inheritance",
                "Mark custom exception classes as sealed.",
                @"class\s+\w+Exception\s*:",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"sealed\s" }),

            new ReviewRule("QUAL-12", Severity.MEDIUM, Category.Quality, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.All,
                "Mutable static collection -- thread safety risk",
                "Use Concurrent* or make readonly with immutable contents.",
                @"static\s+(List|Dictionary|HashSet|Queue|Stack)\s*<",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"readonly\s", @"Concurrent" }),

            new ReviewRule("QUAL-13", Severity.HIGH, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "lock(this) or lock(typeof(...)) -- use dedicated lock object",
                "Use: private readonly object _lock = new object();",
                @"lock\s*\(\s*(this|typeof\s*\()",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("QUAL-14", Severity.MEDIUM, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "IDisposable not disposed -- use 'using' statement",
                "Wrap in 'using' block or call Dispose() in finally/OnDestroy.",
                @"new\s+(StreamReader|StreamWriter|FileStream|BinaryReader|BinaryWriter|HttpClient|WebClient|MemoryStream|UnityWebRequest)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"using\s+(var|\()", @"\.Dispose\s*\(" }),

            new ReviewRule("QUAL-15", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Null check on value type -- value types cannot be null",
                "Remove null check on value types.",
                @"(int|float|double|bool|byte|char|long|short|Vector[234]|Quaternion|Color|Rect|Bounds)\s+\w+.*==\s*null",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\?" }),

            new ReviewRule("QUAL-16", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Dead code after return/break/continue/throw",
                "Remove unreachable statements.",
                @"(return\s+[^;]+;|break\s*;|continue\s*;|throw\s+[^;]+;)\s*$",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"#(else|elif|endif)" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Check if next non-empty line has code (not } or #directive)
                    for (int j = i + 1; j < Math.Min(i + 3, all.Length); j++)
                    {
                        string next = all[j].Trim();
                        if (next == "" || next.StartsWith("//")) continue;
                        if (next == "}" || next.StartsWith("#")) return false;
                        return true;
                    }
                    return false;
                }),

            // QUAL-17 FP fix: threshold raised from 500 to 800
            new ReviewRule("QUAL-17", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.All,
                "God class (>800 lines) -- consider splitting",
                "Split large classes into focused, single-responsibility classes.",
                @"class\s+\w+",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"partial\s+class" },
                guard: (line, all, i, ctx) => ctx[i] != LineContext.Comment && BodyLength(all, i) > 800),

            new ReviewRule("QUAL-18", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Boxing in string interpolation -- call .ToString() explicitly",
                "Call .ToString() on value types in interpolation.",
                @"\$""[^""]*\{(?!.*\.ToString)[^}]*\}",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("QUAL-19", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.FileLevel, FileFilter.All,
                "#region used -- prefer smaller, focused classes",
                "Extract #region contents into separate classes.",
                @"#region\b",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("QUAL-20", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Catch block only rethrows -- remove redundant try/catch",
                "Remove or add logging/cleanup.",
                @"catch\s*\([^)]*\)\s*\{\s*throw\s*;\s*\}",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("QUAL-21", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "String.Equals without StringComparison",
                "Use StringComparison.Ordinal or OrdinalIgnoreCase.",
                @"\.Equals\s*\(\s*""",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"StringComparison" }),

            new ReviewRule("QUAL-22", Severity.MEDIUM, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Nested ternary operator -- hard to read",
                "Replace with if/else or switch expression.",
                @"\?[^;:]*\?[^;]*:",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("QUAL-23", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Parameter count >5 -- consider parameter object",
                "Group related parameters into a struct/class.",
                @"(void|int|float|bool|string|Task|IEnumerator|\w+)\s+\w+\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    var m = Regex.Match(line, @"\(([^)]*)\)");
                    if (!m.Success) return false;
                    string p = m.Groups[1].Value;
                    return !string.IsNullOrWhiteSpace(p) && p.Split(',').Length > 5;
                }),

            // ---- ADDITIONAL BUG DETECTION (41-55) ----

            new ReviewRule("BUG-41", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "yield return inside try block -- not supported in C# coroutines (pre-C# 8)",
                "Move yield return outside the try block, or use async/await with UniTask.",
                @"yield\s+return",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Check if we're inside a try block by looking for try { above
                    int depth = 0;
                    for (int j = i - 1; j >= Math.Max(0, i - 30); j--)
                    {
                        depth += LineClassifier.CountChar(all[j], '}') - LineClassifier.CountChar(all[j], '{');
                        if (depth < 0 && all[j].TrimStart().StartsWith("try")) return true;
                    }
                    return false;
                },
                confidence: 65, priority: 70,
                reasoning: "C# coroutines don't support yield in try-catch before C# 8. If using Unity 2021+ with C# 9, this may be valid."),

            new ReviewRule("BUG-42", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Static event/Action field -- persists across scene loads, leaks subscribers",
                "Clear static events in [RuntimeInitializeOnLoadMethod] or use instance events.",
                @"static\s+(event\s+\w+|Action|Action<|UnityAction|UnityEvent)\s+\w+",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"RuntimeInitializeOnLoadMethod", @"= null", @"= delegate" }),

            new ReviewRule("BUG-43", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Float comparison with == or != -- use Mathf.Approximately for floating-point",
                "Use Mathf.Approximately(a, b) or Mathf.Abs(a - b) < epsilon.",
                @"(==|!=)\s*\d+\.\d+f?",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Approximately", @"epsilon", @"Mathf\.Abs", @"== 0f", @"== 1f", @"!= 0f" },
                confidence: 55, priority: 40,
                reasoning: "Float equality comparisons are often intentional for sentinel values (0f, 1f, -1f). Only a real bug when comparing computed results."),

            new ReviewRule("BUG-44", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Assigning to transform.position.x/y/z -- does nothing (struct copy)",
                "Store position in local var, modify, then assign back: var p = transform.position; p.x = val; transform.position = p;",
                @"transform\.(position|localPosition|rotation|localRotation|eulerAngles|localEulerAngles)\.\w+\s*[+\-*/]?=",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                confidence: 95, priority: 90),

            new ReviewRule("BUG-45", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "AddForce without ForceMode -- defaults to ForceMode.Force (mass-dependent)",
                "Specify ForceMode explicitly: ForceMode.Impulse for instant, ForceMode.VelocityChange for mass-independent.",
                @"\.AddForce\s*\([^)]*\)\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"ForceMode" },
                confidence: 55, priority: 30,
                reasoning: "ForceMode.Force is the default and is often correct for continuous forces. Only a concern when code expects instant impulse behavior. Review the physics intent."),

            // BUG-46 removed: duplicate of QUAL-14 (IDisposable/UnityWebRequest disposal)

            new ReviewRule("BUG-47", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Coroutine with no yield return -- runs synchronously, not as coroutine",
                "Add at least one yield return statement, or convert to a regular method.",
                @"IEnumerator\s+\w+\s*\([^)]*\)\s*\{",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"yield\s+return", @"yield\s+break" },
                antiRadius: 30,
                confidence: 60, priority: 65,
                reasoning: "An IEnumerator method without yield statements compiles but runs entirely in one frame. This defeats the purpose of a coroutine. However, it may be an incomplete implementation or a method that delegates to another coroutine."),

            new ReviewRule("BUG-48", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Destroy() on a component removes only the component, not the GameObject",
                "Use Destroy(gameObject) to remove the entire GameObject, or Destroy(component) intentionally.",
                @"Destroy\s*\(\s*(?:this|GetComponent)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"gameObject" },
                confidence: 55, priority: 50,
                reasoning: "Destroy(this) removes only the MonoBehaviour component, leaving the GameObject alive. This is sometimes intentional (removing one script), but often a mistake when the developer meant to destroy the whole object."),

            new ReviewRule("BUG-49", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Infinite while(true) loop without yield/break/return in coroutine",
                "Add yield return null or yield return new WaitForSeconds() inside the loop.",
                @"while\s*\(\s*true\s*\)\s*\{",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"yield\s+return", @"yield\s+break", @"break\s*;", @"return\s*;" },
                antiRadius: 15),

            // (BUG-50 removed: confidence 40, pattern matches namespaces/transform.position.x -- too many false positives)

            // ---- ADDITIONAL PERFORMANCE (26-35) ----

            new ReviewRule("PERF-26", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "String.Contains without StringComparison -- culture-sensitive by default",
                "Use string.Contains(value, StringComparison.Ordinal) for culture-invariant comparison.",
                @"\.Contains\s*\(\s*""[^""]*""\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"StringComparison", @"Ordinal" },
                confidence: 50, priority: 20,
                reasoning: "String.Contains without StringComparison uses culture-sensitive comparison which is slower but correct for UI text. For internal string matching, Ordinal is faster and safer."),

            new ReviewRule("PERF-27", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Transform.Find in hot path -- string-based child lookup every frame",
                "Cache the child Transform reference in Start() or Awake().",
                @"\.Find\s*\(\s*""[^""]*""\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"_\w+\s*=\s*\w+\.Find" }),

            new ReviewRule("PERF-28", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Multiple GetComponent calls for same type -- cache once",
                "Call GetComponent<T>() once in Awake/Start and store the reference.",
                @"GetComponent\s*<(\w+)>\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    var m = Regex.Match(line, @"GetComponent\s*<(\w+)>");
                    if (!m.Success) return false;
                    string typeName = m.Groups[1].Value;
                    // Check if same GetComponent<Type> appears elsewhere in file
                    int count = 0;
                    for (int j = 0; j < all.Length; j++)
                        if (j != i && all[j].Contains("GetComponent<" + typeName + ">")) count++;
                    return count > 0;
                }),

            new ReviewRule("PERF-29", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Enum.HasFlag causes boxing allocation -- use bitwise check",
                "Use (flags & MyEnum.Value) != 0 instead of flags.HasFlag(MyEnum.Value).",
                @"\.HasFlag\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("PERF-30", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Instantiate in a loop without object pooling -- high GC pressure",
                "Use an ObjectPool<T> or custom pool to recycle instances.",
                @"Instantiate\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"ObjectPool", @"pool\." },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Check if inside a for/foreach/while loop
                    for (int j = Math.Max(0, i - 5); j < i; j++)
                        if (Regex.IsMatch(all[j], @"\b(for|foreach|while)\s*\(")) return true;
                    return false;
                }),

            new ReviewRule("PERF-31", Severity.LOW, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "new List<T>(list) copies entire list -- use AddRange or pass as IReadOnlyList",
                "If you only need to read, pass IReadOnlyList<T> or use .AsReadOnly().",
                @"new\s+List\s*<[^>]+>\s*\(\s*\w+\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                confidence: 45, priority: 15,
                reasoning: "Copying a list is sometimes necessary for thread safety or modification isolation. Only an issue if the copy is used read-only. Check if the original could be passed by reference instead."),

            // ---- ADDITIONAL UNITY (21-30) ----

            new ReviewRule("UNITY-21", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Rigidbody.MovePosition/MoveRotation outside FixedUpdate -- jerky movement",
                "Call Rigidbody.MovePosition only in FixedUpdate for smooth physics movement.",
                @"(MovePosition|MoveRotation)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"FixedUpdate" },
                confidence: 60, priority: 60,
                reasoning: "MovePosition/MoveRotation should be called in FixedUpdate for smooth physics. However, they are valid in Update for kinematic rigidbodies. Check the Rigidbody.isKinematic setting."),

            // UNITY-22 removed: raycastTarget=true is intentional code, ~100% FP rate

            new ReviewRule("UNITY-23", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "TMP_Text.text assigned in Update -- allocates string every frame",
                "Cache the string or use TMP_Text.SetText() with zero-alloc overloads.",
                @"\.text\s*=\s*[^;]+;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"SetText", @"(Start|Awake|OnEnable|Initialize|Init)\s*\(" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    return ctx[i] == LineContext.HotPath && (line.Contains("TMP_") || line.Contains("TextMeshPro") || line.Contains("tmpText"));
                }),

            new ReviewRule("UNITY-24", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "NavMeshAgent.SetDestination without IsOnNavMesh check -- may throw",
                "Check agent.isOnNavMesh before calling SetDestination.",
                @"\.SetDestination\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"isOnNavMesh", @"IsOnNavMesh" }),

            new ReviewRule("UNITY-25", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "ScriptableObject field modified at runtime -- shared across all references",
                "Clone the SO at runtime: Instantiate(mySO) or use a runtime data copy.",
                @"(?:_\w+SO|_\w+Data|_\w+Config)\s*\.\s*\w+\s*[+\-*/]?=",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Instantiate", @"ScriptableObject\.CreateInstance" },
                confidence: 45, priority: 55,
                reasoning: "Modifying ScriptableObject fields at runtime changes them for ALL references (including in the Editor, persisting across play sessions). This is often a critical bug, but the naming pattern match is heuristic -- verify the field is on a ScriptableObject."),

            new ReviewRule("UNITY-26", Severity.LOW, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Addressables.LoadAssetAsync without tracking handle for release",
                "Store the AsyncOperationHandle and call Addressables.Release(handle) when done.",
                @"Addressables\.(LoadAssetAsync|InstantiateAsync)\s*[<(]",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"=\s*Addressables\.", @"\.Release\s*\(" },
                confidence: 65, priority: 50,
                reasoning: "Addressables require explicit release to free memory. If the handle is not stored, it cannot be released later. However, if the asset is needed for the app lifetime, not releasing is acceptable."),

            new ReviewRule("UNITY-27", Severity.MEDIUM, Category.Unity, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "MonoBehaviour with both Update and FixedUpdate -- potential input/physics confusion",
                "Ensure input is read in Update and physics applied in FixedUpdate. Don't mix.",
                @"void\s+Update\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Only flag if same class also has FixedUpdate AND reads input in FixedUpdate
                    bool hasFixed = false;
                    for (int j = 0; j < all.Length; j++)
                        if (Regex.IsMatch(all[j], @"void\s+FixedUpdate\s*\(")) { hasFixed = true; break; }
                    if (!hasFixed) return false;
                    for (int j = 0; j < all.Length; j++)
                        if (Regex.IsMatch(all[j], @"void\s+FixedUpdate") && j + 20 < all.Length)
                            for (int k = j; k < Math.Min(j + 20, all.Length); k++)
                                if (Regex.IsMatch(all[k], @"Input\.(GetKey|GetButton|GetAxis|GetMouse)")) return true;
                    return false;
                },
                confidence: 60, priority: 45,
                reasoning: "This class has both Update and FixedUpdate with input reading in FixedUpdate. Input should be read in Update (runs every frame) and stored, then applied in FixedUpdate (runs at fixed intervals). Reading input in FixedUpdate misses frames."),

            // ---- ADDITIONAL QUALITY (24-35) ----

            new ReviewRule("QUAL-24", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Method has >10 conditional branches -- high cyclomatic complexity",
                "Extract branches into strategy pattern or separate methods.",
                @"(void|int|float|bool|string|var|Task|IEnumerator)\s+\w+\s*\([^)]*\)\s*\{",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    int end = i + BodyLength(all, i);
                    int branches = 0;
                    for (int j = i; j <= end && j < all.Length; j++)
                    {
                        if (Regex.IsMatch(all[j], @"\b(if|else if|case|catch|&&|\|\|)\b")) branches++;
                    }
                    return branches > 10;
                },
                type: FindingType.Strengthening),

            new ReviewRule("QUAL-25", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Method modifies class state AND returns a value -- side effect + return is confusing",
                "Separate into a query method (returns value) and command method (modifies state).",
                @"(public|internal)\s+(?!void|static|override|abstract)\w+\s+\w+\s*\([^)]*\)\s*\{",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Get\w+\s*\(", @"Is\w+\s*\(", @"Has\w+\s*\(", @"Can\w+\s*\(", @"Try\w+\s*\(" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    int end = Math.Min(i + 30, all.Length);
                    bool hasReturn = false, hasFieldWrite = false;
                    for (int j = i; j < end; j++)
                    {
                        if (all[j].Contains("return ") && !all[j].Contains("return;")) hasReturn = true;
                        if (Regex.IsMatch(all[j], @"\b(this\.)?_\w+\s*=")) hasFieldWrite = true;
                    }
                    return hasReturn && hasFieldWrite;
                },
                confidence: 40, priority: 15,
                type: FindingType.Strengthening,
                reasoning: "Command-Query Separation suggests methods should either change state OR return data, not both. However, TryXxx pattern and builder methods legitimately do both."),

            new ReviewRule("QUAL-26", Severity.MEDIUM, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Catch block logs error but continues execution -- may leave object in invalid state",
                "Consider if the method should return/throw after logging, or add state cleanup.",
                @"catch\s*\([^)]*\)\s*\{[^}]*Debug\.Log(Error|Exception)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"return\s*;", @"throw\s*;", @"break\s*;" },
                type: FindingType.Strengthening,
                confidence: 55, priority: 40,
                reasoning: "Logging an error and continuing is sometimes correct (graceful degradation), but can mask bugs by leaving the system in a partially-failed state. Verify the method handles the failure case properly after the catch."),

            new ReviewRule("QUAL-27", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.FileLevel, FileFilter.All,
                "File exceeds 500 lines -- consider splitting into partial classes or modules",
                "Split large files into focused, single-responsibility files.",
                @"^using\s+",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"partial\s+class" },
                guard: (line, all, i, ctx) => i == 0 && all.Length > 500,
                type: FindingType.Strengthening),

            new ReviewRule("QUAL-28", Severity.LOW, Category.Quality, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.All,
                "Multiple return statements with different types of null handling",
                "Standardize on returning null, empty collection, or using TryXxx pattern.",
                @"return\s+null\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Find enclosing method and check for mixed returns
                    int nullReturns = 0, valueReturns = 0;
                    for (int j = Math.Max(0, i - 30); j < Math.Min(i + 30, all.Length); j++)
                    {
                        if (Regex.IsMatch(all[j], @"return\s+null\s*;")) nullReturns++;
                        else if (Regex.IsMatch(all[j], @"return\s+\w+") && !all[j].Contains("return;")) valueReturns++;
                    }
                    return nullReturns >= 1 && valueReturns >= 2;
                },
                confidence: 45, priority: 20,
                type: FindingType.Strengthening,
                reasoning: "Methods that sometimes return null and sometimes return values make calling code fragile. Consider using Optional<T>, TryXxx pattern, or a Result type to make nullability explicit."),

            // ---- ADDITIONAL SECURITY (11-15) ----

            new ReviewRule("SEC-11", Severity.CRITICAL, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Reflection used to invoke methods -- bypasses access control",
                "Avoid reflection on user-controlled type/method names. Whitelist allowed types.",
                @"(MethodInfo|Type)\.\w*Invoke\s*\(|(Type\.GetType|Assembly\.GetType)\s*\(\s*\w+",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"#if\s+UNITY_EDITOR" }),

            new ReviewRule("SEC-12", Severity.HIGH, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "WWW class used (deprecated) -- use UnityWebRequest with certificate validation",
                "Replace WWW with UnityWebRequest and implement certificate validation.",
                @"\bWWW\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("SEC-13", Severity.MEDIUM, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Debug.Log may expose sensitive data in production builds",
                "Use conditional compilation or log level checks for sensitive data logging.",
                @"Debug\.Log\w*\s*\(\s*\$?"".*?(password|token|key|secret|credential|auth|session)",
                RegexOptions.IgnoreCase,
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"#if\s+(UNITY_EDITOR|DEBUG)" }),

            // ---- GAME-SPECIFIC: ANIMATION ----

            new ReviewRule("GAME-01", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Animator parameter set with string -- use cached StringToHash ID",
                "Declare: static readonly int hashParam = Animator.StringToHash(\"Param\"); then use the hash.",
                @"\.(SetTrigger|SetBool|SetFloat|SetInteger|GetBool|GetFloat|GetInteger|ResetTrigger)\s*\(\s*""[^""]+""",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"StringToHash" }),

            new ReviewRule("GAME-02", Severity.HIGH, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "AudioSource.PlayClipAtPoint creates hidden GameObject -- use audio pool",
                "Implement an AudioPool or use a pooled AudioSource.PlayOneShot() instead.",
                @"AudioSource\.PlayClipAtPoint\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("GAME-03", Severity.HIGH, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "RectTransform property animated in Update -- dirties entire Canvas, forces rebuild",
                "Split UI into static and dynamic Canvases. Use CanvasGroup.alpha for fading.",
                @"(rectTransform|RectTransform)\.(sizeDelta|anchoredPosition|localPosition|offsetMin|offsetMax)\s*=",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"CanvasGroup", @"DOTween", @"PrimeTween" }),

            new ReviewRule("GAME-04", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "ParticleSystem.main struct copy -- modifying returned copy has no effect",
                "Store in local variable first: var main = ps.main; main.startSpeed = val;",
                @"(\w+\.)?(particleSystem|GetComponent\s*<\s*ParticleSystem\s*>\s*\(\s*\))\.main\.\w+\s*=",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"var\s+\w+\s*=.*\.main" }),

            new ReviewRule("GAME-05", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "ParticleSystem.Play() called every frame without isPlaying check",
                "Guard with: if (!ps.isPlaying) ps.Play();",
                @"\.Play\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"isPlaying", @"if\s*\(" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] != LineContext.HotPath) return false;
                    return line.Contains("particle") || line.Contains("Particle") || line.Contains("ps.") || line.Contains("_ps.");
                }),

            new ReviewRule("GAME-06", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Async Task in MonoBehaviour without CancellationToken -- orphaned task after Destroy",
                "Pass destroyCancellationToken or this.GetCancellationTokenOnDestroy().",
                @"async\s+(Task|UniTask)\s+\w+\s*\([^)]*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"CancellationToken", @"destroyCancellationToken", @"GetCancellationTokenOnDestroy" }),

            new ReviewRule("GAME-07", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Rigidbody.velocity direct assignment -- use AddForce with ForceMode.VelocityChange",
                "Use rb.AddForce(velocity, ForceMode.VelocityChange) for physics-correct velocity changes.",
                @"\.\s*velocity\s*=\s*(?!Vector3\.zero)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"isKinematic", @"kinematic" },
                confidence: 55, priority: 40,
                reasoning: "Direct velocity assignment is sometimes valid for kinematic rigidbodies or teleportation. For physics-driven movement, AddForce is preferred for proper collision detection."),

            new ReviewRule("GAME-08", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                ".Count() LINQ method on List/Array -- use .Count/.Length property instead",
                "Use collection.Count (List) or array.Length directly -- O(1) vs O(n).",
                @"\.(Count|Any|All)\s*\(\s*\)",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\.Length\b", @"\.Count\b(?!\s*\()" },
                guard: (line, all, i, ctx) => ctx[i] == LineContext.HotPath),

            new ReviewRule("GAME-09", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "SendWebRequest() without yield/await -- fire-and-forget web request, result never observed",
                "Use: yield return request.SendWebRequest(); or await request.SendWebRequest();",
                @"\.SendWebRequest\s*\(\s*\)\s*;",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"yield\s+return", @"await\s" }),

            new ReviewRule("GAME-10", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "SetTrigger in Update without state check -- causes trigger queue buildup",
                "Check animator state first: if (!animator.GetCurrentAnimatorStateInfo(0).IsName(\"Attack\"))",
                @"\.SetTrigger\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"GetCurrentAnimatorStateInfo", @"IsInTransition" },
                guard: (line, all, i, ctx) => ctx[i] == LineContext.HotPath),

            // ---- CRITICAL MISSING RULES (from Opus review) ----

            new ReviewRule("SAVE-01", Severity.CRITICAL, Category.Security, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "BinaryFormatter is a critical security vulnerability -- arbitrary code execution via deserialization",
                "Use JSON (JsonUtility, Newtonsoft), MessagePack, or a custom binary serializer.",
                @"BinaryFormatter",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("TWEEN-01", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "DOTween/PrimeTween not killed in OnDestroy -- tween continues on destroyed object",
                "Kill tweens in OnDestroy: transform.DOKill() or Tween.Kill().",
                @"\.(DOFade|DOScale|DOMove|DORotate|DOColor|DOLocalMove|DOAnchorPos|DOSizeDelta|DOPunchScale|DOShakePosition)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\.DOKill\s*\(", @"\.Kill\s*\(", @"DOTween\.Kill", @"OnDestroy" },
                antiRadius: 30),

            new ReviewRule("THREAD-01", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Task.Run creates thread pool thread -- cannot access Unity API from background thread",
                "Use UniTask.RunOnThreadPool with SwitchToMainThread, or use coroutines.",
                @"Task\.Run\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"#if\s+UNITY_EDITOR", @"/Editor/" }),

            new ReviewRule("ITER-01", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Modifying collection during iteration -- InvalidOperationException",
                "Collect items to remove in a separate list, then remove after the loop.",
                @"\.Remove\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\.ToList\s*\(\s*\)", @"for\s*\(\s*int" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    for (int j = Math.Max(0, i - 10); j < i; j++)
                        if (Regex.IsMatch(all[j], @"foreach\s*\(")) return true;
                    return false;
                }),

            // ---- GAP RULES (from Opus analysis) ----

            new ReviewRule("BUG-51", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "StartCoroutine with string method name -- no compile-time safety, uses reflection",
                "Use StartCoroutine(MethodName()) with IEnumerator return value.",
                @"StartCoroutine\s*\(\s*""[^""]*""",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" }),

            new ReviewRule("BUG-52", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "new WaitUntil/WaitWhile allocated every yield -- cache as field",
                "Declare a WaitUntil/WaitWhile field and reuse it.",
                @"yield\s+return\s+new\s+Wait(Until|While)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"_wait\w+\s*=\s*new\s+Wait(Until|While)" }),

            new ReviewRule("BUG-53", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Sprite.Create() without Destroy -- leaks native memory",
                "Call Destroy(sprite) when no longer needed, or pool created sprites.",
                @"Sprite\.Create\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Destroy\s*\(", @"DestroyImmediate", @"Object\.Destroy",
                                     @"_sprite\s*=\s*Sprite\.Create" },
                antiRadius: 15),

            new ReviewRule("BUG-54", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Animator.enabled = false resets state machine -- use speed = 0 to pause",
                "Set animator.speed = 0f to pause. Only disable Animator in OnDisable/OnDestroy.",
                @"animator\w*\.enabled\s*=\s*false",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\.speed\s*=\s*0", @"OnDisable", @"OnDestroy" }),

            new ReviewRule("BUILD-01", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.FileLevel, FileFilter.Runtime,
                "using UnityEditor outside #if UNITY_EDITOR -- causes build failure",
                "Wrap in #if UNITY_EDITOR / #endif or move to Editor/ folder.",
                @"^using\s+UnityEditor",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"#if\s+UNITY_EDITOR", @"/Editor/" }),

            // ---- UNITY DOCS GAP RULES ----

            new ReviewRule("UNITY-28", Severity.HIGH, Category.Unity, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "NativeArray/NativeContainer not Disposed -- leaks unmanaged memory, Unity logs error",
                "Call .Dispose() in OnDestroy, use Allocator.Temp (auto-disposes at frame end), or wrap in using statement.",
                @"new\s+Native(Array|List|HashSet|HashMap|Queue|Stack|MultiHashMap)\s*<",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\.Dispose\s*\(", @"using\s+var", @"using\s*\(", @"Allocator\.Temp\b" },
                antiRadius: 20),

            new ReviewRule("UNITY-31", Severity.CRITICAL, Category.Bug, Language.CSharp,
                RuleScope.ClassLevel, FileFilter.Runtime,
                "Unity Object created in field initializer -- runs on loading thread, crashes in release builds",
                "Move to Awake() or Start(). Field initializers run on the loading thread where Unity APIs are unavailable.",
                @"=\s*new\s+(GameObject|Texture2D|Material|Mesh|RenderTexture|Sprite|ComputeBuffer)\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Only flag field-level initializers (not inside methods)
                    for (int j = i; j >= Math.Max(0, i - 20); j--)
                    {
                        if (Regex.IsMatch(all[j], @"(void|Task|IEnumerator|async)\s+\w+\s*\(")) return false;
                        if (Regex.IsMatch(all[j], @"^\s*(class|struct)\s+")) return true;
                    }
                    return false;
                }),

            new ReviewRule("PERF-32", Severity.HIGH, Category.Performance, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Manual GC.Collect() causes frame hitch -- let Unity manage garbage collection timing",
                "Remove GC.Collect(). Only acceptable during loading screens with GC.WaitForPendingFinalizers().",
                @"GC\.Collect\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"loading", @"Loading", @"SceneManager", @"#if\s+UNITY_EDITOR" }),

            new ReviewRule("PERF-36", Severity.MEDIUM, Category.Performance, Language.CSharp,
                RuleScope.HotPath, FileFilter.Runtime,
                "Shader.PropertyToID not cached -- recalculates string hash every call",
                "Cache: static readonly int _PropID = Shader.PropertyToID(\"_PropName\"); then use _PropID.",
                @"Shader\.PropertyToID\s*\(",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"static\s+readonly\s+int", @"static\s+int" }),

            new ReviewRule("BUG-55", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "await in OnDestroy/OnDisable -- continuation runs after object destruction, NullReferenceException",
                "Use destroyCancellationToken (Unity 2023+) or avoid async in teardown. Move cleanup to synchronous code.",
                @"await\s+",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"destroyCancellationToken", @"CancellationToken" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    for (int j = i; j >= Math.Max(0, i - 15); j--)
                    {
                        if (Regex.IsMatch(all[j], @"void\s+(OnDestroy|OnDisable)\s*\(")) return true;
                        if (j < i && Regex.IsMatch(all[j], @"(void|Task|async)\s+\w+\s*\(")) return false;
                    }
                    return false;
                }),

            new ReviewRule("BUG-56", Severity.MEDIUM, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "Accumulating eulerAngles causes gimbal lock and 0/360 wrapping artifacts",
                "Use transform.Rotate(delta) or accumulate in a Vector3 field, then apply: transform.rotation = Quaternion.Euler(accum);",
                @"\.eulerAngles\s*\+=",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"Quaternion\.Euler", @"Rotate\s*\(" },
                confidence: 65, priority: 60,
                reasoning: "eulerAngles += is almost always wrong due to gimbal lock. However, some 2D games safely use z-axis-only rotation this way."),

            new ReviewRule("BUG-57", Severity.HIGH, Category.Bug, Language.CSharp,
                RuleScope.AnyMethod, FileFilter.Runtime,
                "UnityWebRequest downloadHandler accessed without checking .result -- silent failure on network errors",
                "Check: if (request.result == UnityWebRequest.Result.Success) before accessing .downloadHandler.text/data.",
                @"\.downloadHandler\.(text|data|bytes)\b",
                antiPatterns: new[]{ @"//\s*VB-IGNORE", @"\.result\s*[!=]=", @"\.isNetworkError", @"\.isHttpError", @"ConnectionError", @"ProtocolError", @"Success" },
                antiRadius: 10)
        };

        // =====================================================================
        //  PYTHON RULES (33 rules, scanned against .py files)
        // =====================================================================

        public static readonly ReviewRule[] PythonRules = new ReviewRule[]
        {
            // ---- SECURITY ----
            new ReviewRule("PY-SEC-01", Severity.CRITICAL, Category.Security, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "eval() usage -- arbitrary code execution risk",
                "Replace with ast.literal_eval() or redesign.",
                @"\beval\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"literal_eval" }),

            new ReviewRule("PY-SEC-02", Severity.CRITICAL, Category.Security, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "os.system() or subprocess with shell=True -- command injection",
                "Use subprocess.run() with list args and shell=False.",
                @"(os\.system\s*\(|subprocess\.\w+\([^)]*shell\s*=\s*True)",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            new ReviewRule("PY-SEC-03", Severity.CRITICAL, Category.Security, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "pickle.load on untrusted data -- arbitrary code execution",
                "Use json, msgpack, or safer format.",
                @"pickle\.(load|loads)\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            new ReviewRule("PY-SEC-04", Severity.HIGH, Category.Security, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "f-string in SQL/shell command -- injection risk",
                "Use parameterized queries or subprocess with list args.",
                @"(execute|run|system|popen)\s*\(\s*f[""']",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            // PY-SEC-05 FP fix: skip constant assignments and default parameters
            new ReviewRule("PY-SEC-05", Severity.HIGH, Category.Security, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "exec() usage -- arbitrary code execution",
                "Avoid exec(); refactor to safe alternatives.",
                @"\bexec\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"^\s*\w+\s*=\s*", @"def\s+\w+\s*\([^)]*exec" }),

            new ReviewRule("PY-SEC-06", Severity.MEDIUM, Category.Security, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Hardcoded file path -- not portable",
                "Use pathlib.Path or os.path.join with configurable base.",
                @"['""](?:/[a-z]+/|[A-Z]:\\\\)[^'""]{3,}['""]",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            new ReviewRule("PY-SEC-07", Severity.HIGH, Category.Security, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "assert for input validation -- stripped with -O",
                "Use if/raise ValueError for validation.",
                @"^\s*assert\s+(?!.*#\s*nosec)",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"#\s*nosec", @"test_|_test\.py" }),

            // ---- CORRECTNESS ----
            new ReviewRule("PY-COR-01", Severity.HIGH, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Mutable default argument -- shared across calls",
                "Use None as default, create mutable inside function body.",
                @"def\s+\w+\s*\([^)]*=\s*(\[\]|\{\}|set\(\))",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            new ReviewRule("PY-COR-02", Severity.HIGH, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Bare except: catches SystemExit, KeyboardInterrupt",
                "Catch specific exceptions: except ValueError, except Exception as e.",
                @"^\s*except\s*:",
                antiPatterns: new[]{ @"#\s*VB-IGNORE" }),

            new ReviewRule("PY-COR-03", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Comparing with None using == instead of 'is None'",
                "Use 'is None' or 'is not None'.",
                @"[!=]=\s*None\b",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            new ReviewRule("PY-COR-04", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "open() without context manager -- file may not close",
                "Use 'with open(...) as f:'.",
                @"(?<!\bwith\s)\bopen\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"\bwith\b",
                    @"Image\.open", @"BytesIO", @"PIL" }),

            new ReviewRule("PY-COR-05", Severity.LOW, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "datetime.now() without timezone -- ambiguous",
                "Use datetime.now(tz=timezone.utc).",
                @"datetime\.now\s*\(\s*\)",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            // PY-COR-06 FP fix: only flag if result is actually mutated, not just read
            new ReviewRule("PY-COR-06", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "dict.get() with mutable default -- mutated result is shared",
                "Use dict.get(key) with None check, then create mutable separately.",
                @"\.get\s*\([^)]*,\s*(\[\]|\{\}|set\(\))",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" },
                guard: (line, all, i, ctx) => {
                    // If the .get() result is consumed read-only on the same line, skip
                    if (Regex.IsMatch(line, @"\blen\s*\(.*\.get\s*\(") ||
                        Regex.IsMatch(line, @"\bfor\s+.*\.get\s*\(") ||
                        Regex.IsMatch(line, @"\bif\s+.*\.get\s*\(") ||
                        Regex.IsMatch(line, @"\breturn\s+.*\.get\s*\(") ||
                        Regex.IsMatch(line, @"\bprint\s*\(.*\.get\s*\(") ||
                        Regex.IsMatch(line, @"\bnot\s+.*\.get\s*\(") ||
                        Regex.IsMatch(line, @"\bor\s+.*\.get\s*\(") ||
                        Regex.IsMatch(line, @"\band\s+.*\.get\s*\("))
                        return false;
                    // Check next lines for read-only usage (.items, .keys, .values, for..in, len)
                    for (int j = i + 1; j < Math.Min(all.Length, i + 8); j++)
                    {
                        if (Regex.IsMatch(all[j], @"\.(items|keys|values)\s*\(|for\s+\w+\s+(in|,)"))
                            return false;
                    }
                    // Only flag if result is mutated nearby (.append, .extend, []=, .add)
                    for (int j = i; j < Math.Min(all.Length, i + 3); j++)
                    {
                        if (Regex.IsMatch(all[j], @"\.(append|extend|add|update|insert)\s*\(|(\[.+\]\s*=)"))
                            return true;
                    }
                    return false;
                }),

            new ReviewRule("PY-COR-07", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.ClassLevel, FileFilter.All,
                "Class with __del__ -- unpredictable GC, prevents ref cycle collection",
                "Use context managers or weakref.finalize instead.",
                @"def\s+__del__\s*\(\s*self",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            new ReviewRule("PY-COR-08", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Thread without daemon=True -- may prevent clean shutdown",
                "Set daemon=True or join before exit.",
                @"Thread\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"daemon" }),

            new ReviewRule("PY-COR-09", Severity.LOW, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "json.loads without error handling",
                "Wrap in try/except json.JSONDecodeError.",
                @"json\.loads?\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"except.*JSON",
                    @"\btry\s*:", @"\bexcept\b", @"JSONDecodeError" },
                antiRadius: 30),

            new ReviewRule("PY-COR-10", Severity.LOW, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Float equality comparison -- use math.isclose",
                "Use math.isclose(a, b) or abs(a - b) < epsilon.",
                @"(?<!\w)(==|!=)\s*\d+\.\d+",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            new ReviewRule("PY-COR-11", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Re-raising exception without chain -- loses traceback",
                "Use 'raise X(...) from e'.",
                @"raise\s+\w+\([^)]*\)\s*$",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"\bfrom\s+\w+" },
                guard: (line, all, i, ctx) => {
                    for (int j = Math.Max(0, i - 5); j < i; j++)
                        if (all[j].Contains("except")) return true;
                    return false;
                }),

            new ReviewRule("PY-COR-12", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Exception type too broad -- catches bugs with expected errors",
                "Catch specific exceptions.",
                @"except\s+Exception\s*(?:as|\s*:)",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"# broad catch intentional",
                    @"logger\.exception", @"mcp\.tool", @"return\s+json\.dumps" },
                antiRadius: 10,
                guard: (line, all, i, ctx) => {
                    // In MCP handlers, broad catch is correct -- check if except block
                    // contains logger.exception or returns JSON error response
                    for (int j = i + 1; j < Math.Min(all.Length, i + 10); j++)
                    {
                        if (Regex.IsMatch(all[j], @"logger\.(exception|error)\s*\(")) return false;
                        if (Regex.IsMatch(all[j], @"return\s+(json\.dumps|""?\{)")) return false;
                        if (Regex.IsMatch(all[j], @"^\s*(except|def |class )\b")) break;
                    }
                    return true;
                }),

            // PY-COR-13 FP fix: only flag magic numbers in control flow, not data dicts/assignments
            new ReviewRule("PY-COR-13", Severity.LOW, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Import inside function body -- may indicate circular import workaround",
                "Restructure modules to avoid circular dependencies.",
                @"SENTINEL_PY_AST_ONLY",
                antiPatterns: new string[0]),

            // PY-COR-14: Variable shadowing built-in names
            new ReviewRule("PY-COR-14", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Variable shadows built-in name (list, dict, set, type, id, etc.)",
                "Choose a different variable name: items, mapping, group, etc.",
                @"^\s*(list|dict|set|str|int|float|bool|tuple|type|id|input|filter|map|zip|range|len|sum|min|max|any|all|sorted|reversed|hash|next|iter|open|print|format|bytes|object|super|property|staticmethod|classmethod)\s*=\s*",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"typing", @"import" }),

            // PY-COR-15: Late binding closure in loop
            new ReviewRule("PY-COR-15", Severity.HIGH, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Lambda in loop captures loop variable by reference -- late binding bug",
                "Capture with default arg: lambda x, i=i: ... or use functools.partial.",
                @"for\s+(\w+)\s+in\b",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    var m = System.Text.RegularExpressions.Regex.Match(line, @"for\s+(\w+)\s+in\b");
                    if (!m.Success) return false;
                    string loopVar = m.Groups[1].Value;
                    for (int j = i + 1; j < Math.Min(all.Length, i + 8); j++)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(all[j],
                            @"lambda\b(?![^:]*\b" + loopVar + @"\s*=).*\b" + loopVar + @"\b"))
                            return true;
                    }
                    return false;
                }),

            // ---- PERFORMANCE ----
            new ReviewRule("PY-PERF-01", Severity.LOW, Category.Performance, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "String concatenation in loop -- O(n^2)",
                "Collect parts in list, ''.join(parts) after loop.",
                @"\w+\s*\+=\s*['""]",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" },
                guard: (line, all, i, ctx) => {
                    if (ctx[i] == LineContext.Comment) return false;
                    // Only flag if inside a for/while loop (check preceding lines)
                    for (int j = Math.Max(0, i - 5); j < i; j++)
                        if (Regex.IsMatch(all[j], @"^\s*(for|while)\b")) return true;
                    return false;
                }),

            // PY-PERF-02 FP fix: skip if regex used only once (not in a loop)
            new ReviewRule("PY-PERF-02", Severity.LOW, Category.Performance, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "re.match/search/findall without compile for repeated pattern",
                "Compile pattern once with re.compile() and reuse.",
                @"re\.(match|search|findall|sub|split)\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"re\.compile" },
                guard: (line, all, i, ctx) => {
                    // Only flag if inside a loop (for/while in preceding 5 lines)
                    for (int j = Math.Max(0, i - 5); j < i; j++)
                        if (Regex.IsMatch(all[j], @"^\s*(for|while)\b")) return true;
                    return false;
                }),

            new ReviewRule("PY-PERF-03", Severity.LOW, Category.Performance, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Large file .read() without chunking -- may exhaust memory",
                "Use chunked reading: for line in file, or file.read(chunk_size).",
                @"\.read\s*\(\s*\)",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"""rb""", @"BytesIO", @"img_bytes", @"image_data",
                    @"base64", @"encoding=""utf-8""", @"\.read_text\s*\(" }),

            // ---- STYLE ----
            new ReviewRule("PY-STY-01", Severity.LOW, Category.Quality, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "os.path usage instead of pathlib.Path",
                "Use pathlib.Path (Python 3.4+).",
                @"os\.path\.(join|exists|isfile|isdir|basename|dirname|splitext)\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" }),

            new ReviewRule("PY-STY-02", Severity.LOW, Category.Quality, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Nested function definitions over 3 levels",
                "Extract inner functions to module level or class methods.",
                @"^\s{12,}def\s+\w+\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE" }),

            new ReviewRule("PY-STY-03", Severity.LOW, Category.Quality, Language.Python,
                RuleScope.FileLevel, FileFilter.All,
                "Star import (from X import *) -- namespace pollution",
                "Import specific names: from X import a, b, c.",
                @"from\s+\S+\s+import\s+\*",
                antiPatterns: new[]{ @"#\s*VB-IGNORE" }),

            new ReviewRule("PY-STY-04", Severity.LOW, Category.Quality, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Global variable mutation",
                "Pass as parameters or use a class.",
                @"^\s+global\s+\w+",
                antiPatterns: new[]{ @"#\s*VB-IGNORE" }),

            // PY-STY-05 through PY-STY-08 are AST-only, handled in DeepPythonAnalyzer
            new ReviewRule("PY-STY-05", Severity.LOW, Category.Quality, Language.Python,
                RuleScope.FileLevel, FileFilter.All,
                "Missing __main__ guard -- code runs on import",
                "Wrap in: if __name__ == '__main__':",
                @"SENTINEL_PY_AST_ONLY",
                antiPatterns: new string[0]),

            new ReviewRule("PY-STY-06", Severity.LOW, Category.Quality, Language.Python,
                RuleScope.FileLevel, FileFilter.All,
                "Missing __all__ in public module",
                "Add __all__ = [...] to define the public API.",
                @"SENTINEL_PY_AST_ONLY",
                antiPatterns: new string[0]),

            // PY-STY-09: function length threshold 100 for *_templates.py, 60 otherwise
            new ReviewRule("PY-STY-09", Severity.LOW, Category.Quality, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Function exceeds length threshold",
                "Break long functions into smaller, well-named helpers.",
                @"SENTINEL_PY_AST_ONLY",
                antiPatterns: new string[0]),

            new ReviewRule("PY-STY-08", Severity.LOW, Category.Quality, Language.Python,
                RuleScope.FileLevel, FileFilter.All,
                "Missing type annotation on public function",
                "Add return type annotation: def func(...) -> ReturnType:",
                @"SENTINEL_PY_AST_ONLY",
                antiPatterns: new string[0]),

            // ---- ADDITIONAL PYTHON (COR-16 through COR-20) ----

            new ReviewRule("PY-COR-16", Severity.HIGH, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "asyncio.run() inside already-running event loop -- raises RuntimeError",
                "Use 'await' inside async functions or nest_asyncio.apply().",
                @"asyncio\.run\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"nest_asyncio" }),

            new ReviewRule("PY-COR-17", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "os.environ mutation at module scope -- side effect on import",
                "Set environment variables in main() or a setup function, not at import time.",
                @"os\.environ\[",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"def\s+\w+", @"if\s+__name__" },
                guard: (line, all, i, ctx) => {
                    // Only flag if at module level (not indented beyond class/function)
                    return !line.TrimStart().StartsWith("def ") && line == line.TrimStart();
                }),

            new ReviewRule("PY-COR-18", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Exception caught and logged but not re-raised -- silently swallowed",
                "Re-raise with 'raise' or 'raise X from e' after logging.",
                @"except\s+\w+",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"\braise\b", @"return\s" },
                guard: (line, all, i, ctx) => {
                    // Check if the except block has logging but no raise
                    for (int j = i + 1; j < Math.Min(all.Length, i + 5); j++)
                    {
                        string t = all[j].TrimStart();
                        if (t.StartsWith("except") || t.StartsWith("finally") || (t.Length > 0 && !t.StartsWith(" ") && !t.StartsWith("\t") && j > i)) break;
                        if (Regex.IsMatch(t, @"(log|print|logger)\w*\s*\(")) return true;
                    }
                    return false;
                },
                confidence: 55, priority: 40,
                reasoning: "Catching an exception, logging it, then continuing is sometimes intentional (graceful degradation). But it can hide real bugs. Verify the code handles the failure path correctly."),

            new ReviewRule("PY-COR-19", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "sys.exit() in library code -- should only be in __main__ scripts",
                "Raise SystemExit or a custom exception instead of calling sys.exit() directly.",
                @"sys\.exit\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"__main__" }),

            new ReviewRule("PY-COR-20", Severity.LOW, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "Mixed pathlib and os.path -- use one consistently",
                "Standardize on pathlib.Path for all path operations.",
                @"os\.path\.\w+",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#" },
                guard: (line, all, i, ctx) => {
                    // Only flag if file also uses pathlib
                    for (int j = 0; j < all.Length; j++)
                        if (all[j].Contains("pathlib") || all[j].Contains("Path(")) return true;
                    return false;
                },
                confidence: 50, priority: 15,
                reasoning: "Mixing pathlib and os.path is not a bug but reduces code consistency. If the project already uses pathlib, prefer it throughout."),

            // ---- ADDITIONAL PYTHON GAME-DEV ----

            new ReviewRule("PY-SEC-08", Severity.HIGH, Category.Security, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "subprocess.run() without check=True -- shell errors swallowed silently",
                "Add check=True to raise CalledProcessError on failure.",
                @"subprocess\.run\s*\(",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"check\s*=\s*(True|False)" }),

            new ReviewRule("PY-COR-21", Severity.HIGH, Category.Bug, Language.Python,
                RuleScope.ClassLevel, FileFilter.All,
                "Mutable default in dataclass field -- shared across instances",
                "Use field(default_factory=list) instead of field: list = [].",
                @"^\s+\w+\s*:\s*(list|dict|set)\s*=\s*(\[\]|\{\}|set\(\))",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"default_factory" }),

            new ReviewRule("PY-COR-22", Severity.MEDIUM, Category.Bug, Language.Python,
                RuleScope.AnyMethod, FileFilter.All,
                "logging.error(msg, exc) loses stack trace -- use logging.exception()",
                "Use logging.exception('msg') or logger.error('msg', exc_info=True).",
                @"log(ger|ging)\.(error|warning)\s*\([^)]*,\s*\w*(err|exc|ex)\w*\s*\)",
                antiPatterns: new[]{ @"#\s*VB-IGNORE", @"^\s*#", @"exc_info" }),
        };
    }

    // =========================================================================
    //  Pass 2: Semantic deep analysis (C# only -- Python AST in CLI tool)
    //  Roslyn-inspired scope/type/flow tracking without Roslyn dependency.
    //  Pre-parses method boundaries, variable types, destroy flow, coroutines.
    // =========================================================================

    public static class DeepAnalyzer
    {
        // ----- Data structures -----

        sealed class MethodInfo
        {
            public string Name;
            public string ReturnType;
            public int StartLine, EndLine;
            public bool IsHotPath;       // Update / FixedUpdate / LateUpdate
            public bool IsLifecycle;     // Awake / Start / OnEnable / OnDisable / OnDestroy / OnGUI / etc.
            public bool IsOnDestroy;
            public bool IsOnDisable;
            public bool IsOnEnable;
            public bool IsAwakeOrStart;  // Awake or Start
            public Dictionary<string, string> LocalVarTypes = new Dictionary<string, string>();
            public List<DestroyCall> DestroyCalls = new List<DestroyCall>();
            public List<CoroutineStart> StartedCoroutines = new List<CoroutineStart>();
            public List<EventSub> EventSubscriptions = new List<EventSub>();
            public List<EventSub> EventUnsubscriptions = new List<EventSub>();
        }

        sealed class DestroyCall
        {
            public int Line;
            public string DestroyedVar; // the variable/expression passed to Destroy()
        }

        sealed class CoroutineStart
        {
            public int Line;
            public string CoroutineName; // method name passed to StartCoroutine
            public bool ResultStored;    // whether result is assigned to a variable
        }

        sealed class EventSub
        {
            public string Source;   // e.g. "button.onClick"
            public string Handler;  // e.g. "OnClick"
            public int Line;
        }

        sealed class ClassInfo
        {
            public string Name;
            public int StartLine, EndLine;
            public bool IsMonoBehaviour, IsScriptableObject;
            public List<MethodInfo> Methods = new List<MethodInfo>();
            public HashSet<string> DeclaredFields = new HashSet<string>();
            public HashSet<string> UsedFields = new HashSet<string>();
            public HashSet<string> SerializedFields = new HashSet<string>();
            public bool HasStopAllCoroutines;
            public HashSet<string> StoredCoroutineFields = new HashSet<string>();
        }

        // ----- Compiled regexes -----

        static readonly Regex ClassDecl = new Regex(
            @"class\s+(\w+)\s*(?::\s*([\w<>,\s]+))?\s*\{", RegexOptions.Compiled);
        static readonly Regex FieldDecl = new Regex(
            @"^\s+(?:\[[\w(,\s""=.]+\]\s*)*(private|protected|public|internal)?\s*(?:static\s+)?(?:readonly\s+)?(\w+(?:<[\w<>,\s]+>)?)\s+(\w+)\s*[;=]",
            RegexOptions.Compiled);
        static readonly Regex MethodDecl = new Regex(
            @"^\s*(?:(?:public|private|protected|internal|static|virtual|override|abstract|async|sealed|new)\s+)*(\w+(?:<[\w<>,\s]+>)?)\s+(\w+)\s*\(([^)]*)\)\s*(?:\{?\s*$)",
            RegexOptions.Compiled);
        static readonly Regex SubRx = new Regex(
            @"(\w+(?:\.\w+)*)\s*\+=\s*(\w+)", RegexOptions.Compiled);
        static readonly Regex UnsubRx = new Regex(
            @"(\w+(?:\.\w+)*)\s*-=\s*(\w+)", RegexOptions.Compiled);
        static readonly Regex FieldUseRx = new Regex(
            @"\b([_a-z]\w*)\b", RegexOptions.Compiled);
        // Variable declaration: explicit type
        static readonly Regex VarDeclExplicit = new Regex(
            @"^\s*(\w+(?:<[\w<>,\s]+>)?)\s+(\w+)\s*=", RegexOptions.Compiled);
        // Variable declaration: var with GetComponent<T>()
        static readonly Regex VarDeclGetComponent = new Regex(
            @"^\s*var\s+(\w+)\s*=\s*\w*\.?GetComponent<(\w+)>\s*\(", RegexOptions.Compiled);
        // Variable declaration: var x = something.gameObject / .transform
        static readonly Regex VarDeclUnityProp = new Regex(
            @"^\s*var\s+(\w+)\s*=\s*\w+\.(gameObject|transform|collider|rigidbody)\b",
            RegexOptions.Compiled);
        // Destroy(expr) or DestroyImmediate(expr)
        static readonly Regex DestroyRx = new Regex(
            @"(?:Destroy|DestroyImmediate)\s*\(\s*(\w+(?:\.\w+)?)", RegexOptions.Compiled);
        // StartCoroutine(MethodName(...)) or StartCoroutine("MethodName")
        static readonly Regex StartCoroutineRx = new Regex(
            @"StartCoroutine\s*\(\s*(?:""(\w+)""|(\w+)\s*\()", RegexOptions.Compiled);
        // Assignment storing coroutine result
        static readonly Regex CoroutineAssignRx = new Regex(
            @"(\w+)\s*=\s*StartCoroutine\s*\(", RegexOptions.Compiled);
        // StopAllCoroutines
        static readonly Regex StopAllRx = new Regex(
            @"StopAllCoroutines\s*\(", RegexOptions.Compiled);
        // Coroutine field declaration
        static readonly Regex CoroutineFieldRx = new Regex(
            @"Coroutine\s+(\w+)", RegexOptions.Compiled);

        static readonly HashSet<string> HotPathMethods = new HashSet<string> {
            "Update", "FixedUpdate", "LateUpdate", "OnGUI"
        };
        static readonly HashSet<string> LifecycleMethods = new HashSet<string> {
            "Awake", "Start", "OnEnable", "OnDisable", "OnDestroy", "OnGUI",
            "Update", "FixedUpdate", "LateUpdate", "OnApplicationQuit",
            "OnBecameVisible", "OnBecameInvisible", "OnCollisionEnter",
            "OnCollisionExit", "OnTriggerEnter", "OnTriggerExit"
        };
        static readonly HashSet<string> UnityObjectTypes = new HashSet<string> {
            "GameObject", "Transform", "Rigidbody", "Rigidbody2D",
            "Collider", "Collider2D", "BoxCollider", "SphereCollider",
            "CapsuleCollider", "MeshCollider", "Renderer", "MeshRenderer",
            "SkinnedMeshRenderer", "SpriteRenderer", "Camera", "Light",
            "AudioSource", "Animator", "Animation", "Canvas", "Image",
            "Text", "TextMeshProUGUI", "Button", "ParticleSystem",
            "NavMeshAgent", "CharacterController", "Component", "MonoBehaviour"
        };
        // Map Unity property shortcuts to types
        static readonly Dictionary<string, string> UnityPropTypes = new Dictionary<string, string> {
            { "gameObject", "GameObject" }, { "transform", "Transform" },
            { "collider", "Collider" }, { "rigidbody", "Rigidbody" }
        };
        // Primitive / non-Unity types to skip in variable tracking
        static readonly HashSet<string> SkipVarTypes = new HashSet<string> {
            "var", "int", "float", "string", "bool", "double", "long",
            "byte", "short", "uint", "ulong", "ushort", "sbyte", "char",
            "decimal", "void", "object", "dynamic"
        };

        // =================================================================
        //  Public API
        // =================================================================

        public static List<ReviewIssue> Analyze(string filePath, string[] lines)
        {
            var issues = new List<ReviewIssue>();
            var classes = ParseClasses(lines);

            foreach (var cls in classes)
            {
                // ---- DEEP-01: Enhanced event balance analysis ----
                CheckEventBalance(cls, filePath, issues);

                // ---- DEEP-02: Unused private fields ----
                foreach (var field in cls.DeclaredFields)
                {
                    if (!cls.UsedFields.Contains(field) && !cls.SerializedFields.Contains(field))
                        issues.Add(new ReviewIssue { RuleId = "DEEP-02", Severity = Severity.LOW,
                            Category = Category.Quality, Lang = Language.CSharp, FilePath = filePath,
                            Line = cls.StartLine + 1, Confidence = 65, Priority = 20,
                            Type = FindingType.Strengthening,
                            Description = $"Private field '{field}' in {cls.Name} appears unused",
                            Fix = "Remove the field if truly unused, or add [SerializeField] if intended for Inspector.",
                            MatchedText = field,
                            Reasoning = "Field declared but not referenced in the same class. May be accessed via reflection, serialization, or editor tooling. Verify before removing." });
                }

                // ---- DEEP-03: MonoBehaviour without lifecycle methods ----
                if (cls.IsMonoBehaviour)
                {
                    bool hasLife = cls.Methods.Any(m => m.IsLifecycle || m.IsHotPath);
                    if (!hasLife)
                        issues.Add(new ReviewIssue { RuleId = "DEEP-03", Severity = Severity.LOW,
                            Category = Category.Quality, Lang = Language.CSharp, FilePath = filePath,
                            Line = cls.StartLine + 1, Confidence = 60, Priority = 15,
                            Type = FindingType.Strengthening,
                            Description = $"MonoBehaviour '{cls.Name}' has no lifecycle methods -- may not need to be a MonoBehaviour",
                            Fix = "Convert to a plain C# class or static utility if no Unity lifecycle needed.",
                            MatchedText = cls.Name,
                            Reasoning = "This class inherits MonoBehaviour but doesn't override any lifecycle methods (Awake, Start, Update, etc.). It may be using MonoBehaviour solely for coroutine support or Inspector serialization, which are valid uses. Check if the class needs to be attached to a GameObject." });
                }

                // ---- DEEP-04: Destroy flow analysis ----
                CheckDestroyFlow(cls, lines, filePath, issues);

                // ---- DEEP-05: Coroutine lifecycle analysis ----
                CheckCoroutineLifecycle(cls, filePath, issues);

                // ---- DEEP-06: Cognitive complexity (SonarQube-inspired) ----
                foreach (var method in cls.Methods)
                {
                    int complexity = ComputeCognitiveComplexity(lines, method.StartLine, method.EndLine);
                    if (complexity > 15) // SonarQube default threshold
                    {
                        issues.Add(new ReviewIssue
                        {
                            RuleId = "DEEP-06",
                            Severity = complexity > 30 ? Severity.HIGH : Severity.MEDIUM,
                            Category = Category.Quality,
                            Lang = Language.CSharp,
                            Type = FindingType.Strengthening,
                            Confidence = 90, // Deterministic calculation
                            Priority = complexity > 30 ? 70 : 45,
                            FilePath = filePath,
                            Line = method.StartLine + 1,
                            Description = $"Method '{method.Name}' has cognitive complexity {complexity} (threshold: 15) -- deeply nested logic is hard to understand",
                            Fix = "Extract nested branches into well-named helper methods. Use guard clauses (early returns) to reduce nesting depth.",
                            MatchedText = method.Name,
                            Reasoning = complexity > 30
                                ? "Very high complexity -- this method likely has 4+ levels of nesting with conditionals inside loops. Strongly recommend refactoring."
                                : "Moderate complexity -- consider extracting the deepest nested block into a helper method."
                        });
                    }
                }
            }
            return issues;
        }

        // =================================================================
        //  Method lookup: find enclosing method for a given line index
        // =================================================================

        /// <summary>Returns the MethodInfo containing the given line, or null.</summary>
        static MethodInfo FindMethodAt(List<MethodInfo> methods, int lineIdx)
        {
            foreach (var m in methods)
                if (lineIdx >= m.StartLine && lineIdx <= m.EndLine) return m;
            return null;
        }

        // =================================================================
        //  DEEP-01: Enhanced event balance analysis
        // =================================================================

        static void CheckEventBalance(ClassInfo cls, string filePath, List<ReviewIssue> issues)
        {
            if (!cls.IsMonoBehaviour) return;

            // Collect all subscriptions and unsubscriptions with their method context
            var subsByMethod = new Dictionary<string, List<EventSub>>();
            var unsubsByMethod = new Dictionary<string, List<EventSub>>();

            foreach (var method in cls.Methods)
            {
                foreach (var s in method.EventSubscriptions)
                {
                    if (!subsByMethod.ContainsKey(method.Name))
                        subsByMethod[method.Name] = new List<EventSub>();
                    subsByMethod[method.Name].Add(s);
                }
                foreach (var u in method.EventUnsubscriptions)
                {
                    if (!unsubsByMethod.ContainsKey(method.Name))
                        unsubsByMethod[method.Name] = new List<EventSub>();
                    unsubsByMethod[method.Name].Add(u);
                }
            }

            // Build a flat set of all unsub keys for fallback matching
            var allUnsubKeys = new HashSet<string>();
            foreach (var kvp in unsubsByMethod)
                foreach (var u in kvp.Value)
                    allUnsubKeys.Add(u.Source + "+=" + u.Handler);

            // Check: += in OnEnable should have -= in OnDisable
            if (subsByMethod.ContainsKey("OnEnable"))
            {
                var disableUnsubs = unsubsByMethod.ContainsKey("OnDisable")
                    ? unsubsByMethod["OnDisable"] : new List<EventSub>();
                var disableKeys = new HashSet<string>(disableUnsubs.Select(u => u.Source + "+=" + u.Handler));

                foreach (var sub in subsByMethod["OnEnable"])
                {
                    string key = sub.Source + "+=" + sub.Handler;
                    if (!disableKeys.Contains(key) && !allUnsubKeys.Contains(key))
                        issues.Add(new ReviewIssue { RuleId = "DEEP-01", Severity = Severity.HIGH,
                            Category = Category.Unity, Lang = Language.CSharp, FilePath = filePath,
                            Line = sub.Line + 1, Confidence = 90, Priority = 85,
                            Type = FindingType.Bug,
                            Description = $"Event '{sub.Source} += {sub.Handler}' in OnEnable without matching -= in OnDisable in {cls.Name}",
                            Fix = "Add matching -= unsubscribe in OnDisable().",
                            MatchedText = key,
                            Reasoning = "OnEnable/OnDisable should be a matched pair. Subscribing in OnEnable without unsubscribing in OnDisable causes duplicate subscriptions on re-enable and memory leaks." });
                }
            }

            // Check: += in Awake/Start should have -= in OnDestroy
            foreach (var initMethod in new[] { "Awake", "Start" })
            {
                if (!subsByMethod.ContainsKey(initMethod)) continue;
                var destroyUnsubs = unsubsByMethod.ContainsKey("OnDestroy")
                    ? unsubsByMethod["OnDestroy"] : new List<EventSub>();
                var destroyKeys = new HashSet<string>(destroyUnsubs.Select(u => u.Source + "+=" + u.Handler));

                foreach (var sub in subsByMethod[initMethod])
                {
                    string key = sub.Source + "+=" + sub.Handler;
                    if (!destroyKeys.Contains(key) && !allUnsubKeys.Contains(key))
                        issues.Add(new ReviewIssue { RuleId = "DEEP-01", Severity = Severity.HIGH,
                            Category = Category.Unity, Lang = Language.CSharp, FilePath = filePath,
                            Line = sub.Line + 1, Confidence = 85, Priority = 80,
                            Type = FindingType.Bug,
                            Description = $"Event '{sub.Source} += {sub.Handler}' in {initMethod} without matching -= in OnDestroy in {cls.Name}",
                            Fix = $"Add matching -= unsubscribe in OnDestroy().",
                            MatchedText = key,
                            Reasoning = $"Subscribing in {initMethod} without unsubscribing in OnDestroy causes memory leaks when the object is destroyed." });
                }
            }

            // Fallback: any remaining += without ANY -= anywhere in the class
            foreach (var kvp in subsByMethod)
            {
                if (kvp.Key == "OnEnable" || kvp.Key == "Awake" || kvp.Key == "Start") continue;
                foreach (var sub in kvp.Value)
                {
                    string key = sub.Source + "+=" + sub.Handler;
                    if (!allUnsubKeys.Contains(key))
                        issues.Add(new ReviewIssue { RuleId = "DEEP-01", Severity = Severity.HIGH,
                            Category = Category.Unity, Lang = Language.CSharp, FilePath = filePath,
                            Line = sub.Line + 1, Confidence = 80, Priority = 75,
                            Type = FindingType.Bug,
                            Description = $"Event '{sub.Source} += {sub.Handler}' in {kvp.Key} never unsubscribed in {cls.Name}",
                            Fix = "Add -= unsubscribe in OnDisable() or OnDestroy().",
                            MatchedText = key,
                            Reasoning = "Cross-method analysis confirms += without matching -= anywhere in the class. Memory leak is highly likely." });
                }
            }
        }

        // =================================================================
        //  DEEP-04: Destroy flow analysis
        // =================================================================

        static void CheckDestroyFlow(ClassInfo cls, string[] lines, string filePath, List<ReviewIssue> issues)
        {
            foreach (var method in cls.Methods)
            {
                if (method.DestroyCalls.Count == 0) continue;
                foreach (var dc in method.DestroyCalls)
                {
                    if (string.IsNullOrEmpty(dc.DestroyedVar)) continue;
                    string varName = dc.DestroyedVar;
                    // Look for accesses to the destroyed variable AFTER the Destroy line
                    var accessRx = new Regex(@"\b" + Regex.Escape(varName) + @"\s*[\.\[\(]", RegexOptions.Compiled);
                    var assignRx = new Regex(@"\b" + Regex.Escape(varName) + @"\s*=\s*", RegexOptions.Compiled);
                    var nullCheckRx = new Regex(@"\b" + Regex.Escape(varName) + @"\s*[!=]=\s*null\b", RegexOptions.Compiled);
                    var nullCheckRevRx = new Regex(@"null\s*[!=]=\s*" + Regex.Escape(varName) + @"\b", RegexOptions.Compiled);
                    for (int i = dc.Line + 1; i <= method.EndLine && i < lines.Length; i++)
                    {
                        string ln = lines[i];
                        string trimmed = ln.TrimStart();
                        // Skip comments
                        if (trimmed.StartsWith("//")) continue;
                        // If variable is reassigned, stop tracking -- it points to a new reference
                        if (assignRx.IsMatch(ln)) break;
                        // If there is a return/break/continue right after Destroy, no issue
                        if (trimmed.StartsWith("return") || trimmed.StartsWith("break") || trimmed.StartsWith("continue")) break;
                        // Check if variable is accessed (member access, indexer, method call)
                        if (accessRx.IsMatch(ln))
                        {
                            // Don't flag null checks
                            if (nullCheckRx.IsMatch(ln)) continue;
                            if (nullCheckRevRx.IsMatch(ln)) continue;
                            issues.Add(new ReviewIssue { RuleId = "DEEP-04", Severity = Severity.HIGH,
                                Category = Category.Bug, Lang = Language.CSharp, FilePath = filePath,
                                Line = i + 1, Confidence = 85, Priority = 85,
                                Type = FindingType.Bug,
                                Description = $"Accessing destroyed object '{varName}' after Destroy() call on line {dc.Line + 1}",
                                Fix = "Add 'return;' after Destroy() or null-check before accessing the destroyed reference.",
                                MatchedText = varName,
                                Reasoning = "Semantic analysis detected Destroy() at line " + (dc.Line + 1) + " followed by member access on the same variable at line " + (i + 1) + ". Unity marks the object as destroyed but the C# reference persists, causing MissingReferenceException." });
                            break; // One finding per destroy call to avoid noise
                        }
                    }
                }
            }
        }

        // =================================================================
        //  DEEP-05: Coroutine lifecycle analysis
        // =================================================================

        static void CheckCoroutineLifecycle(ClassInfo cls, string filePath, List<ReviewIssue> issues)
        {
            if (!cls.IsMonoBehaviour) return;
            foreach (var method in cls.Methods)
            {
                foreach (var cr in method.StartedCoroutines)
                {
                    if (cr.ResultStored) continue; // stored result means it can be stopped
                    if (cls.HasStopAllCoroutines) continue; // StopAllCoroutines covers it
                    // Check if ANY coroutine field exists (class stores coroutine refs elsewhere)
                    if (cls.StoredCoroutineFields.Count > 0) continue;
                    issues.Add(new ReviewIssue { RuleId = "DEEP-05", Severity = Severity.MEDIUM,
                        Category = Category.Unity, Lang = Language.CSharp, FilePath = filePath,
                        Line = cr.Line + 1, Confidence = 75, Priority = 55,
                        Type = FindingType.Bug,
                        Description = $"StartCoroutine('{cr.CoroutineName}') result not stored -- cannot be stopped individually",
                        Fix = "Store: _coroutine = StartCoroutine(X()); then StopCoroutine(_coroutine) in cleanup.",
                        MatchedText = cr.CoroutineName,
                        Reasoning = "StartCoroutine result is not assigned to a variable and the class has no StopAllCoroutines call. If the coroutine needs to be cancelled (e.g. on disable, scene change, or state transition), there is no way to stop it individually. Fire-and-forget coroutines are sometimes intentional, hence moderate confidence." });
                }
            }
        }

        // =================================================================
        //  DEEP-06: Cognitive complexity scoring (SonarQube-inspired)
        //  Penalizes NESTING: if at depth 0 = +1, but if inside for inside if = +1+nesting
        // =================================================================

        static int ComputeCognitiveComplexity(string[] lines, int start, int end)
        {
            int score = 0;
            int nesting = 0;
            for (int i = start; i <= end && i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                // Skip full-line comments and doc-comment continuations
                if (trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("/*")) continue;

                // Strip leading '} ' so patterns like '} else if' and '} while' are matched
                string scoreLine = trimmed.TrimStart('}', ' ');

                // Increment operators: +1 + nesting_level
                if (Regex.IsMatch(scoreLine, @"^(if|else\s+if|switch)\s*\("))
                { score += 1 + nesting; }
                else if (Regex.IsMatch(scoreLine, @"^else\b"))
                { score += 1; } // else gets +1 but NOT +nesting (SonarQube spec)
                else if (Regex.IsMatch(scoreLine, @"^(for|foreach|while|do)\s*[\({]"))
                { score += 1 + nesting; }
                else if (scoreLine.StartsWith("catch"))
                { score += 1 + nesting; }
                // Ternary operator -- only if ? and : are not inside a string literal
                else if (scoreLine.Contains("?") && scoreLine.Contains(":") && !scoreLine.StartsWith("case"))
                {
                    // Verify ? is not inside a quoted string on this line
                    int qPos = trimmed.IndexOf('?');
                    if (qPos >= 0 && !LineClassifier.IsMatchInString(trimmed, qPos))
                        score += 1 + nesting;
                }

                // Boolean operator sequences: +1 per kind change (&&, ||)
                // Skip operators inside string literals
                var boolOps = Regex.Matches(trimmed, @"(&&|\|\|)");
                if (boolOps.Count > 0)
                {
                    string lastOp = null;
                    foreach (Match bm in boolOps)
                    {
                        if (LineClassifier.IsMatchInString(trimmed, bm.Index)) continue;
                        if (lastOp == null || bm.Value != lastOp)
                        { score += 1; lastOp = bm.Value; }
                    }
                }

                // Nesting tracking (after scoring, so opening brace on same line
                // doesn't count for THIS line's increment)
                nesting += LineClassifier.CountChar(lines[i], '{');
                nesting -= LineClassifier.CountChar(lines[i], '}');
                if (nesting < 0) nesting = 0;
            }
            return score;
        }

        // =================================================================
        //  Parsing: classes + methods + variable types + flow data
        // =================================================================

        static List<ClassInfo> ParseClasses(string[] lines)
        {
            var classes = new List<ClassInfo>();
            for (int i = 0; i < lines.Length; i++)
            {
                var m = ClassDecl.Match(lines[i]);
                if (!m.Success) continue;
                var cls = new ClassInfo { Name = m.Groups[1].Value, StartLine = i };
                string bases = m.Groups[2].Value;
                cls.IsMonoBehaviour = bases.Contains("MonoBehaviour");
                cls.IsScriptableObject = bases.Contains("ScriptableObject");
                int depth = 0;
                for (int j = i; j < lines.Length; j++)
                {
                    depth += LineClassifier.CountChar(lines[j], '{') - LineClassifier.CountChar(lines[j], '}');
                    if (depth <= 0 && j > i) { cls.EndLine = j; break; }
                    if (j == lines.Length - 1) cls.EndLine = j;

                    // Field declarations
                    var fm = FieldDecl.Match(lines[j]);
                    if (fm.Success && (fm.Groups[1].Value == "private" || fm.Groups[1].Value == ""))
                    {
                        cls.DeclaredFields.Add(fm.Groups[3].Value);
                        if (j > 0 && lines[j-1].Contains("[SerializeField]"))
                            cls.SerializedFields.Add(fm.Groups[3].Value);
                    }

                    // Field usage
                    if (j != i) foreach (Match fu in FieldUseRx.Matches(lines[j]))
                        cls.UsedFields.Add(fu.Groups[1].Value);

                    // StopAllCoroutines detection
                    if (StopAllRx.IsMatch(lines[j]))
                        cls.HasStopAllCoroutines = true;

                    // Coroutine field storage detection (Coroutine _xxx)
                    var crFieldMatch = CoroutineFieldRx.Match(lines[j]);
                    if (crFieldMatch.Success)
                        cls.StoredCoroutineFields.Add(crFieldMatch.Groups[1].Value);
                }

                // Parse methods within this class
                cls.Methods = ParseMethods(lines, cls.StartLine, cls.EndLine);
                classes.Add(cls);
            }
            return classes;
        }

        /// <summary>
        /// Parse all method declarations within the class body.
        /// Tracks brace depth for accurate method boundaries, then scans
        /// each method body for variable declarations, Destroy calls,
        /// coroutine starts, and event subscriptions.
        /// </summary>
        static List<MethodInfo> ParseMethods(string[] lines, int classStart, int classEnd)
        {
            var methods = new List<MethodInfo>();
            int classDepth = 0;
            for (int i = classStart; i <= classEnd && i < lines.Length; i++)
            {
                int openCount = LineClassifier.CountChar(lines[i], '{');
                int closeCount = LineClassifier.CountChar(lines[i], '}');
                int prevDepth = classDepth;
                classDepth += openCount - closeCount;

                // Method declarations appear at depth 1 (inside class, outside nested blocks)
                if (prevDepth != 1 && !(prevDepth == 0 && openCount > 0 && i > classStart))
                    continue;

                var mm = MethodDecl.Match(lines[i]);
                if (!mm.Success) continue;

                string returnType = mm.Groups[1].Value;
                string methodName = mm.Groups[2].Value;

                var mi = new MethodInfo
                {
                    Name = methodName,
                    ReturnType = returnType,
                    StartLine = i,
                    IsHotPath = HotPathMethods.Contains(methodName),
                    IsLifecycle = LifecycleMethods.Contains(methodName),
                    IsOnDestroy = methodName == "OnDestroy",
                    IsOnDisable = methodName == "OnDisable",
                    IsOnEnable = methodName == "OnEnable",
                    IsAwakeOrStart = methodName == "Awake" || methodName == "Start"
                };

                // Find method body end using brace depth tracking
                int methodDepth = 0;
                bool foundBody = false;
                for (int j = i; j <= classEnd && j < lines.Length; j++)
                {
                    int mOpen = LineClassifier.CountChar(lines[j], '{');
                    int mClose = LineClassifier.CountChar(lines[j], '}');
                    methodDepth += mOpen - mClose;
                    if (mOpen > 0 && !foundBody) foundBody = true;
                    if (foundBody && methodDepth <= 0)
                    {
                        mi.EndLine = j;
                        break;
                    }
                    if (j == classEnd || j == lines.Length - 1) mi.EndLine = j;
                }

                // Scan method body for semantic data
                ScanMethodBody(lines, mi);
                methods.Add(mi);
            }
            return methods;
        }

        /// <summary>
        /// Scan a method body for variable declarations, type inference,
        /// Destroy() calls, StartCoroutine calls, and event subscriptions.
        /// </summary>
        static void ScanMethodBody(string[] lines, MethodInfo mi)
        {
            for (int i = mi.StartLine; i <= mi.EndLine && i < lines.Length; i++)
            {
                string ln = lines[i];
                string trimmed = ln.TrimStart();
                if (trimmed.StartsWith("//")) continue;

                // -- Variable type tracking --

                // Explicit type: Transform t = ...; Animator anim = ...;
                var explicitMatch = VarDeclExplicit.Match(ln);
                if (explicitMatch.Success)
                {
                    string vType = explicitMatch.Groups[1].Value;
                    string vName = explicitMatch.Groups[2].Value;
                    if (!SkipVarTypes.Contains(vType))
                        mi.LocalVarTypes[vName] = vType;
                }

                // var x = GetComponent<T>() or var x = obj.GetComponent<T>()
                var gcMatch = VarDeclGetComponent.Match(ln);
                if (gcMatch.Success)
                    mi.LocalVarTypes[gcMatch.Groups[1].Value] = gcMatch.Groups[2].Value;

                // var x = something.gameObject / .transform / etc.
                var propMatch = VarDeclUnityProp.Match(ln);
                if (propMatch.Success)
                {
                    string propName = propMatch.Groups[2].Value;
                    if (UnityPropTypes.ContainsKey(propName))
                        mi.LocalVarTypes[propMatch.Groups[1].Value] = UnityPropTypes[propName];
                }

                // -- Destroy call tracking --
                var destroyMatch = DestroyRx.Match(ln);
                if (destroyMatch.Success)
                {
                    string destroyedExpr = destroyMatch.Groups[1].Value;
                    // Extract base variable name (strip .gameObject etc.)
                    string baseVar = destroyedExpr.Contains(".")
                        ? destroyedExpr.Substring(0, destroyedExpr.IndexOf('.'))
                        : destroyedExpr;
                    mi.DestroyCalls.Add(new DestroyCall { Line = i, DestroyedVar = baseVar });
                }

                // -- Coroutine tracking --
                var crMatch = StartCoroutineRx.Match(ln);
                if (crMatch.Success)
                {
                    string coroutineName = crMatch.Groups[1].Success
                        ? crMatch.Groups[1].Value   // string form: "MethodName"
                        : crMatch.Groups[2].Value;  // method form: MethodName(...)
                    bool stored = CoroutineAssignRx.IsMatch(ln);
                    mi.StartedCoroutines.Add(new CoroutineStart
                    {
                        Line = i,
                        CoroutineName = coroutineName,
                        ResultStored = stored
                    });
                }

                // -- Event subscription tracking --
                var subMatch = SubRx.Match(ln);
                if (subMatch.Success)
                    mi.EventSubscriptions.Add(new EventSub
                    {
                        Source = subMatch.Groups[1].Value,
                        Handler = subMatch.Groups[2].Value,
                        Line = i
                    });
                var unsubMatch = UnsubRx.Match(ln);
                if (unsubMatch.Success)
                    mi.EventUnsubscriptions.Add(new EventSub
                    {
                        Source = unsubMatch.Groups[1].Value,
                        Handler = unsubMatch.Groups[2].Value,
                        Line = i
                    });
            }
        }
    }

    // =========================================================================
    //  Main EditorWindow -- unified C# + Python
    // =========================================================================

    public sealed class VBCodeReviewer : EditorWindow
    {
        [MenuItem("VeilBreakers/Code Review/Open Reviewer")]
        static void Open() => GetWindow<VBCodeReviewer>("VB Code Reviewer");

        // State
        List<ReviewIssue> _issues = new List<ReviewIssue>();
        Vector2 _scroll;
        string _searchFilter = "";
        bool _groupByFile = true;
        Category? _categoryFilter = null;
        Severity? _severityFilter = null;
        int _langTab = 0; // 0=All, 1=C#, 2=Python
        bool _scanning = false;
        float _scanProgress = 0f;
        int _totalFiles, _scannedFiles;
        string _lastScanTime = "";
        DateTime _scanStart;
        int _criticalCount, _highCount, _mediumCount, _lowCount;

        // Sort
        enum SortCol { Severity, File, Line, Rule, Category }
        SortCol _sortCol = SortCol.Severity;
        bool _sortAsc = true;

        // Ignore / VB-IGNORE
        static readonly Regex CsIgnoreRx = new Regex(@"//\s*VB-IGNORE:\s*([\w,-]+)", RegexOptions.Compiled);
        static readonly Regex PyIgnoreRx = new Regex(@"#\s*VB-IGNORE:\s*([\w,-]+)", RegexOptions.Compiled);

        // Python toolkit path (configurable)
        string _pythonToolkitPath = "";

        // Colors
        static readonly Color CriticalCol = new Color(0.9f, 0.15f, 0.15f);
        static readonly Color HighCol = new Color(0.95f, 0.55f, 0.1f);
        static readonly Color MediumCol = new Color(0.95f, 0.85f, 0.2f);
        static readonly Color LowCol = new Color(0.4f, 0.7f, 0.95f);

        GUIStyle _headerStyle, _issueStyle, _countStyle;
        bool _stylesInit;

        void InitStyles()
        {
            if (_stylesInit) return;
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            _issueStyle = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
            _countStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            _stylesInit = true;
        }

        void OnEnable()
        {
            // Auto-detect toolkit path: look for mcp-toolkit relative to project
            string projRoot = Directory.GetParent(Application.dataPath).FullName;
            string candidate = Path.Combine(projRoot, "Tools", "mcp-toolkit", "src");
            if (!Directory.Exists(candidate))
            {
                // Try parent directories
                var dir = new DirectoryInfo(projRoot);
                while (dir != null && dir.Parent != null)
                {
                    candidate = Path.Combine(dir.FullName, "Tools", "mcp-toolkit", "src");
                    if (Directory.Exists(candidate)) break;
                    dir = dir.Parent;
                }
            }
            if (Directory.Exists(candidate)) _pythonToolkitPath = candidate;
        }

        void OnGUI()
        {
            InitStyles();
            DrawToolbar();
            DrawSummary();
            DrawFilters();
            DrawIssueList();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Scan Project", EditorStyles.toolbarButton, GUILayout.Width(100)))
                StartBatchScan();
            if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton, GUILayout.Width(90)))
                ExportJson();
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _issues.Clear();
                _criticalCount = _highCount = _mediumCount = _lowCount = 0;
            }
            _groupByFile = GUILayout.Toggle(_groupByFile, "Group", EditorStyles.toolbarButton, GUILayout.Width(50));
            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(_lastScanTime))
                GUILayout.Label(_lastScanTime, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (_scanning)
            {
                var rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(rect, _scanProgress, $"Scanning... {_scannedFiles}/{_totalFiles} files");
            }
        }

        void DrawSummary()
        {
            EditorGUILayout.BeginHorizontal();
            DrawCountBox("CRITICAL", _criticalCount, CriticalCol);
            DrawCountBox("HIGH", _highCount, HighCol);
            DrawCountBox("MEDIUM", _mediumCount, MediumCol);
            DrawCountBox("LOW", _lowCount, LowCol);
            EditorGUILayout.EndHorizontal();
        }

        void DrawCountBox(string label, int count, Color color)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = count > 0 ? color : Color.gray;
            EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 4 - 8));
            GUILayout.Label(count.ToString(), _countStyle);
            GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = prev;
        }

        void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal();

            // Language tabs
            string[] langNames = new[] { "All", "C#", "Python" };
            _langTab = GUILayout.Toolbar(_langTab, langNames, GUILayout.Width(200));

            _searchFilter = EditorGUILayout.TextField("Search", _searchFilter, GUILayout.Width(250));

            string[] catNames = new[] { "All", "Bug", "Performance", "Security", "Unity", "Quality" };
            int catIdx = _categoryFilter.HasValue ? (int)_categoryFilter.Value + 1 : 0;
            int newCat = EditorGUILayout.Popup("Category", catIdx, catNames, GUILayout.Width(180));
            _categoryFilter = newCat == 0 ? (Category?)null : (Category)(newCat - 1);

            string[] sevNames = new[] { "All", "CRITICAL", "HIGH", "MEDIUM", "LOW" };
            int sevIdx = _severityFilter.HasValue ? (int)_severityFilter.Value + 1 : 0;
            int newSev = EditorGUILayout.Popup("Severity", sevIdx, sevNames, GUILayout.Width(180));
            _severityFilter = newSev == 0 ? (Severity?)null : (Severity)(newSev - 1);

            EditorGUILayout.EndHorizontal();

            // Python toolkit path
            EditorGUILayout.BeginHorizontal();
            _pythonToolkitPath = EditorGUILayout.TextField("Python Toolkit Path", _pythonToolkitPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string sel = EditorUtility.OpenFolderPanel("Select Python Toolkit Path", _pythonToolkitPath, "");
                if (!string.IsNullOrEmpty(sel)) _pythonToolkitPath = sel;
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawIssueList()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var filtered = GetFilteredIssues();

            // Column headers
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Pri", EditorStyles.toolbarButton, GUILayout.Width(35))) ToggleSort(SortCol.Severity);
            if (GUILayout.Button("Type", EditorStyles.toolbarButton, GUILayout.Width(70))) ToggleSort(SortCol.Category);
            if (GUILayout.Button("Conf", EditorStyles.toolbarButton, GUILayout.Width(40)));
            if (GUILayout.Button("Rule", EditorStyles.toolbarButton, GUILayout.Width(85))) ToggleSort(SortCol.Rule);
            GUILayout.Label("L", EditorStyles.toolbarButton, GUILayout.Width(25));
            if (GUILayout.Button("File:Line", EditorStyles.toolbarButton, GUILayout.Width(200))) ToggleSort(SortCol.File);
            GUILayout.Label("Finding + Fix Suggestion", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            string lastFile = "";
            foreach (var issue in filtered)
            {
                // File group header
                if (_groupByFile && issue.FilePath != lastFile)
                {
                    lastFile = issue.FilePath;
                    string hdr = issue.FilePath;
                    if (hdr.StartsWith("Assets/")) hdr = hdr.Substring(7);
                    int fc = filtered.Count(x => x.FilePath == issue.FilePath);
                    EditorGUILayout.BeginHorizontal("toolbar");
                    GUILayout.Label($"  {hdr} ({fc} issues)", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                }

                Color rowColor = GetSevColor(issue.Severity);
                var prevColor = GUI.contentColor;
                GUI.contentColor = rowColor;

                EditorGUILayout.BeginHorizontal("box");

                // Priority score (0-100)
                GUILayout.Label($"{issue.Priority}", GUILayout.Width(35));

                // Finding type: ERROR/BUG/OPTIMIZE/STRENGTHEN
                GUILayout.Label(issue.TypeLabel, GUILayout.Width(70));

                // Confidence percentage
                Color confColor = issue.Confidence >= 90 ? new Color(0.2f, 0.9f, 0.2f) :
                                  issue.Confidence >= 75 ? new Color(0.9f, 0.9f, 0.2f) :
                                  new Color(0.9f, 0.5f, 0.2f);
                var prevCont = GUI.contentColor;
                GUI.contentColor = confColor;
                GUILayout.Label($"{issue.Confidence}%", GUILayout.Width(40));
                GUI.contentColor = prevCont;

                GUILayout.Label(issue.RuleId, GUILayout.Width(85));
                GUILayout.Label(issue.Lang == Language.CSharp ? "C#" : "Py", GUILayout.Width(25));

                string shortPath = issue.FilePath;
                if (shortPath.StartsWith("Assets/")) shortPath = shortPath.Substring(7);
                if (shortPath.Length > 35) shortPath = "..." + shortPath.Substring(shortPath.Length - 32);
                string fileLabel = $"{shortPath}:{issue.Line}";

                // Left-click: open file at line
                if (GUILayout.Button(fileLabel, EditorStyles.linkLabel, GUILayout.Width(200)))
                {
                    if (issue.Lang == Language.CSharp)
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.FilePath);
                        if (obj != null) AssetDatabase.OpenAsset(obj, issue.Line);
                    }
                    else
                    {
                        if (File.Exists(issue.FilePath))
                            System.Diagnostics.Process.Start(issue.FilePath);
                    }
                }

                // Description + Fix + Priority label
                string priLabel = issue.PriorityLabel;
                string matchSnippet = issue.MatchedText.Length > 80 ? issue.MatchedText.Substring(0, 80) + "..." : issue.MatchedText;
                string labelText = $"<b>[{priLabel}]</b> {issue.Description}\n<i>Fix: {issue.Fix}</i>\n<color=#888888>Code: {matchSnippet}</color>";
                if (issue.Confidence < 70 && !string.IsNullOrEmpty(issue.Reasoning))
                    labelText += $"\n<color=#FFB347>Reasoning ({issue.Confidence}%): {issue.Reasoning}</color>";
                GUILayout.Label(labelText, _issueStyle);
                EditorGUILayout.EndHorizontal();

                // Right-click context menu
                if (Event.current.type == EventType.ContextClick &&
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    var menu = new GenericMenu();
                    var capturedIssue = issue;
                    menu.AddItem(new GUIContent($"Ignore {issue.RuleId}"), false, () => AddIgnoreComment(capturedIssue));
                    menu.AddItem(new GUIContent($"Copy file path"), false, () => EditorGUIUtility.systemCopyBuffer = $"{capturedIssue.FilePath}:{capturedIssue.Line}");
                    menu.ShowAsContext();
                    Event.current.Use();
                }

                GUI.contentColor = prevColor;
            }

            EditorGUILayout.EndScrollView();

            // Summary with grade breakdown
            int errCount = filtered.Count(i => i.Type == FindingType.Error || i.Type == FindingType.Bug);
            int optCount = filtered.Count(i => i.Type == FindingType.Optimization);
            int strCount = filtered.Count(i => i.Type == FindingType.Strengthening);
            float avgPri = filtered.Count > 0 ? (float)filtered.Average(i => i.Priority) : 0f;
            float avgConf = filtered.Count > 0 ? (float)filtered.Average(i => i.Confidence) : 0f;
            EditorGUILayout.LabelField(
                $"Showing {filtered.Count}/{_issues.Count} | Errors/Bugs: {errCount} | Optimizations: {optCount} | Strengthening: {strCount} | Avg Priority: {avgPri:F0} | Avg Confidence: {avgConf:F0}%");
        }

        void AddIgnoreComment(ReviewIssue issue)
        {
            if (issue.Lang == Language.CSharp)
            {
                string fullPath = Path.Combine(Application.dataPath, "..", issue.FilePath).Replace('\\', '/');
                if (!File.Exists(fullPath)) return;
                var lines = File.ReadAllLines(fullPath).ToList();
                int idx = issue.Line - 1;
                if (idx >= 0 && idx < lines.Count)
                {
                    lines[idx] = lines[idx].TrimEnd() + $" // VB-IGNORE: {issue.RuleId}";
                    File.WriteAllLines(fullPath, lines, Encoding.UTF8);
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                if (!File.Exists(issue.FilePath)) return;
                var lines = File.ReadAllLines(issue.FilePath).ToList();
                int idx = issue.Line - 1;
                if (idx >= 0 && idx < lines.Count)
                {
                    lines[idx] = lines[idx].TrimEnd() + $"  # VB-IGNORE: {issue.RuleId}";
                    File.WriteAllLines(issue.FilePath, lines, Encoding.UTF8);
                }
            }
        }

        void ToggleSort(SortCol col)
        {
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
        }

        List<ReviewIssue> GetFilteredIssues()
        {
            var result = _issues.AsEnumerable();
            // Language filter
            if (_langTab == 1) result = result.Where(i => i.Lang == Language.CSharp);
            else if (_langTab == 2) result = result.Where(i => i.Lang == Language.Python);
            if (_categoryFilter.HasValue) result = result.Where(i => i.Category == _categoryFilter.Value);
            if (_severityFilter.HasValue) result = result.Where(i => i.Severity == _severityFilter.Value);
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                string lower = _searchFilter.ToLowerInvariant();
                result = result.Where(i =>
                    i.Description.ToLowerInvariant().Contains(lower) ||
                    i.FilePath.ToLowerInvariant().Contains(lower) ||
                    i.RuleId.ToLowerInvariant().Contains(lower));
            }
            switch (_sortCol)
            {
                case SortCol.Severity: result = _sortAsc ? result.OrderBy(i => i.Severity) : result.OrderByDescending(i => i.Severity); break;
                case SortCol.File: result = _sortAsc ? result.OrderBy(i => i.FilePath) : result.OrderByDescending(i => i.FilePath); break;
                case SortCol.Line: result = _sortAsc ? result.OrderBy(i => i.Line) : result.OrderByDescending(i => i.Line); break;
                case SortCol.Rule: result = _sortAsc ? result.OrderBy(i => i.RuleId) : result.OrderByDescending(i => i.RuleId); break;
                case SortCol.Category: result = _sortAsc ? result.OrderBy(i => i.Category) : result.OrderByDescending(i => i.Category); break;
            }
            return result.ToList();
        }

        Color GetSevColor(Severity sev)
        {
            switch (sev)
            {
                case Severity.CRITICAL: return CriticalCol;
                case Severity.HIGH: return HighCol;
                case Severity.MEDIUM: return MediumCol;
                case Severity.LOW: return LowCol;
                default: return Color.white;
            }
        }

        // Check if a regex match position falls inside a quoted string on the line
        static bool IsMatchInString(string line, int matchPos)
        {
            bool inSingle = false, inDouble = false;
            bool escaped = false;
            for (int k = 0; k < line.Length && k <= matchPos; k++)
            {
                if (escaped) { escaped = false; continue; }
                char c = line[k];
                if (c == '\\') { escaped = true; continue; }
                if (c == '\'' && !inDouble) inSingle = !inSingle;
                else if (c == '"' && !inSingle) inDouble = !inDouble;
                if (k == matchPos) return inSingle || inDouble;
            }
            return false;
        }

        // =====================================================================
        //  Batch scan with EditorApplication.delayCall for progress rendering
        // =====================================================================

        Queue<string> _scanQueue;
        bool _isCSharpPhase;

        void StartBatchScan()
        {
            _scanStart = DateTime.Now;
            _issues.Clear();
            _scannedFiles = 0;

            // Gather C# files
            string[] csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("PackageCache") && !f.Contains("Library") && !f.Contains("Temp"))
                .ToArray();

            // Gather Python files
            string[] pyFiles = new string[0];
            if (!string.IsNullOrEmpty(_pythonToolkitPath) && Directory.Exists(_pythonToolkitPath))
            {
                pyFiles = Directory.GetFiles(_pythonToolkitPath, "*.py", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("__pycache__") && !f.Contains(".venv") && !f.Contains("node_modules"))
                    .ToArray();
            }

            _totalFiles = csFiles.Length + pyFiles.Length;
            _scanQueue = new Queue<string>();
            _isCSharpPhase = true;

            foreach (var f in csFiles) _scanQueue.Enqueue(f);
            // Marker to switch to Python phase
            _scanQueue.Enqueue("__PYTHON_PHASE__");
            foreach (var f in pyFiles) _scanQueue.Enqueue(f);

            _scanning = true;
            ProcessScanBatch();
        }

        void ProcessScanBatch()
        {
            int batchSize = 20; // files per frame tick
            int processed = 0;

            while (_scanQueue.Count > 0 && processed < batchSize)
            {
                string path = _scanQueue.Dequeue();
                if (path == "__PYTHON_PHASE__")
                {
                    _isCSharpPhase = false;
                    continue;
                }

                try
                {
                    if (_isCSharpPhase)
                        ScanCSharpFile(path);
                    else
                        ScanPythonFile(path);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[VBCodeReviewer] Failed to scan {path}: {ex.Message}");
                }

                _scannedFiles++;
                processed++;
            }

            _scanProgress = _totalFiles > 0 ? (float)_scannedFiles / _totalFiles : 1f;
            Repaint();

            if (_scanQueue.Count > 0)
            {
                EditorApplication.delayCall += ProcessScanBatch;
            }
            else
            {
                FinishScan();
            }
        }

        void FinishScan()
        {
            _criticalCount = _issues.Count(i => i.Severity == Severity.CRITICAL);
            _highCount = _issues.Count(i => i.Severity == Severity.HIGH);
            _mediumCount = _issues.Count(i => i.Severity == Severity.MEDIUM);
            _lowCount = _issues.Count(i => i.Severity == Severity.LOW);
            _scanning = false;
            var elapsed = DateTime.Now - _scanStart;
            int csCount = _issues.Count(i => i.Lang == Language.CSharp);
            int pyCount = _issues.Count(i => i.Lang == Language.Python);
            _lastScanTime = $"{_totalFiles} files in {elapsed.TotalSeconds:F2}s | {_issues.Count} issues (C#: {csCount}, Py: {pyCount})";
            Repaint();
        }

        // =====================================================================
        //  C# file scanner
        // =====================================================================

        void ScanCSharpFile(string fullPath)
        {
            string content = File.ReadAllText(fullPath, Encoding.UTF8);
            string[] lines = content.Split('\n');

            // Pre-classify line contexts ONCE
            LineContext[] contexts = LineClassifier.Classify(lines);

            // Build ignored rules set
            var ignored = new HashSet<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                var m = CsIgnoreRx.Match(lines[i]);
                if (m.Success)
                    foreach (string rid in m.Groups[1].Value.Split(','))
                        ignored.Add(rid.Trim());
            }

            string relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length).Replace('\\', '/');
            bool isEditorFile = relativePath.Contains("/Editor/");

            // Build method signature index for Pattern-Inside / Pattern-Not-Inside checks
            // Maps line index -> enclosing method's signature line (null if not inside a method)
            string[] _methodSigForLine = new string[lines.Length];
            {
                string curSig = null;
                int mDepth = 0;
                int mStart = -1;
                for (int mi = 0; mi < lines.Length; mi++)
                {
                    if (contexts[mi] == LineContext.Comment) { _methodSigForLine[mi] = curSig; continue; }
                    var mSigMatch = Regex.Match(lines[mi].TrimStart(),
                        @"^(?:(?:private|protected|public|internal|static|override|virtual|abstract|async|unsafe|extern|sealed|new|partial|ref|readonly)\s+)*(?:[\w<>\[\],\.\?\s]+)\s+(\w+)\s*\(");
                    if (mSigMatch.Success && !lines[mi].TrimStart().StartsWith("//"))
                    {
                        if (curSig != null && mStart >= 0 && mDepth > 0)
                        {
                            // Previous method ended implicitly -- fill remaining lines
                        }
                        curSig = lines[mi];
                        mStart = mi;
                        mDepth = 0;
                    }
                    if (curSig != null)
                    {
                        mDepth += LineClassifier.CountChar(lines[mi], '{') - LineClassifier.CountChar(lines[mi], '}');
                        _methodSigForLine[mi] = curSig;
                        if (mDepth <= 0 && mi > mStart && lines[mi].Contains("}"))
                        {
                            _methodSigForLine[mi] = curSig; // closing brace is still inside
                            curSig = null;
                            mStart = -1;
                        }
                    }
                    else
                    {
                        _methodSigForLine[mi] = null;
                    }
                }
            }

            // Pass 1: Pattern matching with pre-classified contexts
            foreach (var rule in ReviewRules.CSharpRules)
            {
                if (ignored.Contains(rule.Id)) continue;

                // File filter: skip Editor files for Runtime rules, skip non-Editor for EditorOnly
                if (rule.Filter == FileFilter.Runtime && isEditorFile) continue;
                if (rule.Filter == FileFilter.EditorOnly && !isEditorFile) continue;

                // Skip UNITY-09 on editor files
                if (isEditorFile && rule.Id == "UNITY-09") continue;
                // SEC-01 skip Editor/ entirely
                if (isEditorFile && rule.Id == "SEC-01") continue;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("VB-IGNORE")) continue;

                    // Scope check via pre-classified context
                    if (!ReviewRules.MatchesScope(contexts[i], rule.Scope)) continue;

                    // Fast literal pre-filter before regex
                    if (rule.FastCheck != null && !lines[i].Contains(rule.FastCheck)) continue;
                    var csMatch = rule.Pattern.Match(lines[i]);
                    if (!csMatch.Success) continue;
                    if (IsMatchInString(lines[i], csMatch.Index)) continue;

                    // Anti-pattern suppression
                    if (rule.AntiPatterns != null && rule.AntiPatterns.Length > 0)
                    {
                        bool suppressed = false;
                        int lo = Math.Max(0, i - rule.AntiPatternRadius);
                        int hi = Math.Min(lines.Length - 1, i + rule.AntiPatternRadius);
                        for (int j = lo; j <= hi && !suppressed; j++)
                        {
                            for (int a = 0; a < rule.AntiPatterns.Length && !suppressed; a++)
                            {
                                if (rule.AntiPatterns[a].IsMatch(lines[j]))
                                    suppressed = true;
                            }
                        }
                        // Also check file path for path-based anti-patterns
                        if (!suppressed)
                        {
                            for (int a = 0; a < rule.AntiPatterns.Length && !suppressed; a++)
                            {
                                if (rule.AntiPatterns[a].IsMatch(relativePath))
                                    suppressed = true;
                            }
                        }
                        if (suppressed) continue;
                    }

                    // Pattern-Inside / Pattern-Not-Inside check (Semgrep-inspired scoping)
                    if (rule.InsidePattern != null || rule.NotInsidePattern != null)
                    {
                        string methodSig = _methodSigForLine[i];
                        if (rule.InsidePattern != null && (methodSig == null || !rule.InsidePattern.IsMatch(methodSig)))
                            continue;
                        if (rule.NotInsidePattern != null && methodSig != null && rule.NotInsidePattern.IsMatch(methodSig))
                            continue;
                    }

                    // Context guard
                    if (rule.ContextGuard != null && !rule.ContextGuard(lines[i], lines, i, contexts))
                        continue;

                    // Extract code context (3 lines before + 3 after)
                    var ctxSb = new StringBuilder();
                    for (int ci = Math.Max(0, i - 3); ci <= Math.Min(lines.Length - 1, i + 3); ci++)
                    {
                        string marker = ci == i ? ">>>" : "   ";
                        ctxSb.AppendLine($"{marker} {ci + 1}: {lines[ci].TrimEnd()}");
                    }
                    // Generate reasoning for low-confidence findings
                    string reasoning = rule.Reasoning;
                    if (string.IsNullOrEmpty(reasoning) && rule.Confidence < 70)
                    {
                        reasoning = $"[{rule.Confidence}% confident] Pattern matched but context may make it safe. " +
                                    "Review surrounding code to confirm this is a real issue.";
                    }
                    _issues.Add(new ReviewIssue
                    {
                        RuleId = rule.Id, Severity = rule.Severity,
                        Category = rule.Category, Lang = Language.CSharp,
                        Type = rule.Type, Confidence = rule.Confidence, Priority = rule.Priority,
                        FilePath = relativePath, Line = i + 1,
                        Description = rule.Description, Fix = rule.Fix,
                        MatchedText = lines[i].Trim(),
                        Reasoning = reasoning ?? "",
                        CodeContext = ctxSb.ToString()
                    });
                }
            }

            // Pass 2: Deep analysis
            var deep = DeepAnalyzer.Analyze(relativePath, lines);
            foreach (var d in deep)
                if (!ignored.Contains(d.RuleId))
                    _issues.Add(d);
        }

        // =====================================================================
        //  Python file scanner (regex only -- no C# AST for Python)
        // =====================================================================

        void ScanPythonFile(string fullPath)
        {
            string content = File.ReadAllText(fullPath, Encoding.UTF8);
            string[] lines = content.Split('\n');

            var ignored = new HashSet<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                var m = PyIgnoreRx.Match(lines[i]);
                if (m.Success)
                    foreach (string rid in m.Groups[1].Value.Split(','))
                        ignored.Add(rid.Trim());
            }

            // Simple Python line context (comment detection)
            string pyTripleDQ = new string('"', 3);
            string pyTripleSQ = new string((char)39, 3);
            bool inTripleQuote = false;
            var pyCtx = new LineContext[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (inTripleQuote)
                {
                    pyCtx[i] = LineContext.StringLiteral;
                    if (trimmed.Contains(pyTripleDQ) || trimmed.Contains(pyTripleSQ)) inTripleQuote = false;
                    continue;
                }
                // Handle triple-quoted strings with optional prefix (r, b, f, rb, etc.)
                string stripped = trimmed.TrimStart('r', 'b', 'f', 'u', 'R', 'B', 'F', 'U');
                if (stripped.StartsWith(pyTripleDQ) || stripped.StartsWith(pyTripleSQ) ||
                    trimmed.Contains("= r" + pyTripleSQ) || trimmed.Contains("= r" + pyTripleDQ) ||
                    trimmed.Contains("=" + pyTripleSQ) || trimmed.Contains("=" + pyTripleDQ))
                {
                    pyCtx[i] = LineContext.StringLiteral;
                    int dqCount = 0, sqCount = 0;
                    for (int k = 0; k + 2 < trimmed.Length; k++)
                    {
                        if (trimmed[k] == '"' && trimmed[k+1] == '"' && trimmed[k+2] == '"') dqCount++;
                        if (trimmed[k] == (char)39 && trimmed[k+1] == (char)39 && trimmed[k+2] == (char)39) sqCount++;
                    }
                    if ((dqCount + sqCount) % 2 == 1) inTripleQuote = true;
                    continue;
                }
                if (trimmed.StartsWith("#"))
                {
                    pyCtx[i] = LineContext.Comment;
                    continue;
                }
                pyCtx[i] = LineContext.Cold;
            }

            foreach (var rule in ReviewRules.PythonRules)
            {
                if (ignored.Contains(rule.Id)) continue;
                if (rule.Pattern.ToString().Contains("SENTINEL")) continue;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("VB-IGNORE")) continue;
                    if (pyCtx[i] == LineContext.Comment || pyCtx[i] == LineContext.StringLiteral) continue;

                    // Fast literal pre-filter before regex
                    if (rule.FastCheck != null && !lines[i].Contains(rule.FastCheck)) continue;
                    var pm = rule.Pattern.Match(lines[i]);
                    if (!pm.Success) continue;

                    // Skip if match is inside a quoted string on this line
                    if (IsMatchInString(lines[i], pm.Index)) continue;

                    // Anti-pattern suppression
                    if (rule.AntiPatterns != null && rule.AntiPatterns.Length > 0)
                    {
                        bool suppressed = false;
                        int lo = Math.Max(0, i - rule.AntiPatternRadius);
                        int hi = Math.Min(lines.Length - 1, i + rule.AntiPatternRadius);
                        for (int j = lo; j <= hi && !suppressed; j++)
                            for (int a = 0; a < rule.AntiPatterns.Length && !suppressed; a++)
                                if (rule.AntiPatterns[a].IsMatch(lines[j])) suppressed = true;
                        if (!suppressed)
                            for (int a = 0; a < rule.AntiPatterns.Length && !suppressed; a++)
                                if (rule.AntiPatterns[a].IsMatch(fullPath)) suppressed = true;
                        if (suppressed) continue;
                    }

                    if (rule.ContextGuard != null && !rule.ContextGuard(lines[i], lines, i, pyCtx))
                        continue;

                    var ctxSb2 = new StringBuilder();
                    for (int ci = Math.Max(0, i - 3); ci <= Math.Min(lines.Length - 1, i + 3); ci++)
                    {
                        string marker = ci == i ? ">>>" : "   ";
                        ctxSb2.AppendLine($"{marker} {ci + 1}: {lines[ci].TrimEnd()}");
                    }
                    string pyReasoning = rule.Reasoning;
                    if (string.IsNullOrEmpty(pyReasoning) && rule.Confidence < 70)
                    {
                        pyReasoning = $"[{rule.Confidence}% confident] Pattern matched but context may make it safe. " +
                                      "Review surrounding code to confirm.";
                    }
                    _issues.Add(new ReviewIssue
                    {
                        RuleId = rule.Id, Severity = rule.Severity,
                        Category = rule.Category, Lang = Language.Python,
                        Type = rule.Type, Confidence = rule.Confidence, Priority = rule.Priority,
                        FilePath = fullPath.Replace('\\', '/'), Line = i + 1,
                        Description = rule.Description, Fix = rule.Fix,
                        MatchedText = lines[i].Trim(),
                        Reasoning = pyReasoning ?? "",
                        CodeContext = ctxSb2.ToString()
                    });
                }
            }
        }

        // =====================================================================
        //  JSON export
        // =====================================================================

        void ExportJson()
        {
            string path = EditorUtility.SaveFilePanel("Export Code Review Results", "", "vb_code_review.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"scan_time\": \"{_lastScanTime}\",");
            sb.AppendLine($"  \"total_issues\": {_issues.Count},");
            sb.AppendLine($"  \"critical\": {_criticalCount},");
            sb.AppendLine($"  \"high\": {_highCount},");
            sb.AppendLine($"  \"medium\": {_mediumCount},");
            sb.AppendLine($"  \"low\": {_lowCount},");
            sb.AppendLine("  \"issues\": [");
            for (int i = 0; i < _issues.Count; i++)
            {
                var issue = _issues[i];
                string esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
                sb.AppendLine("    {");
                sb.AppendLine($"      \"rule_id\": \"{issue.RuleId}\",");
                sb.AppendLine($"      \"severity\": \"{issue.Severity}\",");
                sb.AppendLine($"      \"category\": \"{issue.Category}\",");
                sb.AppendLine($"      \"type\": \"{issue.TypeLabel}\",");
                sb.AppendLine($"      \"confidence\": {issue.Confidence},");
                sb.AppendLine($"      \"confidence_label\": \"{issue.ConfidenceLabel}\",");
                sb.AppendLine($"      \"priority\": {issue.Priority},");
                sb.AppendLine($"      \"priority_label\": \"{issue.PriorityLabel}\",");
                sb.AppendLine($"      \"language\": \"{(issue.Lang == Language.CSharp ? "C#" : "Python")}\",");
                sb.AppendLine($"      \"file\": \"{esc(issue.FilePath.Replace("\\", "/"))}\",");
                sb.AppendLine($"      \"line\": {issue.Line},");
                sb.AppendLine($"      \"description\": \"{esc(issue.Description)}\",");
                sb.AppendLine($"      \"fix_suggestion\": \"{esc(issue.Fix)}\",");
                sb.AppendLine($"      \"matched_text\": \"{esc(issue.MatchedText)}\",");
                sb.AppendLine($"      \"reasoning\": \"{esc(issue.Reasoning ?? "")}\",");
                sb.AppendLine($"      \"code_context\": \"{esc(issue.CodeContext ?? "")}\"");
                sb.Append("    }");
                if (i < _issues.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[VBCodeReviewer] Exported {_issues.Count} issues to {path}");
        }

        // =====================================================================
        //  Headless / CI API
        // =====================================================================

        public static string RunHeadless()
        {
            var reviewer = CreateInstance<VBCodeReviewer>();
            reviewer.OnEnable();
            // Synchronous scan for CI
            reviewer._scanStart = DateTime.Now;
            reviewer._issues.Clear();
            string[] csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("PackageCache") && !f.Contains("Library") && !f.Contains("Temp")).ToArray();
            foreach (var f in csFiles) { try { reviewer.ScanCSharpFile(f); } catch {} }
            if (!string.IsNullOrEmpty(reviewer._pythonToolkitPath) && Directory.Exists(reviewer._pythonToolkitPath))
            {
                string[] pyFiles = Directory.GetFiles(reviewer._pythonToolkitPath, "*.py", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("__pycache__")).ToArray();
                foreach (var f in pyFiles) { try { reviewer.ScanPythonFile(f); } catch {} }
            }
            reviewer._criticalCount = reviewer._issues.Count(i => i.Severity == Severity.CRITICAL);
            reviewer._highCount = reviewer._issues.Count(i => i.Severity == Severity.HIGH);
            reviewer._mediumCount = reviewer._issues.Count(i => i.Severity == Severity.MEDIUM);
            reviewer._lowCount = reviewer._issues.Count(i => i.Severity == Severity.LOW);
            var sb = new StringBuilder();
            sb.AppendLine($"VB Code Review: {reviewer._issues.Count} issues (C#: {reviewer._issues.Count(i => i.Lang == Language.CSharp)}, Python: {reviewer._issues.Count(i => i.Lang == Language.Python)})");
            sb.AppendLine($"  CRITICAL: {reviewer._criticalCount}  HIGH: {reviewer._highCount}  MEDIUM: {reviewer._mediumCount}  LOW: {reviewer._lowCount}");
            DestroyImmediate(reviewer);
            return sb.ToString();
        }

        [MenuItem("VeilBreakers/Code Review/Run Headless (Console Output)")]
        static void RunHeadlessMenu() => Debug.Log(RunHeadless());
    }
}