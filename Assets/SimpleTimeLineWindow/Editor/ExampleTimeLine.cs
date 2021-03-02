using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DMTimeArea;
using System;

public class ExampleTimeLine : SimpleTimeArea
{
    private Rect rectTotalArea;
    private Rect rectContent;
    private Rect rectTimeRuler;

    private Rect rectTopBar;
    private Rect rectLeft;
    public Rect rectLeftTopToolBar;

    public AnimationCurve m_AnimationCurve;

    private float _lastUpdateTime = 0f;
    #region Used
    private double runningTime = 10.0f;
    protected override double RunningTime
    {
        get { return runningTime; }
        set
        {
            runningTime = value;
        }
    }

    private static double cutOffTime = 15.0f;
    protected override double CutOffTime
    {
        get { return cutOffTime; }
        set
        {
            cutOffTime = value;
        }
    }

    private float LEFTWIDTH = 250f;

    public bool IsPlaying
    {
        get;
        set;
    }

    protected override bool IsLockedMoveFrame
    {
        get { return (IsPlaying || Application.isPlaying); }
    }

    protected override bool IsLockDragHeaderArrow
    {
        get { return IsPlaying; }
    }

    public override Rect _rectTimeAreaTotal
    {
        get { return rectTotalArea; }
    }

    public override Rect _rectTimeAreaContent
    {
        get { return rectContent; }
    }

    public override Rect _rectTimeAreaRuler
    {
        get { return rectTimeRuler; }
    }

    protected override float sequencerHeaderWidth
    {
        get { return LEFTWIDTH; }
    }

    #endregion

    [MenuItem("Window/ExampleTimeLineWindow", false, 2002)]
    public static void DoWindow()
    {
        var window = GetWindow<ExampleTimeLine>(false, "ExampleTimeLine");
        window.minSize = new Vector3(400f, 200f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update = (EditorApplication.CallbackFunction)System.Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(OnEditorUpdate));
        _lastUpdateTime = (float)EditorApplication.timeSinceStartup;
    }

