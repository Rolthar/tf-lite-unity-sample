using TensorFlowLite;
using UnityEngine;
using UnityEngine.UI;
using easyar;
using TMPro;


public class SsdSample : MonoBehaviour
{
    [SerializeField]
    private SSD.Options options = default;

    [SerializeField]
    private AspectRatioFitter frameContainer = null;

    [SerializeField]
    private Text framePrefab = null;

    [SerializeField, Range(0f, 1f)]
    private float scoreThreshold = 0.5f;

    [SerializeField]
    private TextAsset labelMap = null;

    private SSD ssd;
    private Text[] frames;
    private string[] labels;
    public ARSession Session;
    public RenderTexture renderTexture;
    public TMP_Text classesDisplayText;
    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // This is an example usage of the NNAPI delegate.
        if (options.accelerator == SSD.Accelerator.NNAPI && !Application.isEditor)
        {
            string cacheDir = Application.persistentDataPath;
            string modelToken = "ssd-token";
            var interpreterOptions = new InterpreterOptions();
            var nnapiOptions = NNAPIDelegate.DefaultOptions;
            nnapiOptions.AllowFp16 = true;
            nnapiOptions.CacheDir = cacheDir;
            nnapiOptions.ModelToken = modelToken;
            interpreterOptions.AddDelegate(new NNAPIDelegate(nnapiOptions));
            ssd = new SSD(options, interpreterOptions);
        }
        else
#endif // UNITY_ANDROID && !UNITY_EDITOR
        {
            ssd = new SSD(options);
        }

        // Init frames
        frames = new Text[10];
        Transform parent = frameContainer.transform;
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Instantiate(framePrefab, Vector3.zero, Quaternion.identity, parent);
            frames[i].transform.localPosition = Vector3.zero;
        }

        // Labels
        labels = labelMap.text.Split('\n');

        Session.StateChanged += (state) =>
        {
            if (state == ARSession.SessionState.Ready)
            {

                var renderer = Session.Assembly.FrameSource.GetComponent<CameraImageRenderer>();
                renderer.RequestTargetTexture((camera, texture) =>
                {
                    renderTexture = texture;
                });
            }
        };
    }

    private void OnDestroy()
    {
        ssd?.Dispose();
    }

    void Update()
    {
        if (renderTexture != null)
            Invoke(renderTexture);
    }

    private void Invoke(Texture texture)
    {
        ssd.Invoke(texture);
        classesDisplayText.text = "";
        SSD.Result[] results = ssd.GetResults();
        Vector2 size = (frameContainer.transform as RectTransform).rect.size;
        for (int i = 0; i < 10; i++)
        {
            SetFrame(frames[i], results[i], size, out string objectText);
            classesDisplayText.text += objectText;
        }
    }

    private void SetFrame(Text frame, SSD.Result result, Vector2 size, out string text)
    {
        if (result.score < scoreThreshold)
        {
            frame.gameObject.SetActive(false);
            text = "";
            return;
        }
        else
        {
            frame.gameObject.SetActive(true);
        }

        frame.text = $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
        text = frame.text;
        var rt = frame.transform as RectTransform;
        rt.anchoredPosition = result.rect.position * size - size * 0.5f;
        rt.sizeDelta = result.rect.size * size;
    }

    private string GetLabelName(int id)
    {
        if (id < 0 || id >= labels.Length - 1)
        {
            return "?";
        }
        return labels[id + 1];
    }

}
