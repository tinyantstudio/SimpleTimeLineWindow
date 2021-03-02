using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEditor;
using DMTimeArea;
using TimeUtility = DMTimeArea.TimeUtility;

namespace DMTimeArea
{
    public abstract class SimpleTimeArea : EditorWindow
    {
        protected TimeArea _simpleTimeArea;
        public TimeArea GetTimeArea
        {
            get { return _simpleTimeArea; }
        }
        private static readonly float kTimeAreaYPosition = 100f;
        private static readonly float kTimeAreaHeight = 100f;
        private static readonly float kTimeAreaMinWidth = 50f;

        protected const float ARROW_WIDTH = 6f;
        protected const int TIMELINETIMELABEL_HEIGHT = 18;
        private const int TIMERULER_HEIGHT = 28;// 18;
        protected const int HORIZONTALBAR_HEIGHT = 15;

        protected virtual int timeRulerHeight
        {
            get { return TIMERULER_HEIGHT; }
        }

        protected virtual int toolbarHeight
        {
            get
            {
                return 18;// origin 18}
            }
        }

        private bool _dragCurrentTimeArrow = false;
        private bool _dragCutOffTimeArrow = false;

        public abstract Rect _rectTimeAreaTotal
        {
            get;
        }

        public abstract Rect _rectTimeAreaContent
        {
            get;
        }

        public abstract Rect _rectTimeAreaRuler
        {
            get;
        }

        protected virtual double RunningTime
        {
            get { return 0f; }
            set { }
        }
        protected virtual double CutOffTime
        {
            get { return 0.0; }
            set { }
        }

        protected abstract bool IsLockedMoveFrame
        {
            get;
        }

        protected abstract bool IsLockDragHeaderArrow
        {
            get;
        }

        protected abstract float sequencerHeaderWidth
        {
            get;
        }

        public Rect _timeAreaBounds
        {
            get
            {
                float width = base.position.width - sequencerHeaderWidth;
                return new Rect(_rectTimeAreaContent.x, _rectTimeAreaContent.y, Mathf.Max(width, kTimeAreaMinWidth), _rectTimeAreaContent.height);
            }
        }

        //
        // Time Area settings
        //
        // Frame rate
        public float _frameRate = 30f;
        // Frame Snap
        public bool _frameSnap = true;
        // Time ruler format
        public bool _timeInFrames = false;
        // Edge snap
        public bool _edgeSnaps = true;
        // Draw helper action line
        public bool _drawActionHelperLine = true;
        // Helper dot line
        // public bool _actionHelperDotLine = true;

        public int RunningFrame
        {
            get
            {
                return TimeUtility.ToFrames(this.RunningTime, (double)this._frameRate);
            }
            set
            {
                this.RunningTime = (float)TimeUtility.FromFrames(Mathf.Max(0, value), (double)this._frameRate);
            }
        }

        public void PreviousTimeFrame()
        {
            if (!IsLockedMoveFrame)
            {
                this.RunningFrame--;
            }
        }

        public void NextTimeFrame()
        {
            if (!IsLockedMoveFrame)
            {
                this.RunningFrame++;
            }
        }

        public int CutOffFrame
        {
            get
            {
                return TimeUtility.ToFrames(this.CutOffTime, (double)this._frameRate);
            }
            set
            {
                this.CutOffTime = (float)TimeUtility.FromFrames(Mathf.Max(0, value), (double)this._frameRate);
            }
        }

        public Vector2 timeAreaScale
        {
            get
            {
                return this._simpleTimeArea.scale;
            }
        }

        public Vector2 timeAreaTranslation
        {
            get
            {
                return this._simpleTimeArea.translation;
            }
        }

        public Vector2 timeAreaTimeShownRange
        {
            get
            {
                float x = PixelToTime(_timeAreaBounds.xMin);
                float y = PixelToTime(_timeAreaBounds.xMax);
                return new Vector2(x, y);
            }
        }

