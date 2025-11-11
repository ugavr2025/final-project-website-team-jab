using System;
using UnityEngine;

public class Midi2Locomotion : MonoBehaviour
{
    [System.Serializable]
    public struct NoteDirection
    {
        [Tooltip("音名前缀，例如 C、D#、Gb。大小写忽略，只匹配开头。")]
        public string notePrefix;
        [Tooltip("局部空间方向 (x=右, z=前)。会根据 facing/target 转到世界空间。")]
        public Vector3 localDirection;
    }

    [Header("Targets")]
    [Tooltip("通常就是 XR Origin（挂谁就用谁）")]
    public Transform target;
    [Tooltip("朝向参考：建议拖 Main Camera。为空则用 target.forward")]
    public Transform facing;

    [Header("Step Settings")]
    [Tooltip("每条 MIDI 消息触发的基础步进距离（米）")]
    public float baseStepDistance = 0.5f;

    [Tooltip("vel(0~127) 的步进加成系数")]
    public float velWeight = 0.5f;

    [Tooltip("payload speed(0~1) 的步进加成系数")]
    public float payloadSpeedWeight = 0.5f;

    [Tooltip("执行一步时的移动速度（米/秒）")]
    public float moveSpeed = 2.0f;

    [Header("Note Directions")]
    [Tooltip("音符对应的朝向映射；未匹配时默认使用 forward")]
    public NoteDirection[] noteDirections = new NoteDirection[]
    {
        new NoteDirection{ notePrefix = "C",  localDirection = new Vector3(0f, 0f, 1f) },
        new NoteDirection{ notePrefix = "D",  localDirection = new Vector3(1f, 0f, 0f) },
        new NoteDirection{ notePrefix = "E",  localDirection = new Vector3(0f, 0f, -1f) },
        new NoteDirection{ notePrefix = "F",  localDirection = new Vector3(-1f, 0f, 0f) },
        new NoteDirection{ notePrefix = "G",  localDirection = new Vector3(1f, 0f, 1f) },
        new NoteDirection{ notePrefix = "A",  localDirection = new Vector3(-1f, 0f, 1f) },
        new NoteDirection{ notePrefix = "B",  localDirection = new Vector3(0f, 0f, 1f) },
    };

    [Header("Debug")]
    public bool showDebug = true;

    // DataManager 把原始消息塞到这里
    [NonSerialized] public string data = null;

    // 内部状态
    float _distanceRemaining = 0f;
    Vector3 _currentDirection = Vector3.zero;
    CharacterController _cc;

    void Awake()
    {
        if (target == null) target = transform;
        _cc = target.GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1) 若有新消息 -> 累加步进距离并选择方向
        string raw = data;
        if (!string.IsNullOrEmpty(raw))
        {
            float vel = 0f, pspd = 0f;
            int midiNumber = -1;
            string noteName = null;
            try
            {
                int semi = raw.IndexOf(';');
                string head = semi >= 0 ? raw.Substring(0, semi) : raw;
                var parts = head.Split(',');
                foreach (var p in parts)
                {
                    var kv = p.Split(':');
                    if (kv.Length != 2) continue;
                    var k = kv[0].Trim();
                    var v = kv[1].Trim();
                    if (k == "vel") float.TryParse(v, out vel);
                    else if (k == "speed") float.TryParse(v, out pspd);
                    else if (k == "note") noteName = v;
                    else if (k == "midi") int.TryParse(v, out midiNumber);
                }

                if (string.IsNullOrEmpty(noteName) && semi >= 0)
                {
                    string tail = raw.Substring(semi + 1).Trim();
                    if (!string.IsNullOrEmpty(tail))
                        noteName = tail;
                }
            }
            catch { /* 容错解析 */ }

            float velFactor = Mathf.Clamp01(vel / 127f);
            float spdFactor = Mathf.Clamp01(pspd);

            float factor = 1f + velWeight * velFactor + payloadSpeedWeight * spdFactor;
            float step = baseStepDistance * factor;
            _distanceRemaining = step;
            _currentDirection = ResolveDirection(GetNoteName(noteName, midiNumber));

            if (showDebug)
                Debug.Log($"[Midi2Locomotion] step: note={GetNoteName(noteName, midiNumber)} vfac={velFactor:0.00} sfac={spdFactor:0.00} -> +{step:0.00}m dir={_currentDirection} (remaining={_distanceRemaining:0.00}m)");

            // 消耗掉本次消息，避免下一帧重复解析同一个音
            data = null;
        }

        // 2) 按“身体/相机朝向”的水平 forward 前进
        Vector3 fwd = facing
            ? Vector3.ProjectOnPlane(facing.forward, Vector3.up).normalized
            : Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;

        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;

        if (_distanceRemaining > 0f)
        {
            Vector3 worldDir = (_currentDirection.sqrMagnitude > 1e-6f)
                ? ConvertToWorldDirection(fwd, _currentDirection)
                : fwd;
            if (worldDir.sqrMagnitude < 1e-6f) worldDir = fwd;
            worldDir.Normalize();

            float step = Mathf.Min(_distanceRemaining, moveSpeed * Time.deltaTime);
            Vector3 delta = worldDir * step;

            if (_cc) _cc.Move(delta);
            else target.position += delta;

            _distanceRemaining -= step;
        }
    }

    string GetNoteName(string rawNote, int midiNumber)
    {
        if (!string.IsNullOrEmpty(rawNote))
            return rawNote;

        if (midiNumber >= 0)
        {
            string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int note = midiNumber % 12;
            int octave = (midiNumber / 12) - 1; // MIDI octave convention
            return $"{names[note]}{octave}";
        }

        return null;
    }

    Vector3 ResolveDirection(string noteName)
    {
        if (noteDirections == null || noteDirections.Length == 0 || string.IsNullOrEmpty(noteName))
            return Vector3.zero;

        string upper = noteName.ToUpperInvariant();
        foreach (var map in noteDirections)
        {
            if (string.IsNullOrEmpty(map.notePrefix)) continue;
            if (upper.StartsWith(map.notePrefix.ToUpperInvariant()))
                return new Vector3(map.localDirection.x, 0f, map.localDirection.z);
        }
        return Vector3.zero;
    }

    Vector3 ConvertToWorldDirection(Vector3 forward, Vector3 localDir)
    {
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 dir = right * localDir.x + forward * localDir.z;
        if (dir.sqrMagnitude < 1e-6f)
            dir = forward;
        return dir.normalized;
    }
}
