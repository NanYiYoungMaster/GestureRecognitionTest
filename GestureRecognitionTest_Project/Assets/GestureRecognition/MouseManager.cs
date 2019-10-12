using System;
using System.Collections;
using System.Collections.Generic;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;
using WobbrockLib;
using WobbrockLib.Extensions;
using Recognizer.Dollar;
using System.IO;
using UnityEditor;
using UnityEngine.UI;

public class MouseManager : MonoBehaviour
{
    public ScreenTransformGesture transformGesture;
    public Text ModeText;
    public Text ResultText;
    public GameObject FileNamePanel;
    public InputField fileNameInput;
    public Text TipText;

    private string FilePath;
    private const int MinNoPoints = 5;
    private Recognizer.Dollar.Recognizer _rec = new Recognizer.Dollar.Recognizer();
    private bool _isDown;
    private List<TimePointF> _points = new List<TimePointF>(256);

    void Start()
    {
        FilePath = Application.streamingAssetsPath + "/Gestures";
        if (Directory.Exists(FilePath))
        {
            string[] files = Directory.GetFiles(FilePath);
            for (int i = 0; i < files.Length; i++)
            {
                string name = files[i];
                if (name.EndsWith(".xml") || name.EndsWith(".XML"))
                {
                    Debug.Log(name);
                    _rec.LoadGesture(name);
                }
            }
        }
        transformGesture.TransformStarted += TransformGesture_TransformStarted;
        transformGesture.Transformed += TransformGesture_Transformed;
        transformGesture.TransformCompleted += TransformGesture_TransformCompleted;
    }

    //private void MainForm_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
    private void TransformGesture_TransformStarted(object sender, EventArgs e)
    {
        float x = transformGesture.ScreenPosition.x;
        float y = Screen.height - transformGesture.ScreenPosition.y;
        _isDown = true;
        _points.Clear();
        _points.Add(new TimePointF(x, y, TimeEx.NowMs));
        //Invalidate();
    }

    private void TransformGesture_Transformed(object sender, EventArgs e)
    {
        if (_isDown)
        {
            float x = transformGesture.ScreenPosition.x;
            float y = Screen.height - transformGesture.ScreenPosition.y;
            _points.Add(new TimePointF(x, y, TimeEx.NowMs));
            //Invalidate(new Rectangle(x - 2, y - 2, 4, 4));
        }
    }
    public bool _recording = true;

    private void TransformGesture_TransformCompleted(object sender, EventArgs e)
    {
        if (_isDown)
        {
            _isDown = false;

            if (_points.Count >= MinNoPoints)
            {
                //如果是录入模式
                if (_recording)
                {
                    if (!Directory.Exists(FilePath))
                    {
                        Directory.CreateDirectory(FilePath);
                    }
                    TipText.gameObject.SetActive(false);
                    FileNamePanel.SetActive(true);
                }
                //如果是比对模式
                else if (_rec.NumGestures > 0) // not recording, so testing
                {
                    RecognizeAndDisplayResults();
                }
                //Debug.Log(_points.Count + ":" + _rec.NumGestures);
            }
        }
    }

    private void RecognizeAndDisplayResults()
    {
        //Application.DoEvents(); // forces label to display

        bool _protractor = false;
        NBestList result = _rec.Recognize(_points, _protractor); // where all the action is!!
        //Debug.LogFormat("{0}: {1} ({2}px, {3}{4}{5}({6},{7}))",
        //    result.Name.Split('/')[result.Name.Split('/').Length - 1],
        //    Math.Round(result.Score, 2),
        //    Math.Round(result.Distance, 2),
        //    Math.Round(result.Angle, 2), (char)176, _points.Count, _points[0].X, _points[0].Y);
        ResultText.text = string.Format("{0}: {1} ({2}px, {3}{4}{5})",
            result.Name.Split('/')[result.Name.Split('/').Length - 1],
            Math.Round(result.Score, 2),
            Math.Round(result.Distance, 2),
            Math.Round(result.Angle, 2), (char)176, _points.Count);
    }

    public List<string> GetFiles(string path,string extension)
    {
        List<string> files = new List<string>();
        //获取指定文件夹的所有文件
        string[] paths = Directory.GetFiles(path);
        foreach (var item in paths)
        {
            //获取文件后缀名
            string _extension = Path.GetExtension(item).ToLower();
            if (_extension == extension)
            {
                files.Add(item);//添加到list中
            }
        }
        return files;
    }

    public void ChangeText(string buttonName)
    {
        if (buttonName=="save")
        {
            ModeText.text = "录入模式";
            _recording = true;
        }
        else if (buttonName=="compare")
        {
            ModeText.text = "比对模式";
            _recording = false;
        }
    }

    public void OKBtnClick()
    {
        if (fileNameInput.text == "")
        {
            TipText.gameObject.SetActive(true);
            return;
        }
        TipText.gameObject.SetActive(false);
        string fileName = Application.streamingAssetsPath + "/Gestures/" + fileNameInput.text + ".xml";
        _rec.SaveGesture(fileName, _points);  // resample, scale, translate to origin
        ChangeText("compare");
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
        FileNamePanel.SetActive(false);
    }

    public void CancelBtnClick()
    {
        FileNamePanel.SetActive(false);
    }
}