        protected virtual void DrawTimeAreaBackGround()
        {
            GUI.Box(_rectTimeAreaContent, GUIContent.none, new GUIStyle("CurveEditorBackground"));
            // EditorGUI.DrawRect(_rectTimeAreaContent, new Color(0.16f, 0.16f, 0.16f, 1f));
            // EditorGUI.DrawRect(_rectTimeAreaContent, DMTimeLineStyles)
            _simpleTimeArea.mRect = this._timeAreaBounds;
            _simpleTimeArea.BeginViewGUI();
            _simpleTimeArea.SetTickMarkerRanges();
            _simpleTimeArea.DrawMajorTicks(this._rectTimeAreaTotal, (float)_frameRate);
            // DrawVerticalTickLine();
            _simpleTimeArea.EndViewGUI();

            // Mouse Event for zoom area
            _simpleTimeArea.OnAreaEvent();
        }

        protected virtual void DrawVerticalTickLine()
        {
        }

        protected void DrawTimeCodeGUI(bool setBeginTime = true)
        {
            string text;
            if (_simpleTimeArea != null)
            {
                double time01 = setBeginTime ? this.RunningTime : this.CutOffTime;
                text = this.TimeAsString(time01, "F2");
                bool flag = TimeUtility.OnFrameBoundary(time01, (double)this._frameRate);
                if (this._timeInFrames)
                {
                    if (flag)
                    {
                        text = setBeginTime ? RunningFrame.ToString() : CutOffFrame.ToString();
                    }
                    else
                        text = TimeUtility.ToExactFrames(time01, (double)this._frameRate).ToString("F2");
                }
            }
            else
                text = "0";
            EditorGUI.BeginChangeCheck();
            string text2 = EditorGUILayout.DelayedTextField(text, EditorStyles.toolbarTextField, new GUILayoutOption[]
            {
                GUILayout.Width(70f)
            });

            bool flag2 = EditorGUI.EndChangeCheck();
            if (flag2)
            {
                if (_timeInFrames)
                {
                    int frame = setBeginTime ? RunningFrame : CutOffFrame;
                    double d = 0.0;
                    if (double.TryParse(text2, out d))
                        frame = Math.Max(0, (int)Math.Floor(d));

                    if (setBeginTime)
                        RunningFrame = frame;
                    else
                        CutOffFrame = frame;
                }
                else
                {
                    double num = TimeUtility.ParseTimeCode(text2, (double)this._frameRate, -1.0);
                    if (num > 0.0)
                    {
                        if (setBeginTime)
                            RunningTime = (float)num;
                        else
                            CutOffTime = (float)num;
                    }
                }
            }
        }

        public float TimeToTimeAreaPixel(double time)
        {
            float num = (float)time;
            num *= this.timeAreaScale.x;
            return num + (this.timeAreaTranslation.x + this.sequencerHeaderWidth);
        }

        public float TimeToScreenSpacePixel(double time)
        {
            float num = (float)time;
            num *= this.timeAreaScale.x;
            return num + this.timeAreaTranslation.x;
        }

        public string TimeAsString(double timeValue, string format = "F2")
        {
            string result;
            if (this._timeInFrames)
            {
                result = TimeUtility.TimeAsFrames(timeValue, (double)this._frameRate, format);
            }
            else
            {
                result = TimeUtility.TimeAsTimeCode(timeValue, (double)this._frameRate, format);
            }
            return result;
        }

        public float TimeToPixel(double time)
        {
            return _simpleTimeArea.TimeToPixel((float)time, _timeAreaBounds);
        }

        public float TimeToPixel(double time, float rectWidth, float rectX, float x, float y, float width)
        {
            return _simpleTimeArea.TimeToPixel((float)time, rectWidth, rectX, x, y, width);
        }

        public float YToPixel(float y)
        {
            return _simpleTimeArea.YToPixel(y, _timeAreaBounds);
        }

        public float PixelToY(float pixel)
        {
            return _simpleTimeArea.PixelToY(pixel);
        }

        public float PixelToTime(float pixel)
        {
            return _simpleTimeArea.PixelToTime(pixel, _timeAreaBounds);
        }

        public float TimeAreaPixelToTime(float pixel)
        {
            return this.PixelToTime(pixel);
        }

        public double GetSnappedTimeAtMousePosition(Vector2 mousePos)
        {
            return this.SnapToFrameIfRequired((double)this.ScreenSpacePixelToTimeAreaTime(mousePos.x));
        }

        public double SnapToFrameIfRequired(double time)
        {
            double result;
            if (this._frameSnap)
            {
                result = TimeUtility.FromFrames(TimeUtility.ToFrames(time, (double)this._frameRate), (double)this._frameRate);
            }
            else
            {
                result = time;
            }
            return result;
        }

