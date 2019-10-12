using System;
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
    public ScreenTransformGesture transformGesture; //屏幕滑动手势获取组件
    private string FilePath;                        //手势XML读写路径
    private const int MinNoPoints = 5;              //最小数量特征点数（防止误操作）
    private bool _isDown;                           //手指是否按下
    private bool _recording = true;                 //是否为录入模式
    private Recognizer.Dollar.Recognizer _rec = new Recognizer.Dollar.Recognizer(); //手势识别类
    private List<TimePointF> _points = new List<TimePointF>(256);                   //特征点集


    //UI界面
    public Text ModeText;           //当前模式文本（录入模式/对比模式）
    public Text ResultText;         //对比结果文本
    public GameObject FileNamePanel;//存储文件名称输入界面
    public InputField fileNameInput;//存储文件名称输入框
    public Text TipText;            //提示文字（显示"文件名称不能为空"等提示文字）

    void Start()
    {
        ReadXMLFile();
        transformGesture.TransformStarted += TransformGesture_TransformStarted;
        transformGesture.Transformed += TransformGesture_Transformed;
        transformGesture.TransformCompleted += TransformGesture_TransformCompleted;
    }

    /// <summary>
    /// 读取XML特征点文件
    /// </summary>
    private void ReadXMLFile()
    {
        FilePath = Application.streamingAssetsPath + "/Gestures";
        if (Directory.Exists(FilePath))
        {
            string[] files = GetFiles(FilePath, ".xml");
            for (int i = 0; i < files.Length; i++)
            {
                string name = files[i];
                _rec.LoadGesture(name);
            }
        }
        else
        {
            Directory.CreateDirectory(FilePath);
        }
    }
    
    /// <summary>
    /// 获取路径下指定后缀名的所有文件路径
    /// </summary>
    /// <param name="path">文件夹路径</param>
    /// <param name="extension">后缀名</param>
    /// <returns>文件路径数组</returns>
    public string[] GetFiles(string path, string extension)
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
        return files.ToArray();
    }

    /// <summary>
    /// 手指开始滑动
    /// </summary>
    private void TransformGesture_TransformStarted(object sender, EventArgs e)
    {
        float x = transformGesture.ScreenPosition.x;
        float y = transformGesture.ScreenPosition.y;
        _isDown = true;
        _points.Clear();
        _points.Add(new TimePointF(x, y, TimeEx.NowMs));
    }
    
    /// <summary>
    /// 手指滑动时
    /// </summary>
    private void TransformGesture_Transformed(object sender, EventArgs e)
    {
        if (_isDown)
        {
            float x = transformGesture.ScreenPosition.x;
            float y = transformGesture.ScreenPosition.y;
            _points.Add(new TimePointF(x, y, TimeEx.NowMs));
        }
    }

    /// <summary>
    /// 手指滑动结束（抬起）时
    /// </summary>
    private void TransformGesture_TransformCompleted(object sender, EventArgs e)
    {
        if (_isDown)
        {
            _isDown = false;
            if (_points.Count >= MinNoPoints)
            {
                if (_recording)
                {
                    //录入模式
                    ShowFileNamePanel();
                }
                else if (_rec.NumGestures > 0) // not recording, so testing
                {
                    //对比模式
                    RecognizeAndDisplayResults();
                }
            }
        }
    }

    /// <summary>
    /// 得到对比结果并显示
    /// </summary>
    private void RecognizeAndDisplayResults()
    {
        bool _protractor = false;
        NBestList result = _rec.Recognize(_points, _protractor); // where all the action is!!
        ResultText.text = string.Format("{0}: {1} ({2}px, {3}{4}{5})",
            result.Name.Split('/')[result.Name.Split('/').Length - 1],
            Math.Round(result.Score, 2),
            Math.Round(result.Distance, 2),
            Math.Round(result.Angle, 2), (char)176, _points.Count);
    }

    #region UI相关
    /// <summary>
    /// 改变当前模式显示文本
    /// </summary>
    /// <param name="buttonName"></param>
    public void ChangeModeText(string buttonName)
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

    /// <summary>
    /// 弹出文件命名界面
    /// </summary>
    private void ShowFileNamePanel()
    {
        fileNameInput.text = "";
        TipText.gameObject.SetActive(false);
        FileNamePanel.SetActive(true);
    }

    /// <summary>
    /// 确定命名并录入
    /// </summary>
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
        ChangeModeText("compare");//录入成功后自动切换到对比模式
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
        FileNamePanel.SetActive(false);
    }

    /// <summary>
    /// 取消录入
    /// </summary>
    public void CancelBtnClick()
    {
        FileNamePanel.SetActive(false);
    }
    #endregion
}