    private void OnDisable()
    {
        EditorApplication.update = (EditorApplication.CallbackFunction)System.Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(OnEditorUpdate));
    }

    private void OnEditorUpdate()
    {
        // float delta = (float)(EditorApplication.timeSinceStartup - _lastUpdateTime);
        if (!Application.isPlaying && this.IsPlaying)
        {
            double fTime = (float)EditorApplication.timeSinceStartup - _lastUpdateTime;
            this.RunningTime += Math.Abs(fTime) * 1.0f;
            if (this.RunningTime >= this.CutOffTime)
            {
                this.PausePreView();
            }
        }
        //if (_simpleSample)
        //    BlendCenter.BlendAnimation((float)this.RunningTime, aa01, aa02, _targetObject);

        _lastUpdateTime = (float)EditorApplication.timeSinceStartup;
        Repaint();
    }

    private void OnGUI()
    {
        Rect rectMainBodyArea = new Rect(0, toolbarHeight, base.position.width, this.position.height - toolbarHeight);
        rectTopBar = new Rect(0, 0, this.position.width, toolbarHeight);
        rectLeft = new Rect(rectMainBodyArea.x, rectMainBodyArea.y + timeRulerHeight, LEFTWIDTH, rectMainBodyArea.height);
        rectLeftTopToolBar = new Rect(rectMainBodyArea.x, rectMainBodyArea.y, LEFTWIDTH, timeRulerHeight);

        rectTotalArea = new Rect(rectMainBodyArea.x + LEFTWIDTH, rectMainBodyArea.y, base.position.width - LEFTWIDTH, rectMainBodyArea.height);
        rectTimeRuler = new Rect(rectMainBodyArea.x + LEFTWIDTH, rectMainBodyArea.y, base.position.width - LEFTWIDTH, timeRulerHeight);
        rectContent = new Rect(rectMainBodyArea.x + LEFTWIDTH, rectMainBodyArea.y + timeRulerHeight, base.position.width - LEFTWIDTH, rectMainBodyArea.height - timeRulerHeight);

        InitTimeArea(false, false, true, true);
        DrawTimeAreaBackGround();
        OnTimeRulerCursorAndCutOffCursorInput();
        DrawTimeRulerArea();

        // Draw your top bar
        DrawTopToolBar();
        // Draw left content
        DrawLeftContent();
        // Draw your left tool bar
        DrawLeftTopToolBar();

        GUILayout.BeginArea(rectContent);
        DrawCurveLine(rectTotalArea.x);

        GUILayout.EndArea();
    }


    protected override void DrawVerticalTickLine()
    {
        Color preColor = Handles.color;
        Color color = Color.white;
        color.a = 0.3f;
        Handles.color = color;
        // draw vertical ticks
        float step = 10;
        float preStep = GetTimeArea.drawRect.height / 20f;
        // step = GetTimeArea.drawRect.y;
        step = 0f;
        while (step <= GetTimeArea.drawRect.height + GetTimeArea.drawRect.y)
        {
            Vector2 pos = new Vector2(rectContent.x, step + GetTimeArea.drawRect.y);
            Vector2 endPos = new Vector2(position.width, step + GetTimeArea.drawRect.y);
            step += preStep;
            float height = PixelToY(step);
            Rect rect = new Rect(rectContent.x + 5f, step - 10f + GetTimeArea.drawRect.y, 100f, 20f);
            GUI.Label(rect, height.ToString("0"));
            Handles.DrawLine(pos, endPos);
        }
        Handles.color = preColor;
    }

    protected virtual void DrawLeftContent()
    {
        GUILayout.BeginArea(rectLeft);
        GUILayout.Label("Draw your left content");
        if (m_AnimationCurve == null)
            m_AnimationCurve = new AnimationCurve();
        m_AnimationCurve = EditorGUILayout.CurveField("Target Curve", m_AnimationCurve);
        GUILayout.EndArea();
    }

    protected virtual void DrawTopToolBar()
    {
        GUILayout.BeginArea(rectTopBar);
        Rect rect = new Rect(rectTopBar.width - 32, rectTopBar.y, 30, 30);
        if (!Application.isPlaying && GUI.Button(rect, ResManager.SettingIcon, EditorStyles.toolbarDropDown))
        {
            OnClickSettingButton();
        }
        GUILayout.EndArea();
    }

    private void DrawLeftTopToolBar()
    {
        // left top tool bar
        GUILayout.BeginArea(rectLeftTopToolBar, string.Empty, EditorStyles.toolbarButton);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(ResManager.prevKeyContent, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
        {
            PreviousTimeFrame();
        }

        bool playing = IsPlaying;
        playing = GUILayout.Toggle(playing, ResManager.playContent, EditorStyles.toolbarButton, new GUILayoutOption[0]);
        if (!Application.isPlaying)
        {
            if (IsPlaying != playing)
            {
                IsPlaying = playing;
                if (IsPlaying)
                    PlayPreview();
                else
                    PausePreView();
            }
        }

        if (GUILayout.Button(ResManager.nextKeyContent, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
        {
            NextTimeFrame();
        }

        if (GUILayout.Button(ResManager.StopIcon, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false))
            && !Application.isPlaying)
        {
            PausePreView();
            this.RunningTime = 0.0f;
        }

        GUILayout.FlexibleSpace();
        string timeStr = TimeAsString((double)this.RunningTime, "F2");
        GUILayout.Label(timeStr);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void PlayPreview()
    {
        IsPlaying = true;
    }

    private void PausePreView()
    {
        IsPlaying = false;
    }

    private void DrawCurveLine(float offsetX)
    {
        // draw curve
        Keyframe[] keys = m_AnimationCurve.keys;
        int keyCount = keys.Length;
        if (keyCount > 1)
        {
            //Keyframe startKey = keys[0];
            //Keyframe endKey = keys[keyCount - 1];
            m_cachePoints.Clear();
            m_segmentResolution = Mathf.Clamp(m_segmentResolution, 3, 50);
            for (int i = 0; i < keyCount - 1; i++)
            {
                Keyframe cur = keys[i];
                Keyframe next = keys[i + 1];

                m_cachePoints.Add(new Vector3(cur.time, cur.value));
                float num = Mathf.Lerp(cur.time, next.time, 0.001f / (float)m_segmentResolution);
                m_cachePoints.Add(new Vector3(num, m_AnimationCurve.Evaluate(num)));

                for (float num2 = 1f; num2 < (float)m_segmentResolution; num2 += 1f)
                {
                    num = Mathf.Lerp(cur.time, next.time, num2 / (float)m_segmentResolution);
                    m_cachePoints.Add(new Vector3(num, m_AnimationCurve.Evaluate(num)));
                }
                num = Mathf.Lerp(cur.time, next.time, 1f - 0.000f / (float)m_segmentResolution);
                m_cachePoints.Add(new Vector3(num, m_AnimationCurve.Evaluate(num)));
                m_cachePoints.Add(new Vector3(next.time, next.value));
            }

            for (int i = 0; i < m_cachePoints.Count; i++)
            {
                float time = m_cachePoints[i].x;
                float value = m_cachePoints[i].y;
                time = TimeToPixel((double)time) - offsetX;
                value = YToPixel(value);

                Vector2 pos = new Vector2(time, value);
                m_cachePoints[i] = pos;
            }
            if (m_cachePoints.Count != 0)
            {
                Handles.BeginGUI();
                Handles.color = Color.white;
                Handles.DrawAAPolyLine(3, m_cachePoints.ToArray());
                Handles.EndGUI();
            }

            keys = m_AnimationCurve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                float time = keys[i].time;
                float value = keys[i].value;
                int iconWidth = 12;
                value = YToPixel(value);
                time = TimeToPixel((double)time) - offsetX;
                Vector2 pos = new Vector2(time, value);

                Rect rect = new Rect(pos.x - iconWidth * 0.5f, pos.y - iconWidth * 0.5f, iconWidth, iconWidth);
                if (iconTexture == null)
                    iconTexture = EditorGUIUtility.IconContent("Animation.Record", "|Enable/disable keyframe recording mode.").image;
                GUI.DrawTexture(rect, iconTexture);
            }
        }
    }
    private List<Vector3> m_cachePoints = new List<Vector3>();
    public int m_segmentResolution = 20;
    private static Texture iconTexture = null;
}