        public float ScreenSpacePixelToTimeAreaTime(float p)
        {
            p -= this._timeAreaBounds.x;
            return this.TrackSpacePixelToTimeAreaTime(p);
        }

        public float TrackSpacePixelToTimeAreaTime(float p)
        {
            p -= this.timeAreaTranslation.x;
            float result;
            if (this.timeAreaScale.x > 0f)
            {
                result = p / this.timeAreaScale.x;
            }
            else
            {
                result = p;
            }
            return result;
        }

        // Change params to hrangelocked and vrangelocked
        // protected void InitTimeArea()
        protected void InitTimeArea(
            bool hLocked = false,
            bool vLocked = true,
            bool showhSlider = false,
            bool showVSlider = false)
        {
            if (_simpleTimeArea == null)
            {
                // create new timeArea
                this._simpleTimeArea = new TimeArea(false)
                {
                    hRangeLocked = hLocked,
                    vRangeLocked = vLocked,
                    margin = 10f,
                    scaleWithWindow = true,
                    hSlider = showhSlider,
                    vSlider = showVSlider,
                    hRangeMin = 0f,
                    vRangeMin = float.NegativeInfinity,
                    vRangeMax = float.PositiveInfinity,
                    mRect = _timeAreaBounds,
                };
                this._simpleTimeArea.hTicks.SetTickModulosForFrameRate(this._frameRate);
                // show time range begin seconds to end seconds(xxs - xxs)
                this._simpleTimeArea.SetShownHRange(-1, 5f);
                this._simpleTimeArea.SetShownVRange(0, 100f);

                Debug.Log("------>## Init Simple Time Area ##");
            }
        }

        protected void DrawTimeRulerArea()
        {
            //
            // Draw Current Running Time Cursor and red guide line
            //
            GUILayout.BeginArea(_rectTimeAreaTotal, string.Empty/*, EditorStyles.toolbarButton*/);
            Color cl01 = GUI.color;
            GUI.color = Color.red;
            float timeToPos = TimeToPixel(this.RunningTime);
            GUI.DrawTexture(new Rect(-ARROW_WIDTH + timeToPos - _rectTimeAreaRuler.x, 2, ARROW_WIDTH * 2f, ARROW_WIDTH * 2f * 1.82f), ResManager.TimeHeadTexture);
            GUI.color = cl01;
            Rect lineRect = new Rect(timeToPos - _rectTimeAreaRuler.x, TIMELINETIMELABEL_HEIGHT, 1, _rectTimeAreaContent.height + 6);
            EditorGUI.DrawRect(lineRect, Color.red);
            GUILayout.EndArea();

            //
            // Draw Cut off Cursor and blue guide line
            //
            GUILayout.BeginArea(_rectTimeAreaTotal);
            timeToPos = TimeToPixel(this.CutOffTime);
            GUI.color = new Color(0 / 255f, 196f / 255f, 255f / 255f);
            Handles.color = new Color(0 / 255f, 196f / 255f, 255f / 255f);
            Handles.DrawLine(
                new Vector3(timeToPos - _rectTimeAreaRuler.x, timeRulerHeight),
                new Vector3(timeToPos - _rectTimeAreaRuler.x, this.position.height - timeRulerHeight - ARROW_WIDTH * 2 + 16));

            Rect cutOffRect = new Rect(-ARROW_WIDTH + timeToPos - _rectTimeAreaRuler.x, base.position.height - ARROW_WIDTH * 2 * 1.82f - toolbarHeight, ARROW_WIDTH * 2, ARROW_WIDTH * 2 * 1.82f);
            GUI.DrawTexture(cutOffRect, ResManager.CutOffGuideLineTexture);
            GUI.color = cl01;
            GUILayout.EndArea();

            //
            // Time ruler
            //
            _simpleTimeArea.TimeRuler(_rectTimeAreaRuler, _frameRate, true, false, 1f, _timeInFrames ? TimeArea.TimeFormat.Frame : TimeArea.TimeFormat.TimeFrame);

            if (_dragCutOffTimeArrow)
            {
                DrawLineWithTipsRectByTime(this.CutOffTime, 0f, _rectTimeAreaRuler.y, true, new Color(0.6f, 0.6f, 0.6f));
            }
        }

        protected void OnTimeRulerCursorAndCutOffCursorInput()
        {
            //
            // Mouse drag for cut off guideline
            //
            float timeToPos = TimeToPixel(this.CutOffTime);
            Rect cutOffRect = new Rect(-ARROW_WIDTH + timeToPos - _rectTimeAreaRuler.x, base.position.height - ARROW_WIDTH * 2 - toolbarHeight, ARROW_WIDTH * 2, ARROW_WIDTH * 2);
            Event evt = Event.current;
            int controlId = GUIUtility.GetControlID(kBlueCursorControlID, FocusType.Passive);
            int redControlId = GUIUtility.GetControlID(kRedCursorControlID, FocusType.Passive);

            if (evt.rawType == EventType.MouseUp)
            {
                if (GUIUtility.hotControl == controlId || GUIUtility.hotControl == redControlId)
                {
                    GUIUtility.hotControl = 0;
                    evt.Use();
                }
                _dragCutOffTimeArrow = false;
            }
            Vector2 mousePos = new Vector2(evt.mousePosition.x - _rectTimeAreaTotal.x, evt.mousePosition.y - _rectTimeAreaTotal.y);
            if (!Application.isPlaying)
            {
                switch (evt.GetTypeForControl(controlId))
                {
                    case EventType.MouseDown:
                        {
                            if (cutOffRect.Contains(mousePos))
                            {
                                GUIUtility.hotControl = controlId;
                                evt.Use();
                            }
                        }
                        break;
                    case EventType.MouseDrag:
                        {
                            if (GUIUtility.hotControl == controlId)
                            {
                                Vector2 vec = new Vector2(evt.mousePosition.x, evt.mousePosition.y);
                                double fTime = GetSnappedTimeAtMousePosition(vec);
                                if (fTime <= 0)
                                    fTime = 0;
                                this.CutOffTime = fTime;
                                _dragCutOffTimeArrow = true;
                            }
                        }
                        break;
                    default: break;
                }
            }

            //
            // Drag cut off time guide line
            //
            // Mouse for time guide line
            evt = Event.current;
            mousePos = evt.mousePosition;

            if (!Application.isPlaying)
            {
                switch (evt.GetTypeForControl(redControlId))
                {
                    case EventType.MouseDown:
                        {
                            if (_rectTimeAreaRuler.Contains(mousePos))
                            {
                                GUIUtility.hotControl = redControlId;
                                evt.Use();
                                double fTime = GetSnappedTimeAtMousePosition(mousePos);
                                if (fTime <= 0)
                                    fTime = 0.0;
                                this.RunningTime = fTime;
                            }
                        }
                        break;
                    case EventType.MouseDrag:
                        {
                            if (GUIUtility.hotControl == redControlId)
                            {
                                if (!IsLockDragHeaderArrow)
                                {
                                    double fTime = GetSnappedTimeAtMousePosition(mousePos);
                                    if (fTime <= 0)
                                        fTime = 0.0;
                                    this.RunningTime = fTime;
                                }
                            }
                        }
                        break;
                    default: break;
                }
            }
        }

        private static int kRedCursorControlID = "RedCursorControlRect".GetHashCode();
        private static int kBlueCursorControlID = "BlueCursorControlRect".GetHashCode();

        protected void DrawLineWithTipsRectByTime(double fTime, float offSet, float yPos, bool dotLine, Color color)
        {
            float timeToPos = TimeToPixel(fTime);
            Rect drawRect = new Rect(timeToPos - offSet, yPos, 1, _rectTimeAreaContent.height + 15);
            float num = drawRect.y;
            Vector3 p = new Vector3(drawRect.x, num, 0f);
            Vector3 p2 = new Vector3(drawRect.x, num + Mathf.Min(drawRect.height, _rectTimeAreaTotal.height), 0f);
            if (true)
            {
                if (dotLine)
                {
                    TimeAreaTools.DrawDottedLine(p, p2, 5f, color);
                }
                else
                {
                    // Rect rect2 = Rect.MinMaxRect(p.x - 0.5f, p.y, p2.x + 0.5f, p2.y);
                    EditorGUI.DrawRect(drawRect, color);
                }
            }

            // draw time tips
            // Time ruler
            GUIStyle TimelineTick = "AnimationTimelineTick";
            string beginTime = TimeAsString(fTime);
            GUIContent lb = new GUIContent(beginTime);
            Vector2 size = TimelineTick.CalcSize(lb);
            Color pre = GUI.color;
            GUI.color = Color.white;
            Rect rectTip = new Rect(timeToPos - offSet, yPos, size.x, size.y);
            rectTip.x -= 4;
            rectTip.width += 8;
            GUI.Box(rectTip, GUIContent.none, "Button");
            rectTip.y = yPos - 3;
            rectTip.x += 4;
            rectTip.width -= 8;
            GUI.color = pre;
            GUI.Label(rectTip, lb, TimelineTick);
        }

        //
        // Key for Frame Movement
        //

        public delegate void OnUserInputKeyCode(bool ctrl, KeyCode code);

        private event OnUserInputKeyCode _inputKeyCodeEvt;
        protected void RegisterInputKeyCodeEvt(OnUserInputKeyCode evt)
        {
            if (evt != null)
            {
                _inputKeyCodeEvt += evt;
            }
        }

        protected void UnRegisterInputKeyCodeEvt(OnUserInputKeyCode evt)
        {
            if (evt != null)
            {
                _inputKeyCodeEvt -= evt;
            }
        }


        protected void OnUserInput(Event evt)
        {
            if (evt.type == EventType.KeyDown)
            {
                if (!evt.control && evt.keyCode == KeyCode.P)
                {
                    if (_inputKeyCodeEvt != null)
                        _inputKeyCodeEvt(false, KeyCode.P);
                }
            }

            if (evt.control)
            {
                if (evt.type == EventType.KeyDown)
                {
                    if (_inputKeyCodeEvt != null)
                    {
                        _inputKeyCodeEvt(true, evt.keyCode);
                    }
                }
            }
        }

        private void ChangeTimeCode(object obj)
        {
            string a = obj.ToString();
            if (a == "frames")
            {
                _timeInFrames = true;
            }
            else
            {
                _timeInFrames = false;
            }
        }

        protected void OnClickSettingButton()
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Seconds"), !_timeInFrames, new GenericMenu.MenuFunction2(this.ChangeTimeCode), "seconds");
            genericMenu.AddItem(new GUIContent("Frames"), _timeInFrames, new GenericMenu.MenuFunction2(this.ChangeTimeCode), "frames");
            genericMenu.AddSeparator("");
            genericMenu.AddDisabledItem(new GUIContent("Frame rate"));
            genericMenu.AddItem(new GUIContent("Film (24)"), _frameRate.Equals(24f), delegate (object r)
            {
                this._frameRate = (float)r;
            }, 24f);
            //genericMenu.AddItem(new GUIContent("PAL (25)"), _frameRate.Equals(25f), delegate (object r)
            //{
            //    _frameRate = (float)r;
            //}, 25f);
            //genericMenu.AddItem(new GUIContent("NTSC (29.97)"), _frameRate.Equals(29.97f), delegate (object r)
            //{
            //    _frameRate = (float)r;
            //}, 29.97f);
            genericMenu.AddItem(new GUIContent("30"), _frameRate.Equals(30f), delegate (object r)
            {
                _frameRate = (float)r;
            }, 30f);
            genericMenu.AddItem(new GUIContent("50"), _frameRate.Equals(50f), delegate (object r)
            {
                _frameRate = (float)r;
            }, 50f);
            genericMenu.AddItem(new GUIContent("60"), _frameRate.Equals(60f), delegate (object r)
            {
                _frameRate = (float)r;
            }, 60f);
            genericMenu.AddDisabledItem(new GUIContent("Custom"));
            genericMenu.AddSeparator("");
            genericMenu.AddItem(new GUIContent("Snap to Frame"), this._frameSnap, delegate
            {
                this._frameSnap = !this._frameSnap;
            });
            genericMenu.AddItem(new GUIContent("Edge Snap"), this._edgeSnaps, delegate
            {
                this._edgeSnaps = !this._edgeSnaps;
            });

            OnCreateSettingContent(genericMenu);
            genericMenu.ShowAsContext();
        }

        protected virtual void OnCreateSettingContent(GenericMenu menu)
        {

        }
    }
}
